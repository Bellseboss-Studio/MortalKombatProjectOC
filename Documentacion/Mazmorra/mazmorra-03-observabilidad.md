# Mazmorra · Observabilidad de playtest

> Objetivo: que "se siente lento" se convierta en un número. Un log por jugador por sesión,
> que yo pueda leer y del que salgan decisiones de nivel.
> No es analytics de producto: es instrumentación de diseño.

---

## 1. El problema

Un jugador no puede distinguir entre:

- "el personaje se mueve lento" (feel: velocidad, animación, aceleración),
- "hay demasiado camino entre salas" (pacing: metros),
- "no sé para dónde ir" (legibilidad: orientación),
- "el combate no termina más" (balance: HP y ventanas).

Las cuatro se sienten igual desde el sillón. **El log existe para separarlas.**

Y algo que no se puede medir: *"¿es divertido?"*. Eso lo dice el jugador. Lo que sí se mide es
**dónde se aburre, dónde se traba y dónde se frustra**. Con eso alcanza.

---

## 2. Las tres capas

| Capa | Pregunta | Dato |
|---|---|---|
| **Posición** | ¿dónde estuvo? | muestreo de posición 4 Hz → mapa de calor |
| **Evento** | ¿qué pasó? | muertes, daño, verbos, palancas, checkpoints |
| **Veredicto** | ¿cuándo se frustró? | marca manual del jugador (una tecla) |

La tercera capa es la que casi nadie implementa y la que más sirve: **el jugador aprieta una tecla
en el momento exacto en que algo le molesta**, sin tener que explicarlo después. El reporte cruza esa
marca con los últimos 20 segundos de eventos.

---

## 3. Arquitectura: pasiva, no invasiva

`PlaytestLog` **no toca el gameplay**. Se suscribe a eventos que **ya existen**:

| Evento del log | Fuente que ya está en el build |
|---|---|
| `player.died` | `PlayerDamage.OnDied` |
| `player.damaged` | `PlayerDamage.OnDamaged` |
| `enemy.died` | `HitReceiver.AnyDied` (ya es `static`) |
| `flow` | `GameFlowController.StateChanged` |
| `objective` | `DungeonObjective.Progress` / `Completed` |
| `boss.phase` / `boss.died` | `BossController.PhaseChanged` / `Died` |
| `encounter` | `BossEncounterTrigger.Triggered` |
| `stance` | `PlayerStance.OnStanceChanged` |
| `checkpoint` | `Checkpoint.SectionId` al activarse |
| `room.enter` / `room.exit` | `RoomProbe` (trigger por sala, nuevo) |
| `pos` / `idle` / `verb` | `SessionProbe` (sondeo por frame, nuevo) |

Total: **2 componentes nuevos** (`RoomProbe`, `SessionProbe`) + **1 servicio** (`PlaytestLog`).
Cero cambios en `PlayerMotor`, `EnemyController` o `PlayerCombat`.

---

## 4. Formato: JSONL

Una línea = un evento. Append-only. Si el juego crashea, el archivo sirve igual hasta la última línea.

```
Logs/playtest/ana/ana-20261117-2130.jsonl
```

- **Editor:** `<Proyecto>/Logs/playtest/<jugador>/`
- **Build:** `Application.persistentDataPath/playtest/<jugador>/`
- **Jugador:** argumento `-player ana`; si no hay, `PlayerPrefs` con el último usado; si no hay, `anon`.

### Esquema (14 eventos)

| `e` | Campos | Cuándo |
|---|---|---|
| `session.start` | `p` jugador, `v` build, `sc` escena | al cargar |
| `session.end` | `dur`, `deaths`, `dist` | al salir |
| `p` | `t`, `r` sala, `x,y,z`, `v` velocidad | cada 0.25 s |
| `room.enter` | `t`, `r`, `from` | al cruzar un `RoomProbe` |
| `room.exit` | `t`, `r`, `to`, `dwell` | al salir |
| `idle` | `t`, `r`, `x,y,z`, `dur` | quieto > 3 s |
| `verb` | `t`, `r`, `k` dash/jump/grab/throw/mantle, `ok` | al ejecutarse |
| `player.damaged` | `t`, `r`, `dmg`, `hp`, `src` | al recibir |
| `player.died` | `t`, `r`, `cause`, `hp=0` | al morir |
| `player.respawn` | `t`, `r` | al revivir |
| `enemy.died` | `t`, `r`, `kind` | al morir |
| `room.clear` | `t`, `r`, `secs`, `hpLost` | al quedar la sala sin enemigos |
| `mark` | `t`, `r`, `txt` (opcional) | **F9 del jugador** |
| `flow` / `objective` / `boss` | `t`, `from→to`, `n/total` | eventos de sistema |

