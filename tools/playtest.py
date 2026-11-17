#!/usr/bin/env python3
"""Analizador de sesiones de playtest de MK Rebuild.

Lee los dos canales de una sesión (``*.level.jsonl`` y ``*.combat.jsonl``) y los cruza
por tiempo. Python 3 estándar: sin dependencias, sin red y con salida determinista
(misma entrada => mismos bytes; sin timestamps, sin floats sin acotar, sin orden de
diccionario accidental).

Uso:
    python tools/playtest.py <archivo|carpeta> resumen
    python tools/playtest.py <archivo|carpeta> embudo
    python tools/playtest.py <archivo|carpeta> combos
    python tools/playtest.py <archivo|carpeta> entradas
    python tools/playtest.py <archivo|carpeta> estela [--out estela.svg]

El ``path`` puede ser cualquiera de los dos archivos de la sesión o la carpeta que
los contiene; con una carpeta se elige la sesión más reciente por nombre (orden
alfabético = cronológico, TEL-1) salvo que se pase ``--session <prefijo>``.

Heurísticas documentadas (el log no las guarda y el script no las inventa en silencio):

* ``quieto`` = intervalo entre dos muestras ``p`` consecutivas cuyo ``sp`` de la
  muestra que abre el intervalo es menor a 0,10 m/s.
* ``combate`` = intervalo que contiene al menos un evento de combate
  (``player.damaged``, ``enemy.damaged``, ``combo``, ``encounter``, ``boss.phase``,
  ``boss.died``). Tiene prioridad sobre ``quieto``.
* ``tránsito`` = todo lo demás.
* La sala de un evento del canal ``combat`` es la de la última muestra ``p`` (o
  ``room.enter``) con ``t`` menor o igual: los dos archivos comparten el mismo reloj
  y el mismo ``sid`` (TEL-7). Ese cruce también se usa para el daño RECIBIDO, que
  sólo existe en ``combat`` (``player.damaged``): si el canal no está, la columna
  muestra ``—`` en vez de un 0 falso.
* ``dwell`` sale SÓLO de ``room.exit`` del canal ``level``; la visita que quedó
  abierta al cerrar la app no suma (no hay ``dwell`` que leer).
* Una sala se marca ``!`` cuando su dwell se aparta más del 20 % de su presupuesto
  (doc 02). La tabla siempre tiene las 10 salas, incluidas las no visitadas.
* El daño INFLIGIDO sale de ``enemy.damaged``; el golpe letal de cada enemigo no
  dispara ``Damaged``, así que ese daño no existe en el archivo (límite del gameplay,
  no del script).
* ``mark.tr = 1`` marca una pulsación hecha en tránsito (corredor): ``r`` es la
  última sala real y la posición no estaba dentro de ella. Un archivo viejo sin
  ``tr`` se lee como ``tr = 0``.
* ``combo.h`` es la cantidad de golpes (pasos distintos) que conectaron en la cadena:
  la profundidad real de lo que TOCÓ. ``combo.n`` es el índice del paso en el asset y en los
  combos ramificados salta (``0→6``), así que no sirve como profundidad. Un archivo viejo sin
  ``h`` se lee con ``h = n + 1`` y la tabla lo anota al pie: es un piso, no una medición.
  ``combo.ok`` sólo tiene sentido con ``len >= 0`` (combo lineal).
* ``combo.ph`` es la cantidad de pasos DISTINTOS JUGADOS (``combo.p`` es el índice máximo
  jugado): la profundidad de lo que SALIÓ, haya tocado o no. ``h`` sólo sube por impacto, así
  que una cadena 0→1→2 con un solo golpe conectado trae ``h=1, ph=3`` y la diferencia entre
  GOLPES y JUGADOS es el whiff. Una cadena que no conectó nada llega con ``n=-1, h=0`` y
  ``c="?"`` (el asset sólo se conoce al conectar): es un intento, no un error. Un archivo viejo
  sin ``p``/``ph`` muestra ``—`` en esas columnas, nunca un 0 inventado.
* ``in`` es UNA pulsación de ataque (``k = light|heavy|both``) con el ``step`` vigente y los
  ``ms`` desde que arrancó ese paso (``-1`` sin paso). ``step`` es cada cambio de
  ``PlayerCombat.CurrentStepIndex`` (``n = 0`` arranca, ``n = -1`` cierra). El comando
  ``entradas`` cruza los dos: un press encadena si el PRIMER ``step`` posterior está a 400 ms o
  menos y su ``n`` es mayor. La ventana de cancel se MIDE con el histograma de ``ms``; no se
  asume. Las pulsaciones sin paso activo (``step = -1``) son aperturas y no entran en el %.
  Durante la pausa no se graba ningún ``in``: el clic del menú es ``LightAttack``.
"""

from __future__ import annotations

import argparse
import bisect
import colorsys
import json
import os
import sys
from typing import Dict, Iterable, List, Optional, Sequence, Tuple

