# Mazmorra · Arquitectura de salas

> Capa que faltaba sobre `mazmorra-base-02`. El mapa dice **qué** hay; esto dice **cómo se comporta**,
> con qué medidas y cómo se verifica que funciona.
> Unidad: metros. Ejes: **X** lateral, **Y** altura, **Z** avance.
> Todas las medidas son **luz libre** entre paramentos, no entre ejes.

---

## 1. Las 7 reglas duras

1. **Primero se lee, después se pelea.** La reja no baja hasta 1.5 s después de que el jugador
   cruzó el umbral, y sólo si ve la sala. *Fundamento: affordances y sightlines (Totten, An
   Architectural Approach to Level Design).*
2. **Nunca dos locks.** Se cierra el paso hacia adelante; la retirada queda abierta. Sólo 3 salas
   encierran por los dos lados, y sólo porque tienen cobertura para romper la línea de carga.
3. **Los primeros 60 s no encierran a nadie.** La llegada enseña, no evalúa.
4. **Una sala, una lección.** Si enseña dos verbos distintos, se parte.
5. **El techo es piso.** Altura libre ≥ 4.5 m en combate, ≥ 3.2 m en pasillo. Con salto de 2.0 m,
   menos que eso convierte un salto en un cabezazo.
6. **Ningún foso mata sin avisar.** Todo borde con muerte lleva cambio de material + baranda de
   0.4 m o una fuente de luz al ras. `VoidReset` existe, pero el aviso es diseño, no código.
7. **El checkpoint está antes de la sala, nunca dentro.** Morir dentro de una sala ya es castigo;
   reaparecer rodeado es ensañamiento.

---

## 2. Qué medimos (y con qué umbral)

Cada beat se valida con cinco números. Sin esto, "quedó divertido" no es un dato.

| Métrica | Cómo se lee | Umbral |
|---|---|---|
| **Tiempo** | del trigger de entrada a la apertura de la reja | ±20% del presupuesto del mapa |
| **Daño** | % de vida perdido en el beat | ≤ 30% en salas normales, ≤ 50% en jefes |
| **Muertes** | `PlayerDamage.OnDied` dentro de la sección | ≤ 1 por beat en demo |
| **Verbo** | ¿se usó la mecánica que la sala enseña? | binario, obligatorio |
| **Duda** | tiempo quieto (>3 s) y miradas a la salida | ≤ 5 s por sala |

**El verbo es la métrica que importa.** Si el jugador limpia la Sala de las Lanzas sin usar el dash,
la sala falló aunque haya tardado lo previsto: el foso de 2 m era demasiado angosto.

---

## 3. Cómo se activan los enemigos

Tres figuras, tres propósitos distintos. Confundirlas es lo que hace que "3 enemigos" no signifique nada.

| Figura | Cuándo existe | Para qué sirve |
|---|---|---|
| **Guarnición** | está en la escena desde el arranque, quieta | Ambientar: la sala se lee como ocupada antes de pelear |
| **Oleada** | aparece cuando la anterior murió | Escalonar la presión sin llenar la pantalla |
| **Refuerzo** | aparece si el jugador se demora (>25 s) | Castigar el pasmo. Nunca es obligatorio |

**Restricción real del build:** `EnemyController` aggrea por `definition.aggroRadius`, **sin línea de
vista ni cono**. No se puede tener una guarnición que te mire de frente y no reaccione a 6 m.
Consecuencias:
- La guarnición se coloca **fuera del radio de aggro** hasta que el jugador entra, o no se coloca.
- Lo que tiene que aparecer "de la nada" es **oleada**, no guarnición.
- El radio de aggro define el tamaño útil de la sala: si es 8 m, la sala de 16 m se "despierta" desde el centro.

### Ciclo de un contenedor de combate

```
[Trigger de entrada] ──> [0.5 s: ¿ve la sala?] ──> [Baja reja delantera]
                                                        │
                          [Oleada 1] <──────────────────┘
                               │
                     (todos muertos)
                               │
                     [Oleada 2] ──> (todos muertos) ──> [Sube reja] ──> [Checkpoint siguiente]
                               │
                    (queda 1 vivo y pasan 60 s) ──> [Refuerzo]
```

---

## 4. Qué existe y qué falta