Un archivo de 12 minutos pesa ~150-250 KB. Nada.

### Seguridad de la instrumentación

- **Nunca** en `Update` con `string +=`. Buffer en memoria, `StreamWriter` y `Flush` cada 1 s.
- El log **no puede tirar el juego**: todo dentro de `try/catch`, y si falla una vez, se apaga solo.
- `-nolog` desactiva todo. Para jugar sin dejar rastro.

---

## 5. Qué saco yo de ahí

`Tools/playtest_report.py` lee la carpeta y escribe un `reporte.md` + `mapa.html`.

### Resumen

```
Sesión ana · 11:42 · 2 muertes · 1.4 km recorridos
  combate   42%   tránsito 38%   quieto 20%
```

Ese `quieto 20%` ya es un diagnóstico. Si en un shooter está bien, en una mazmorra de acción es
demasiado: probablemente no sabe para dónde ir.

### Embudo por sala

| Sala | Visitas | Tiempo medio | Presupuesto | Muertes | Daño | Quieto | Verbos |
|---|---|---|---|---|---|---|---|
| Vestíbulo | 1 | 1:42 | 1:00 | 1 | 64% | 12% | dash 3 |
| Galería | 1 | 0:38 | 0:45 | 0 | 18% | 2% | — |

La columna **Presupuesto** es la del mapa base 02. La diferencia es el veredicto.

### Mapa de calor (`mapa.html`)

Grilla de 1 × 1 m sobre la planta de cada sala, con densidad de muestras. Se abre en el navegador,
sin dependencias. Sirve para lo mismo que en un shooter:

- **zonas rojas donde no hay nada** = el jugador se quedó trabado en un lugar sin contenido,
- **pasillos en negro** = nadie pasó por ahí (el atajo no se descubre),
- **una esquina saturada** = ahí está la única cobertura real, y no fue intencional.

### Secuencia de salas

Grafo de transiciones con frecuencia. Si el 70% vuelve al nodo sin accionar la segunda palanca,
no es un problema de combate: es de señalización del objetivo.

### Alertas automáticas

| Regla | Lectura |
|---|---|
| dwell > 1.5× presupuesto | la sala está mal medida o mal explicada |
| idle > 15% del beat | no sabe qué hacer |
| muertes en el mismo punto 2× | el salto o la reja están injustos |
| verbo nunca usado en su sala | la sala no enseña nada |
| `mark` agrupadas en una sala | el jugador lo dijo: algo molesta ahí |

### El caso concreto

> "Se siente lento."

El reporte puede decir:

```
tránsito 61% · quieto 14% · combate 25%
Nodo→Sala Palanca 1: 4:10 de recorrido (presupuesto 2:30)
muertes 0 · daño 12% · verbo dash: 3 usos en 11 min
```

**No es feel. Es pacing.** El personaje no se mueve lento: hay 40% más de camino del que el mapa
decía, y el jugador no está usando el dash porque el foso de 2 m se cruza saltando. Dos acciones
distintas: acortar el ala y subir el borde del foso. Ninguna de las dos es tocar `runSpeed`.

Y si en cambio el reporte dijera `combate 70% · daño 80%`, la respuesta sería opuesta: no es el
camino, es el balance de la sala.

---

## 6. Cómo lo recibo

1. El jugador juega con `-player nombre`.
2. Al terminar, copia su carpeta `Logs/playtest/nombre/` (o el `.jsonl`).
3. Vos me pasás la carpeta; yo corro el reporte y te devuelvo el diagnóstico.

Para la demo alcanza con un Zip compartido. Si algún día hay muchos jugadores, esto se convierte en
un endpoint — pero el formato no cambia, así que el trabajo de hoy no se tira.

---

## 7. Costo

| Pieza | Tamaño | Riesgo |
|---|---|---|
| `PlaytestLog` | ~150 líneas | bajo: suscriptor pasivo |
| `RoomProbe` | ~40 líneas | bajo: un trigger por sala |
| `SessionProbe` | ~80 líneas | bajo: sondeo, sin estado de gameplay |
| `playtest_report.py` | ~250 líneas | ninguno: lee archivos |
| `mapa.html` | generado | — |

**Total: media jornada.** Y cambia el modo de trabajar: en vez de discutir si una sala es divertida,
se mira el número y después se discute la solución.