# Catálogo CERRADO del doc 02, en el mismo orden que PlaytestRooms.All (TEL-4).
ROOMS: Tuple[Tuple[str, float], ...] = (
    ("atrio", 20.0),
    ("lanzas", 30.0),
    ("pozo", 45.0),
    ("guardia", 45.0),
    ("galeria", 45.0),
    ("pesos", 60.0),
    ("camara-peso", 40.0),
    ("guardian", 90.0),
    ("orochi", 120.0),
    ("descenso", 20.0),
)
NO_ROOM = "sin-sala"
TOLERANCE = 0.20
IDLE_SPEED = 0.10
MISSING = "—"  # dato ausente: el canal que lo tendría no existe (≠ 0 medido)
COMBAT_EVENTS = frozenset(
    ("player.damaged", "enemy.damaged", "combo", "encounter", "boss.phase", "boss.died")
)

LEVEL_SUFFIX = ".level.jsonl"
COMBAT_SUFFIX = ".combat.jsonl"


# --------------------------------------------------------------------------- carga


def load_jsonl(path: Optional[str]) -> Tuple[List[dict], int]:
    """Carga un JSONL tolerante a líneas truncadas o corruptas (TEL-2).

    Devuelve los eventos válidos en el orden del archivo y la cantidad de líneas
    descartadas. Un corte abrupto deja como máximo la última línea partida, y esa
    línea no puede romper el análisis.
    """
    events: List[dict] = []
    discarded = 0
    if not path or not os.path.exists(path):
        return events, discarded

    with open(path, "r", encoding="utf-8") as handle:
        for raw in handle:
            line = raw.strip()
            if not line:
                continue
            try:
                obj = json.loads(line)
            except ValueError:
                discarded += 1
                continue
            if isinstance(obj, dict) and "e" in obj:
                events.append(obj)
            else:
                discarded += 1

    return events, discarded


class Session:
    """Una ejecución: prefijo, dos canales y los eventos ya cargados."""

    def __init__(self, folder: str, prefix: str) -> None:
        self.folder = folder
        self.prefix = prefix
        self.level_path = os.path.join(folder, prefix + LEVEL_SUFFIX)
        self.combat_path = os.path.join(folder, prefix + COMBAT_SUFFIX)

        self.level, self.level_discarded = load_jsonl(self.level_path)
        self.combat, self.combat_discarded = load_jsonl(self.combat_path)

        if not self.level and not self.combat:
            raise SystemExit("no hay eventos legibles en la sesión " + prefix)

    @property
    def discarded(self) -> int:
        return self.level_discarded + self.combat_discarded

    def has_level(self) -> bool:
        return os.path.exists(self.level_path)

    def has_combat(self) -> bool:
        return os.path.exists(self.combat_path)

    def start(self) -> dict:
        for event in self.level + self.combat:
            if event.get("e") == "session.start":
                return event
        return {}

    def end(self) -> Optional[dict]:
        last = None
        for event in self.level:
            if event.get("e") == "session.end":
                last = event
        return last


def split_prefix(file_name: str) -> Optional[str]:
    if file_name.endswith(LEVEL_SUFFIX):
        return file_name[: -len(LEVEL_SUFFIX)]
    if file_name.endswith(COMBAT_SUFFIX):
        return file_name[: -len(COMBAT_SUFFIX)]
    return None


def open_session(path: str, wanted: Optional[str]) -> Session:
    if os.path.isdir(path):
        folder = path
        prefixes = sorted(
            {split_prefix(name) for name in os.listdir(folder) if split_prefix(name)}
        )
        if not prefixes:
            raise SystemExit("no hay sesiones en " + folder)
        if wanted:
            if wanted not in prefixes:
                raise SystemExit("no existe la sesión '" + wanted + "' en " + folder)
            prefix = wanted
        else:
            prefix = prefixes[-1]
        return Session(folder, prefix)

    folder = os.path.dirname(os.path.abspath(path))
    name = os.path.basename(path)
    prefix = wanted or split_prefix(name)
    if not prefix:
        raise SystemExit("el archivo no se llama <sesion-...>.level.jsonl / .combat.jsonl")
    return Session(folder, prefix)


# --------------------------------------------------------------------------- tiempo


def event_t(event: dict) -> int:
    try:
        return int(event.get("t", 0))
    except (TypeError, ValueError):
        return 0


def sorted_by_t(events: Iterable[dict]) -> List[dict]:
    return sorted(events, key=lambda e: (event_t(e), str(e.get("e", ""))))


def build_room_timeline(level: Sequence[dict]) -> Tuple[List[int], List[str]]:
    points: List[Tuple[int, str]] = []
    for event in level:
        if event.get("e") not in ("p", "room.enter", "room.exit"):
            continue
        room = event.get("r")
        if isinstance(room, str) and room:
            points.append((event_t(event), room))

    points.sort()
    return [p[0] for p in points], [p[1] for p in points]


class RoomIndex:
    """Sala atribuida a un instante, usando el reloj del canal level."""

    def __init__(self, level: Sequence[dict]) -> None:
        self.times, self.rooms = build_room_timeline(level)

    def at(self, t: int) -> str:
        if not self.times:
            return NO_ROOM
        index = bisect.bisect_right(self.times, t) - 1
        return self.rooms[index] if index >= 0 else NO_ROOM


# --------------------------------------------------------------------------- formato


def secs(value: float) -> str:
    return "{:.1f} s".format(value)