| Pieza | Estado | Nota |
|---|---|---|
| `EnemySpawner` (prefab + spawnPoints + initialCount) | ✅ | **Una sola oleada.** No encadena. |
| `ArenaGate` | ⚠️ | Funciona, pero está cableado a `BossSpawner` |
| `Checkpoint` / `CheckpointSystem` / `SectionReset` | ✅ | Muerte y reset de sección resueltos |
| `VoidReset` | ✅ | Fosos |
| `CameraZone` (Follow, priority, blend) | ✅ | Una zona por sala |
| `LeverInteractable` / `DungeonObjective` / `GreatDoor` | ✅ | 2 palancas |
| `HazardZone` | ✅ | Púas |
| **`CombatRoom`** | ❌ **falta** | Trigger → reja → oleadas → abrir |

`CombatRoom` es el único bloque nuevo y es chico:

```
[SerializeField] EnemySpawner[] waves;      // una por oleada, en orden
[SerializeField] Collider[] gates;          // slab que baja
[SerializeField] float readDelay = 1.5f;
[SerializeField] bool lockBothSides = false;
[SerializeField] float reinforcementAfter = 25f;
[SerializeField] EnemySpawner reinforcements;
```

Al morir el jugador, `SectionReset` ya resetea los spawners: `CombatRoom` sólo tiene que volver a
cerrar la reja y reiniciar el índice de oleada.

---

## 5. Fichas de sala

Formato: **[planta ancho × profundidad × alto]** · entradas · lock · enemigos · verbo · medidas.
Las coordenadas de spawn son relativas al centro de la sala, en (X, Z).

### 5.1 LLEGADA — ala Sur · *prueba diagnóstica, no combate*
**14 × 10 × 6** · 1 entrada (oeste, spawn), 1 salida (norte) · **sin lock**

- **Objetivo:** verificar los tres verbos base antes del nodo: avanzar, saltar, agarrar. No evalúa.
- **Enemigos:** guarnición de **2 Esbirro** (no 3). Puntos: `(-2, +3)` y `(+3, +6)`.
  El tercero es **refuerzo** a los 25 s en `(0, +7)`, para el jugador que se queda mirando.
- **Activación:** por radio de aggro, sin trigger. Están en el camino, se ven al entrar.
- **Lock:** no. *Regla 3.*
- **Checkpoint:** al cruzar la entrada (`sectionId 1`).
- **Cámara:** zona Follow propia, 0°.
- **Verbo:** agarrar. Hay un `Grabbable` (urna) al costado a 2 m de la entrada, opcional.
- **Mide:** limpieza ≤ 20 s · daño ≤ 20% · ¿usó grab? (sí/no).
- **Riesgo:** si el jugador llega con vida baja desde otro sitio, esta sala no perdona. Es la única
  sin reja justamente para que pueda retirarse.

### 5.2 VESTÍBULO — *primer contenedor de combate*
**18 × 12 × 7** · 1 entrada (oeste), 1 salida (este) · **lock delantero**

- **Objetivo:** instalar la gramática: *la puerta de adelante se cierra, la de atrás no*.
- **Enemigos:** **1 oleada de 3** (2 Esbirro + 1 Punalada). Spawns: `(-4, -4)`, `(+2, +4)`, `(+6, 0)`.
- **Activación:** trigger de 4 × 3 en el centro. Reja a los **1.5 s** (regla 1).
- **Lock:** sólo la salida este (slab 4.2 × 6). La entrada oeste queda abierta.
- **Checkpoint:** antes, en el umbral oeste.
- **Verbo:** leer el espacio. El Punalada entra por detrás del pilar: hay que girar.
- **Mide:** ≤ 45 s · daño ≤ 30% · **% de jugadores que retroceden a la entrada** (si > 50%, el lock
  está demasiado cerca del spawn y hay que correr el trigger 3 m).
- **Riesgo:** es la primera vez que se cierra una puerta. Si el jugador no entiende por qué, la
  culpa no es del jugador.

### 5.3 NODO 3 — *landmark y decisión*
**28 × 28 × 8** · 4 vanos (S, N, E, O) · sin lock

- **Objetivo:** orientar sin texto. Obelisco partido + 4 pilares: dos alas visibles desde el centro.
- **Enemigos:** **ninguno. Jamás.** *Regla 4: es lectura.*
- **Checkpoint:** al entrar por el sur.
- **Cámara:** Follow con prioridad alta al centro; las alas tienen su propia zona.
- **Mide:** decisión ≤ 20 s · ¿miró las dos alas antes de elegir? (trigger de mirada de 6 × 6 frente
  a cada vano).

### 5.4 PATIO DE ARMAS — ala Este · *combate en campo abierto*
**16 × 16 × 7** · 2 entradas (S, N) · **lock de los dos lados**

- **Objetivo:** enseñar cobertura. 4 pilares de 1.5 m para romper la línea de carga del Embestidor.
- **Enemigos:** **2 oleadas.**
  - O1: 2 Esbirro `(-5, -3)`, `(+5, +3)` + 1 Embestidor `(+5, -5)`.
  - O2 (>5 s de margen): 1 Punalada `(-5, +5)`.
- **Lock:** ambos vanos. Es la 1ª sala que encierra del todo, y se permite porque hay cobertura.
- **Verbo:** usar el pilar. Si el jugador come la carga de frente, la sala no enseñó.
- **Mide:** ≤ 45 s · daño ≤ 30% · ¿usó un pilar como cobertura? (distancia al pilar < 1.2 m durante
  el Charge del Embestidor).

### 5.5 CORREDOR DE LOS ECOS — *presión sin espacio*
**10 × 4 × 5** · pasillo recto · sin lock (el corredor *es* el lock)

- **Objetivo:** obligar el dash con i-frames. 4 m de ancho: no se puede rodear.
- **Enemigos:** guarnición de **2**: 1 Punalada al frente `(0, +2)` y 1 **Tirador en repisa a 3 m**
  `(0, +8)` — fuera del alcance de melee, visible desde la entrada.
- **Verbo:** dash a través del ataque del Punalada.
- **Mide:** ≤ 30 s · ¿usó dash con enemigo atacando? Si no, sacar al Tirador y volver a medir.

### 5.6 GALERÍA — *verticalidad*
**18 × 18 × 8** · 2 entradas (S, N) · **lock delantero**

- **Objetivo:** pelear en dos alturas. Dais central de 5 × 5 × 0.8 y anillo de 6 columnas.
- **Enemigos:** **2 oleadas.**
  - O1: 2 Esbirro abajo `(-6, 0)`, `(+6, 0)`.
  - O2: 2 **Tirador sobre columnas a 4 m** `(-6.5, +4)`, `(+6.5, -4)`.
- **Lock:** salida norte.
- **Verbo:** subir. El Tirador de arriba sólo se contesta en vertical.
- **Mide:** ≤ 45 s · ¿subió al dais o a una columna al menos una vez?

### 5.7 SALA PALANCA 2 — ala Este · *objetivo*
**16 × 16 × 8** · 1 entrada (S) · **lock hasta la palanca**

- **Objetivo:** cerrar el ala. Dais central 4 × 4 × 3.0 con la palanca arriba: 3 m = un mantle o una
  escalera de 2 peldaños.
- **Enemigos:** **1 oleada de 3** (1 Embestidor + 2 Punalada) al entrar. Después, silencio.
- **Lock:** la reja baja al entrar y **no sube al limpiar**: sube al accionar la palanca. La palanca es
  la llave, no la recompensa.
- **Verbo:** el dais. Subir 3 m con el combate encima.
- **Mide:** ≤ 30 s de combate + ≤ 10 s de interacción · daño ≤ 30%.
- **Atajo:** al accionar, se abre también la reja de la entrada → vuelta corta al nodo (`0:25` del mapa).

### 5.8 SALA DE LAS LANZAS — ala Oeste · *enseñar el dash*
**14 × 14 × 6** · foso de **2.0 m de ancho**, 2.5 m de profundidad (no mata: `Hazard_SoftPit`)

- **Objetivo:** el dash como salto horizontal. Foso angosto a propósito: falla y se sale por el escalón.
- **Enemigos:** guarnición de **2 Esbirro**, uno a cada lado del foso `(-4.5, 0)`, `(+4.5, 0)`.
- **Verbo:** dash. Sin lock, sin castigo.
- **Mide:** ≤ 30 s · **% que cruza con dash** (si > 30% cruza saltando, el foso mide de más: bajar a 1.8 m
  o subir el borde).

### 5.9 CORREDOR DEL ABISMO — *traversal con castigo*
**14 × 5 × 6** · 2 fosos de **2.5 m** sobre pozo de muerte (`Hazard_KillPit`)

- **Objetivo:** el mismo verbo, ahora sin red. Dos saltos encadenados.
- **Enemigos:** guarnición de **2**: 1 **Tirador en repisa a 3 m** `(0, +6)` y 1 Punalada después del
  segundo foso `(0, +11)`.