def clock(value: float) -> str:
    total = max(0, int(value))
    return "{:02d}:{:02d}".format(total // 60, total % 60)


def print_header(session: Session) -> None:
    start = session.start()
    print("sesión: " + session.prefix)
    print("canales: level={} combat={}".format(
        "sí" if session.has_level() else "no",
        "sí" if session.has_combat() else "no",
    ))
    print("líneas: level={} combat={} (descartadas: {})".format(
        len(session.level), len(session.combat), session.discarded
    ))
    if start:
        print("jugador: {} · build: {} · escena: {} · sid: {}".format(
            start.get("p", "?"), start.get("bv", "?"), start.get("sc", "?"), start.get("sid", "?")
        ))
    print()


# --------------------------------------------------------------------------- resumen


def session_duration(session: Session) -> float:
    end = session.end()
    if end is not None:
        try:
            return max(0.0, float(end.get("dur", 0.0)))
        except (TypeError, ValueError):
            return 0.0

    last = 0
    for event in session.level + session.combat:
        last = max(last, event_t(event))
    return last / 1000.0


def time_split(session: Session) -> Tuple[float, float, float]:
    """Reparte el tiempo en combate / tránsito / quieto sobre las muestras de 4 Hz."""
    samples = sorted_by_t([e for e in session.level if e.get("e") == "p"])
    if len(samples) < 2:
        return 0.0, 0.0, 0.0

    combat_times = sorted(
        event_t(e) for e in list(session.level) + list(session.combat)
        if e.get("e") in COMBAT_EVENTS
    )

    in_combat = 0.0
    still = 0.0
    moving = 0.0

    for index in range(len(samples) - 1):
        start = event_t(samples[index])
        stop = event_t(samples[index + 1])
        delta = (stop - start) / 1000.0
        if delta <= 0.0:
            continue

        # Cota de cordura: un hueco gigante (crash, pausa larga) no es tiempo jugado.
        if delta > 5.0:
            continue

        low = bisect.bisect_left(combat_times, start)
        if low < len(combat_times) and combat_times[low] < stop:
            in_combat += delta
            continue

        try:
            speed = float(samples[index].get("sp", 0.0))
        except (TypeError, ValueError):
            speed = 0.0

        if speed < IDLE_SPEED:
            still += delta
        else:
            moving += delta

    return in_combat, moving, still


def mark_room_label(event: dict) -> str:
    """Sala de la marca, con sufijo de tránsito cuando `tr = 1`.

    `tr` es opcional: un archivo anterior al campo se lee como `tr = 0` (marca dentro de
    la sala), así la sesión vieja sigue leyéndose igual que antes.
    """
    room = event.get("r", NO_ROOM)
    try:
        transit = int(event.get("tr", 0))
    except (TypeError, ValueError):
        transit = 0
    return "{} (tránsito)".format(room) if transit else str(room)


def marks_by_room(level: Sequence[dict]) -> List[Tuple[str, int]]:
    counts: Dict[str, int] = {}
    for event in level:
        if event.get("e") != "mark":
            continue
        label = mark_room_label(event)
        counts[label] = counts.get(label, 0) + 1
    return sorted(counts.items())


def cmd_resumen(session: Session) -> None:
    print_header(session)

    end = session.end()
    duration = session_duration(session)

    deaths = sum(1 for e in session.level if e.get("e") == "player.died")
    distance = 0.0
    if end is not None:
        try:
            distance = float(end.get("dist", 0.0))
        except (TypeError, ValueError):
            distance = 0.0

    marks = marks_by_room(session.level)
    total_marks = sum(count for _, count in marks)

    print("duración: {} ({})".format(clock(duration), secs(duration)))
    print("muertes: {}".format(deaths))
    print("distancia: {:.1f} m".format(distance))
    print("session.end: {}".format("sí" if end is not None else "no (corte abrupto)"))

    distribution = ", ".join("{} {}".format(room, count) for room, count in marks)
    print("marcas: {}{}".format(total_marks, " (" + distribution + ")" if distribution else ""))

    in_combat, moving, still = time_split(session)
    played = in_combat + moving + still
    if played > 0.0:
        print("reparto: combate {} ({:.1f} %) · tránsito {} ({:.1f} %) · quieto {} ({:.1f} %)".format(
            secs(in_combat), 100.0 * in_combat / played,
            secs(moving), 100.0 * moving / played,
            secs(still), 100.0 * still / played,
        ))
    else:
        print("reparto: sin muestras suficientes")


# --------------------------------------------------------------------------- embudo


class RoomStats:
    def __init__(self) -> None:
        self.dwell = 0.0
        self.visits = 0
        self.deaths = 0
        self.damage_taken = 0
        self.damage_dealt = 0
        self.idle = 0.0
        self.verbs: Dict[str, int] = {}


def collect_rooms(session: Session) -> Dict[str, RoomStats]:
    stats: Dict[str, RoomStats] = {room_id: RoomStats() for room_id, _ in ROOMS}

    for event in session.level:
        kind = event.get("e")
        room = event.get("r")

        if kind == "room.exit" and room in stats:
            try:
                stats[room].dwell += max(0.0, float(event.get("dwell", 0.0)))
            except (TypeError, ValueError):
                pass
        elif kind == "room.enter" and room in stats:
            stats[room].visits += 1
        elif kind == "idle" and room in stats:
            try:
                stats[room].idle += max(0.0, float(event.get("dur", 0.0)))
            except (TypeError, ValueError):
                pass
        elif kind == "verb" and room in stats:
            key = str(event.get("k", "?"))
            stats[room].verbs[key] = stats[room].verbs.get(key, 0) + 1
        elif kind == "player.died" and room in stats:
            stats[room].deaths += 1

    index = RoomIndex(session.level)
    for event in session.combat:
        kind = event.get("e")
        if kind == "enemy.damaged":
            room = index.at(event_t(event))
            if room not in stats:
                continue
            try:
                stats[room].damage_dealt += int(event.get("dmg", 0))
            except (TypeError, ValueError):
                pass
        elif kind == "player.damaged":
            # El daño RECIBIDO vive en `combat` (TEL-7), no en `level`: se cruza por `t`
            # igual que el infligido. Leerlo de `level` daba 0 en todas las filas.
            room = index.at(event_t(event))
            if room not in stats:
                continue
            try:
                stats[room].damage_taken += int(event.get("dmg", 0))
            except (TypeError, ValueError):
                pass

    return stats


def cmd_embudo(session: Session) -> None:
    print_header(session)

    stats = collect_rooms(session)

    # Las dos columnas de daño viven en el canal `combat`. Sin ese archivo no hay 0 medido:
    # hay dato ausente, y un `—` es honesto donde un `0` inventaría una pelea que no se grabó.
    has_combat = session.has_combat()

    header = (
        "{:<16}{:>9}{:>10}{:>13}{:>11}{:>9}{:>11}{:>11}{:>8}  {}".format(
            "SALA", "VISITAS", "TIEMPO", "PRESUPUESTO", "Δ", "MUERTES", "DAÑO-REC", "DAÑO-INF", "%IDLE", "VERBOS"
        )
    )
    print(header)
    print("-" * len(header))

    exceeded = 0
    for room_id, budget in ROOMS:
        row = stats[room_id]
        delta = row.dwell - budget
        percent = abs(delta) / budget if budget > 0 else 0.0

        # Una sala no visitada tiene 0 dwell y Δ = -presupuesto: se informa, pero NO se marca
        # como excedida. El `!` es para desvíos medidos, no para "todavía no llegó".
        flagged = row.visits > 0 and percent > TOLERANCE
        if flagged:
            exceeded += 1

        verbs = " ".join(
            "{} {}".format(key, row.verbs[key]) for key in sorted(row.verbs)
        )
        idle_percent = (100.0 * row.idle / row.dwell) if row.dwell > 0 else 0.0

        damage_taken = row.damage_taken if has_combat else MISSING
        damage_dealt = row.damage_dealt if has_combat else MISSING

        marker = "! " if flagged else ("- " if row.visits == 0 else "  ")
        print("{}{:<16}{:>9}{:>10}{:>13}{:>11}{:>9}{:>11}{:>11}{:>8}  {}".format(
            marker,
            room_id,
            row.visits,
            secs(row.dwell),
            secs(budget),
            ("+" if delta >= 0 else "-") + secs(abs(delta)),
            row.deaths,
            damage_taken,
            damage_dealt,
            "{:.1f}".format(idle_percent),
            verbs,
        ))

    print("-" * len(header))
    print("salas visitadas fuera de ±{:.0f} %: {}".format(TOLERANCE * 100.0, exceeded))
    print("! = dwell fuera de tolerancia · - = sala no visitada (0 visitas, se informa igual)")
    if not has_combat:
        print("{} = el canal combat no existe en esta sesión: sin daño medido, no daño 0".format(MISSING))


# --------------------------------------------------------------------------- combos


def cmd_combos(session: Session) -> None:
    print_header(session)

    if not session.has_combat():
        print("el canal combat no está habilitado en esta sesión (-log=level)")
        print("las cadenas se emiten SÓLO en el canal combat; corré con -log=level,combat")
        return

    combos: List[dict] = [e for e in sorted_by_t(session.combat) if e.get("e") == "combo"]
    if not combos:
        print("no hay líneas 'combo' en esta sesión")
        return

    index = RoomIndex(session.level)

    per_combo: Dict[str, Dict[str, object]] = {}
    per_room: Dict[str, Dict[str, int]] = {}
    measured_hits = 0
    derived_hits = 0
    played_lines = 0
    unplayed_lines = 0
    paired_hits = 0
    paired_played = 0

    for event in combos:
        key = str(event.get("c", "?"))
        entry = per_combo.setdefault(
            key,
            {
                "lines": 0,
                "hits": 0,
                "hits_max": 0,
                "reached_max": 0,
                "played": 0,
                "played_max": 0,
                "played_lines": 0,
                "ok": 0,
                "len": -1,
            },
        )
        entry["lines"] = int(entry["lines"]) + 1

        try:
            reached = int(event.get("n", 0))
        except (TypeError, ValueError):
            reached = 0
        try:
            length = int(event.get("len", -1))
        except (TypeError, ValueError):
            length = -1
        try:
            ok = int(event.get("ok", 0))
        except (TypeError, ValueError):
            ok = 0

        # h = golpes que conectaron en la cadena, la profundidad REAL. Un archivo viejo no lo
        # trae: se deriva de n + 1 y se avisa al pie. En un combo ramificado n salta (0→6), así
        # que el derivado es un PISO de la profundidad, no una medición.
        raw_hits = event.get("h")
        if raw_hits is None:
            hits = max(0, reached) + 1
            derived_hits += 1
        else:
            try:
                hits = int(raw_hits)
            except (TypeError, ValueError):
                hits = max(0, reached) + 1
                derived_hits += 1
            else:
                measured_hits += 1

        # p/ph = profundidad JUGADA (lo que salió, tocara o no). Es la mitad que faltaba: una
        # cadena 0→1→2 con un solo impacto sale h=1, ph=3 y la diferencia es el whiff. Un archivo
        # viejo (sesión anterior a este campo) no los trae: se muestra `—`, no un 0 inventado.
        raw_played = event.get("ph")
        if raw_played is not None:
            try:
                played = int(raw_played)
            except (TypeError, ValueError):
                played = None

            if played is not None:
                played_lines += 1
                entry["played"] = int(entry["played"]) + played
                entry["played_max"] = max(int(entry["played_max"]), played)
                entry["played_lines"] = int(entry["played_lines"]) + 1
                paired_hits += hits
                paired_played += played
            else:
                unplayed_lines += 1
        else:
            unplayed_lines += 1

        entry["hits"] = int(entry["hits"]) + hits
        entry["hits_max"] = max(int(entry["hits_max"]), hits)
        entry["reached_max"] = max(int(entry["reached_max"]), reached)
        entry["ok"] = int(entry["ok"]) + (1 if ok else 0)
        entry["len"] = max(int(entry["len"]), length)

        room = index.at(event_t(event))
        room_entry = per_room.setdefault(room, {})
        room_entry[key] = room_entry.get(key, 0) + 1

    header = "{:<20}{:>9}{:>12}{:>11}{:>13}{:>13}{:>10}{:>9}{:>8}".format(
        "COMBO", "INTENTOS", "GOLPES PROM", "GOLPES MAX", "JUGADOS PROM", "JUGADOS MAX",
        "PASO MAX", "OK", "LARGO"
    )
    print(header)
    print("-" * len(header))

    for combo in sorted(per_combo):
        entry = per_combo[combo]
        lines = int(entry["lines"])
        average = (float(entry["hits"]) / lines) if lines else 0.0
        length = int(entry["len"])

        # Sólo hay promedio de pasos jugados si alguna cadena de ese combo trae `ph`; si no, la
        # columna es dato AUSENTE (`—`) y no un 0 que se leería como "no jugó nada".
        if int(entry["played_lines"]) > 0:
            played_average = "{:.2f}".format(float(entry["played"]) / int(entry["played_lines"]))
            played_max = str(int(entry["played_max"]))
        else:
            played_average = MISSING
            played_max = MISSING

        print("{:<20}{:>9}{:>12.2f}{:>11}{:>13}{:>13}{:>10}{:>9}{:>8}".format(
            combo,
            lines,
            average,
            int(entry["hits_max"]),
            played_average,
            played_max,
            int(entry["reached_max"]),
            int(entry["ok"]),
            "-" if length < 0 else length,
        ))

    print("-" * len(header))
    print("h = pasos que CONECTARON en la cadena (1, 2, 3…) · ph = pasos DISTINTOS JUGADOS "
          "(1, 2, 3…), tocaran o no")
    print("n = índice del paso que conectó · p = índice máximo JUGADO; en un combo ramificado "
          "el índice salta y no mide profundidad")
    print("PASO MAX se informa como referencia del asset · ok sólo aplica a combos lineales")
    print("len = largo esperado del combo · len '-' = ramificado o no resuelto: ahí ok es 0 siempre")

    # La lectura central de este reporte: sellar la diferencia entre lo que salió y lo que tocó.
    if played_lines:
        whiffed = paired_played - paired_hits
        print("diferencia GOLPES vs JUGADOS = pasos que salieron y no tocaron: "
              "{} jugados - {} conectados = {} whiff".format(paired_played, paired_hits, whiffed))
        print("h = 0 (con n = -1) = la cadena salió y no tocó NINGÚN golpe: es un intento medido, "
              "no un error")
        print("'?' = cadena sin nombre: el asset sólo se conoce cuando un golpe conecta")
    else:
        print("JUGADOS {} = sesión sin 'p'/'ph' (archivo viejo): los pasos jugados no se "
              "midieron".format(MISSING))
        print("en una sesión vieja sólo aparecen las cadenas que conectaron al menos un golpe: "
              "un whiff total no deja línea")
    if unplayed_lines and played_lines:
        print("JUGADOS parcial: {} de {} cadenas sin 'p'/'ph' (archivo mezclado) — "
              "esas filas quedan fuera de la diferencia".format(unplayed_lines, len(combos)))
    if derived_hits:
        print("GOLPES derivado de n + 1 en {} de {} cadenas (sesión sin 'h': archivo viejo) — "
              "es un piso, no una medición".format(derived_hits, derived_hits + measured_hits))
        print("ojo: un archivo viejo puede traer 'len' = cantidad de NODOS de un combo ramificado; "
              "no es el largo de la cadena")
    print()

    print("POR SALA")
    for room in sorted(per_room):
        summary = ", ".join(
            "{} x{}".format(combo, per_room[room][combo]) for combo in sorted(per_room[room])
        )
        print("  {:<16}{}".format(room, summary))


# --------------------------------------------------------------------------- entradas


INPUT_WINDOW_MS = 400

# Buckets del histograma de `in.ms`: ms transcurridos del paso en el momento de apretar.
MS_BUCKETS: Tuple[Tuple[int, Optional[int], str], ...] = (
    (0, 100, "0-100"),
    (100, 200, "100-200"),
    (200, 300, "200-300"),
    (300, 400, "300-400"),
    (400, None, "400+"),
)


def ms_bucket(ms: int) -> Optional[str]:
    """Bucket de `ms` (ms desde el arranque del paso). None = no medible (-1)."""
    if ms < 0:
        return None
    for low, high, label in MS_BUCKETS:
        if ms < low:
            continue
        if high is None or ms < high:
            return label
    return None


def _int_field(event: dict, key: str, default: int) -> int:
    try:
        return int(event.get(key, default))
    except (TypeError, ValueError):
        return default


def cmd_entradas(session: Session) -> None:
    """Cruza las pulsaciones (`in`) con el paso que provocaron (`step`).

    La pregunta que responde: ¿el press que hice dentro del paso N encadenó o se cayó? El
    criterio es el del archivo, sin relojes inventados: un `in` encadena si el PRIMER `step`
    posterior está a 400 ms o menos y su `n` es MAYOR que el `step` del press.
    """
    print_header(session)

    if not session.has_level():
        print("el canal level no está en esta sesión: 'in' y 'step' se escriben SÓLO ahí")
        return

    events = sorted_by_t(session.level)
    inputs = [event for event in events if event.get("e") == "in"]
    steps = [event for event in events if event.get("e") == "step"]

    if not inputs:
        print("no hay líneas 'in' en esta sesión: la pulsación se empezó a grabar después")
        if steps:
            print("sí hay {} 'step': hubo combos, pero falta la pulsación que los causó".format(
                len(steps)))
        print("una sesión con 'in'/'step' se consigue jugando con el build nuevo "
              "(canal level, default)")
        return

    # Posición de cada `step` en el stream ordenado: el "siguiente" es el primero con índice
    # mayor. Con t iguales, 'in' va antes que 'step' (mismo orden en que los emite el probe).
    step_positions: List[int] = [
        index for index, event in enumerate(events) if event.get("e") == "step"
    ]
    step_times: List[int] = [_int_field(event, "t", 0) for event in steps]

    total = 0        # pulsaciones con un paso activo (entran en el juicio)
    chained = 0
    dropped = 0
    openers = 0      # step = -1: arranque de combo, no es encadenar
    opener_hit = 0
    unmatched_presses = 0
    chained_ms: List[int] = []
    chained_delay: List[int] = []

    buckets: Dict[str, List[int]] = {label: [0, 0] for _, _, label in MS_BUCKETS}
    unmeasured = 0
    per_room: Dict[str, List[int]] = {}   # [puls, encad, cayó, apert]
    per_combo: Dict[str, List[int]] = {}  # [puls, encad, cayó]

    combo_now = "?"
    step_cursor = 0

    for index, event in enumerate(events):
        name = event.get("e")

        if name == "step":
            step_n = _int_field(event, "n", -1)
            step_combo = str(event.get("c", "?") or "?")
            combo_now = step_combo if step_n >= 0 and step_combo != "?" else "?"
            step_cursor += 1
            continue

        if name != "in":
            continue

        t = _int_field(event, "t", 0)
        step_now = _int_field(event, "step", -1)
        ms = _int_field(event, "ms", -1)
        room = str(event.get("r", NO_ROOM))

        # Primer `step` posterior dentro de la ventana.
        while step_cursor < len(step_positions) and step_positions[step_cursor] <= index:
            step_cursor += 1
        nxt: Optional[dict] = None
        if step_cursor < len(step_positions):
            candidate = steps[step_cursor]
            if _int_field(candidate, "t", 0) - t <= INPUT_WINDOW_MS:
                nxt = candidate

        row = per_room.setdefault(room, [0, 0, 0, 0])

        if step_now < 0:
            openers += 1
            row[3] += 1
            if nxt is not None and _int_field(nxt, "n", -1) >= 0:
                opener_hit += 1
            continue

        total += 1
        row[0] += 1

        combo_entry = per_combo.setdefault(combo_now, [0, 0, 0])
        combo_entry[0] += 1

        if nxt is not None and _int_field(nxt, "n", -1) > step_now:
            chained += 1
            row[1] += 1
            combo_entry[1] += 1
            chained_ms.append(ms)
            chained_delay.append(_int_field(nxt, "t", 0) - t)
            label = ms_bucket(ms)
            if label is None:
                unmeasured += 1
            else:
                buckets[label][0] += 1
        else:
            dropped += 1
            row[2] += 1
            combo_entry[2] += 1
            if nxt is None:
                unmatched_presses += 1
            label = ms_bucket(ms)
            if label is None:
                unmeasured += 1
            else:
                buckets[label][1] += 1

    closed = sum(1 for event in steps if _int_field(event, "n", -1) < 0)
    started = len(steps) - closed

    percent = (100.0 * chained / total) if total else 0.0
    print("PULSACIONES: {} · en combo: {} · aperturas: {}".format(
        len(inputs), total, openers))
    print("encadenaron: {} ({:.1f} %) · se cayeron: {} ({:.1f} %)".format(
        chained, percent, dropped, 100.0 - percent if total else 0.0))
    print("step: {} transiciones ({} arranques/avances con n >= 0 · {} cierres a -1)".format(
        len(steps), started, closed))
    if openers and not opener_hit:
        print("aperturas sin step en 400 ms: {} (el press no arrancó ningún paso)".format(
            openers))
    if unmatched_presses and not steps:
        print("no hay ninguna línea 'step' en esta sesión: sin cursor grabado no hay forma de "
              "saber si encadenó")
    print()

    header = "{:<10}{:>11}{:>9}{:>9}{:>9}".format("MS", "ENCADENÓ", "CAYÓ", "TOTAL", "%ENC")
    print(header)
    print("-" * len(header))
    for _, _, label in MS_BUCKETS:
        good, bad = buckets[label]
        bucket_total = good + bad
        ratio = (100.0 * good / bucket_total) if bucket_total else 0.0
        print("{:<10}{:>11}{:>9}{:>9}{:>9.1f}".format(
            label, good, bad, bucket_total, ratio))
    print("-" * len(header))

    measured_buckets = [
        (good, good + bad, label) for label, (good, bad) in buckets.items() if good + bad
    ]
    if chained_ms:
        print("VENTANA EMPÍRICA: los presses que encadenaron van de {} a {} ms del paso "
              "(n = {})".format(min(chained_ms), max(chained_ms), len(chained_ms)))
        if chained_delay:
            ordered = sorted(chained_delay)
            median = ordered[len(ordered) // 2]
            print("desde el press hasta el step: mín {} ms · mediana {} ms · máx {} ms".format(
                ordered[0], median, ordered[-1]))
        if measured_buckets:
            best = max(measured_buckets, key=lambda row: (row[0], -row[1]))
            print("mejor bucket: {} ms ({} de {} presses encadenaron, {:.1f} %)".format(
                best[2], best[0], best[1], 100.0 * best[0] / best[1]))
    else:
        print("VENTANA EMPÍRICA: ningún press con paso activo encadenó: no hay ventana que "
              "medir todavía")
    if unmeasured:
        print("{} presses sin 'ms' medible: no entran en el histograma".format(unmeasured))
    print()

    print("POR SALA")
    room_header = "{:<16}{:>11}{:>9}{:>9}{:>9}{:>8}".format(
        "SALA", "PULSACIONES", "ENCADENÓ", "CAYÓ", "%ENC", "APERT")
    print(room_header)
    print("-" * len(room_header))
    for room in sorted(per_room):
        puls, good, bad, apert = per_room[room]
        ratio = (100.0 * good / (good + bad)) if (good + bad) else 0.0
        print("{:<16}{:>11}{:>9}{:>9}{:>9.1f}{:>8}".format(
            room, puls, good, bad, ratio, apert))
    print()

    print("POR COMBO (el combo que el jugador estaba jugando al apretar)")
    combo_header = "{:<20}{:>11}{:>9}{:>9}{:>9}".format(
        "COMBO", "PULSACIONES", "ENCADENÓ", "CAYÓ", "%ENC")
    print(combo_header)
    print("-" * len(combo_header))
    for combo in sorted(per_combo):
        puls, good, bad = per_combo[combo]
        ratio = (100.0 * good / puls) if puls else 0.0
        print("{:<20}{:>11}{:>9}{:>9}{:>9.1f}".format(combo, puls, good, bad, ratio))
    print("-" * len(combo_header))
    print("in = una línea por pulsación (k = light|heavy|both) con el step vigente y los ms "
          "desde que empezó")
    print("step = cada cambio de CurrentStepIndex (n = 0 arranca, n = -1 cierra)")
    print("encadenó = el PRIMER 'step' a <= {} ms del press tiene n > step del press".format(
        INPUT_WINDOW_MS))
    print("la ventana de cancel NO se asume: se mide con el histograma de arriba")
    print("aperturas (step = -1) no entran en el %: arrancar un combo no es encadenar")
    print("'?' = el nombre del combo sólo se conoce cuando un golpe conecta")


# --------------------------------------------------------------------------- estela


def svg_color(ratio: float) -> str:
    """Azul (inicio) → ámbar → rojo (fin). Sin mapa de calor: sólo progreso temporal."""
    hue = 0.58 * (1.0 - max(0.0, min(1.0, ratio)))
    red, green, blue = colorsys.hsv_to_rgb(hue, 0.72, 0.98)
    return "#{:02x}{:02x}{:02x}".format(int(red * 255), int(green * 255), int(blue * 255))


def cmd_estela(session: Session, out_path: Optional[str]) -> None:
    samples = sorted_by_t([e for e in session.level if e.get("e") == "p"])
    if len(samples) < 2:
        raise SystemExit("no hay muestras 'p' suficientes para dibujar la estela")

    points = []
    for event in samples:
        try:
            x = float(event.get("x", 0.0))
            z = float(event.get("z", 0.0))
        except (TypeError, ValueError):
            continue
        points.append((event_t(event), x, z, str(event.get("r", NO_ROOM))))

    if len(points) < 2:
        raise SystemExit("las muestras no tienen coordenadas utilizables")

    width = 1200.0
    height = 1200.0
    padding = 60.0

    xs = [p[1] for p in points]
    zs = [p[2] for p in points]
    min_x, max_x = min(xs), max(xs)
    min_z, max_z = min(zs), max(zs)
    span_x = max(0.001, max_x - min_x)
    span_z = max(0.001, max_z - min_z)
    scale = min((width - 2 * padding) / span_x, (height - 2 * padding) / span_z)

    first_t = points[0][0]
    last_t = points[-1][0]
    span_t = max(1, last_t - first_t)

    def project(x: float, z: float) -> Tuple[float, float]:
        px = padding + (x - min_x) * scale + ((width - 2 * padding) - span_x * scale) / 2.0
        py = padding + (max_z - z) * scale + ((height - 2 * padding) - span_z * scale) / 2.0
        return px, py

    segments: List[str] = []
    visits: Dict[str, int] = {}

    for index in range(len(points) - 1):
        t, x, z, room = points[index]
        next_t, next_x, next_z, next_room = points[index + 1]

        visits[room] = visits.get(room, 0) + 1
        if next_room != room:
            continue

        x1, y1 = project(x, z)
        x2, y2 = project(next_x, next_z)
        ratio = (t - first_t) / span_t
        segments.append(
            '<line x1="{:.2f}" y1="{:.2f}" x2="{:.2f}" y2="{:.2f}" stroke="{}"/>'.format(
                x1, y1, x2, y2, svg_color(ratio)
            )
        )

    legend: List[str] = []
    for row, room in enumerate(sorted(visits)):
        legend.append(
            '<text x="{:.0f}" y="{:.0f}">{:>16}  {} muestras</text>'.format(
                40.0, 40.0 + row * 22.0, room, visits[room]
            )
        )

    gradient_start = svg_color(0.0)
    gradient_end = svg_color(1.0)

    svg = [
        '<svg xmlns="http://www.w3.org/2000/svg" width="1200" height="1200" '
        'viewBox="0 0 1200 1200">',
        '<rect width="1200" height="1200" fill="#11131a"/>',
        '<g stroke-width="2" stroke-linecap="round" fill="none">',
    ]
    svg.extend(segments)
    svg.append("</g>")
    svg.append(
        '<g font-family="monospace" font-size="16" fill="#e6e8ee">'
    )
    svg.append(
        '<text x="40" y="{:.0f}">sesión {} · {} muestras · duración {}</text>'.format(
            height - 70.0, session.prefix, len(points), clock(span_t / 1000.0)
        )
    )
    svg.append(
        '<text x="40" y="{:.0f}" fill="{}">inicio</text>'.format(height - 44.0, gradient_start)
    )
    svg.append(
        '<text x="140" y="{:.0f}" fill="{}">fin</text>'.format(height - 44.0, gradient_end)
    )
    svg.extend(legend)
    svg.append("</g>")
    svg.append("</svg>")

    target = out_path or os.path.join(session.folder, session.prefix + ".estela.svg")
    target = os.path.normpath(target)
    with open(target, "w", encoding="utf-8", newline="\n") as handle:
        handle.write("\n".join(svg))
        handle.write("\n")

    print("estela escrita: " + target)
    print("muestras: {} · salas: {}".format(len(points), len(visits)))


# --------------------------------------------------------------------------- main


COMMANDS = ("resumen", "embudo", "combos", "entradas", "estela")


def _command_first(argv: Sequence[str]) -> list[str]:
    """Acepta `playtest.py <cmd> <path>` además de `playtest.py <path> <cmd>`.

    La convención de CLI pone el verbo primero (git log, docker ps). El orden
    original del script era path-first, que es contraintuitivo y ya causó un
    error de uso real: acá se normaliza sin romper la forma vieja.
    """
    args = list(argv)
    if len(args) >= 2 and args[0] in COMMANDS and args[1] not in COMMANDS:
        args[0], args[1] = args[1], args[0]
    return args


def main(argv: Sequence[str]) -> int:
    # UTF-8 en stdout sin excepción: la tabla usa Δ y el archivo de una sesión es UTF-8.
    # Sin esto, una consola cp1252 (Windows en español) rompe con UnicodeEncodeError.
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")

    parser = argparse.ArgumentParser(
        prog="playtest.py",
        description="Resumen, embudo, combos y estela de una sesión de playtest.",
    )
    parser.add_argument("path", help="archivo .jsonl de la sesión o carpeta que los contiene")
    parser.add_argument("--session", default=None, help="prefijo de sesión (sesion-YYYYMMDD-HHmmss)")
    subparsers = parser.add_subparsers(dest="command", required=True)
    subparsers.add_parser("resumen", help="duración, muertes, distancia, marcas y reparto del tiempo")
    subparsers.add_parser("embudo", help="tabla por sala contra el presupuesto del doc 02")
    subparsers.add_parser("combos", help="cadenas del canal combat (cruce por sala con level)")
    subparsers.add_parser(
        "entradas", help="press vs step: qué pulsaciones encadenaron y en qué ms del paso"
    )
    estela = subparsers.add_parser("estela", help="SVG con la polilínea coloreada por tiempo")
    estela.add_argument("--out", default=None, help="ruta del SVG (default: junto a la sesión)")

    args = parser.parse_args(_command_first(argv))

    session = open_session(args.path, args.session)

    if args.command == "resumen":
        cmd_resumen(session)
    elif args.command == "embudo":
        cmd_embudo(session)
    elif args.command == "combos":
        cmd_combos(session)
    elif args.command == "entradas":
        cmd_entradas(session)
    elif args.command == "estela":
        cmd_estela(session, args.out)
    else:
        parser.error("comando desconocido")

    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