- **Verbo:** dash con presión a distancia.
- **Mide:** ≤ 45 s · muertes ≤ 1 · ¿el jugador cae en el mismo foso dos veces? (si sí, el salto es
  demasiado justo: 2.5 m es el techo, no el piso).
- **Aviso:** baranda baja o luz al ras en los dos bordes *(regla 6)*.

### 5.10 POZO DE LOS ESTRANGULADORES — *vertical*
**16 × 16 × 10** · foso central 8 × 10, 3 repisas a 2.0 / 3.5 / 5.0 m

- **Objetivo:** ledge grab y wall jump en un espacio que se lee de abajo hacia arriba.
- **Enemigos:** guarnición de **2**: 1 Esbirro en el fondo `(0, -3)` y 1 Tirador en la repisa de 5 m
  `(+2, +4)`.
- **Verbo:** colgarse. Las repisas están a 1.5 m entre sí: salto, no mantle. A 3.5 m hay que colgarse.
- **Mide:** ≤ 45 s · ¿usó ledge grab? · ¿subió sin caer al foso?

### 5.11 SALA PALANCA 1 — ala Oeste · *puzzle + combate*
**22 × 14 × 8** · 1 entrada (N) · **lock hasta la palanca**

- **Objetivo:** el remate del ala. **Placa de peso** (2 × 2, al ras) + **foso de 14 m** con
  **5 plataformas de 2 × 2** que emergen desde el piso.
- **Secuencia:** poner un `Grabbable` pesado en la placa (o pararse 2 s) → las plataformas suben con
  **1.5 s de separación** y se retraen a los **4 s** de pisada. Encadenar las 5.
- **Enemigos:** **2 Embestidores** en la plataforma de llegada `(-8, 0)`, `(-6, +3)`. Con las
  plataformas retrayéndose, la carga es una amenaza de tiempo, no de espacio.
- **Lock:** reja al entrar; se abre al accionar la palanca (igual que el ala Este).
- **Checkpoints:** antes de entrar **y** al cruzar el foso (para no repetir el puzzle tras morir en la palanca).
- **Verbo:** grabbable + timing.
- **Mide:** ≤ 30 s puzzle + ≤ 30 s combate · muertes en el foso ≤ 1 · ¿reintentó el puzzle más de 3 veces?
  (si sí, subir la ventana de retracción a 5 s).

### 5.12 GRAN PUERTA — *el objetivo*
**4 × 3.2** de vano · `GreatDoor` + `DungeonObjective(count = 2)`

- No hay enemigos. Cinemática `Cinematic_GreatDoorLocked` la primera vez, `GreatDoorOpen` al completar.
- **Mide:** ¿el jugador vuelve al nodo sin haber accionado las dos palancas? (si sí, el feedback de
  progreso no se está leyendo).

### 5.13 BOSS 1 — EL GUARDIÁN
**26 × 26 × 9** · 2 puertas (S, N) · `ArenaGate` + `BossEncounterTrigger`

- Ya implementado. Checkpoint en el pasillo de entrada, **nunca dentro** (regla 7).
- **Mide:** 1:30 · daño ≤ 50% · muertes ≤ 2.

### 5.14 DESCENSO — *respiro*
**10 × 4 × 4** · pasillo · sin enemigos, sin lock

- **Objetivo:** bajar pulsación después del jefe y antes del final. Cámara más lenta, luz que cambia de
  gris a naranja (anticipo del Santuario).
- **Mide:** 0:20. Si alguien muere acá, algo está muy mal.

### 5.15 BOSS 2 — OROCHI · SANTUARIO DE LAVA
**28 × 28 × 12** · `ArenaGate`

- Remate visual: huevo del dragón, 3-4 cultistas encapuchados de set dressing, lava en el perímetro.
- **Mide:** 2:00 · daño ≤ 50% · muertes ≤ 2.

---

## 6. Orden de construcción recomendado

1. `CombatRoom` (el único bloque nuevo) + una sala de prueba en el Vestíbulo.
2. Greybox con medidas de este documento **sin enemigos**: se camina y se mide el tiempo.
3. Enemigos por sala, una sala por vez, midiendo el verbo.
4. Ambiente y luces.
5. Cinemáticas y audio.

Construir el ambiente antes de validar el tiempo es el error clásico: se decora una sala que después
hay que mover 3 m.
