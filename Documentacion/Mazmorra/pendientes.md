# Pendientes

Lista viva. Lo que está acá **no está construido**, aunque esté especificado.

## Código

- [ ] **`CombatRoom`** — trigger de entrada → reja delantera → oleadas encadenadas → abrir.
      Es el único bloque nuevo de la arquitectura de salas. Ver `mazmorra-02-salas.md` §4.
      Estado: `EnemySpawner` hace una sola oleada; `ArenaGate` está cableado a `BossSpawner`.
- [x] **Telemetría de playtest** — entregada en el change `playtest-observability`
      (`PlaytestLog` + `tools/playtest.py`, validada con 4 sesiones reales).
      Ver `.atl/sdd/playtest-observability/archive-report.md`.
- [x] **Guardar la escena con el rig de telemetría** — resuelto: el builder ahora hace `MarkSceneDirty` + `SaveOpenScenes`,
      `Dungeon.unity` tiene el rig `Telemetry` y los 10 `Probe_*` con su `roomId` y presupuesto, y está commiteado.
- [x] **Encadenado de combos roto** (`Combo_1.siguiente` vs `Stance_Mantis.rows`) — resuelto en `combo-grammar-neutral`:
      el press neutro resuelve primero contra el asset. Validado en juego: 186/401 presses encadenaron (46,4 %) y
      `Combo_1` llegó a 5 pasos jugados. Ver `.atl/sdd/combo-grammar-neutral/archive-report.md`.
- [ ] **Gesto del jugador en la palanca** — hoy usa el estado `LedgeGrab` como interino.
      Falta el clip real de accionar.

## Diseño

- [ ] **Rediseño de combos** — **implementado el 2026-11-19, pendiente de validar el empuje en gameplay.**
      El encadenado ya funciona (`combo-grammar-neutral`), pero el combo no se sentía completo. Tres causas medidas en
      `playtest/sesion-20261117-162127`:
      1. **Sin remate:** `Combo_1` ramifica, no tiene ruta lineal completa (`ok = 0`, `len = -1` siempre). La ruta de
         Ligero (`#0 → #1 → #2 → #6 → #7`) muere en una hoja: el jugador encadena y el juego nunca le confirma
         "combo terminado". Falta un golpe final/Closer que cierre la cadena.
      2. **Sin cancel-out:** una vez en el paso 2 quedan ~800 ms obligatorios (mediana 782 ms en 19 cadenas). No se
         puede ceder a defensa, movimiento ni a otro ataque: el jugador queda comprometido con la animación.
      3. **Distribución bimodal:** de 85 cadenas de `Combo_1`, 36 jugaron 1 paso y 31 jugaron 4; los largos de 2–3
         casi no existen (6 y 7). O no se entra en la ventana o la cadena se va hasta el tope del asset.
      Contexto: ventana empírica de cancel ≈ 0–300 ms (60 % encadena en 0–100); cadenas de mediana 0,52 s y p90 1,25 s.
      Ver `.atl/sdd/combo-grammar-neutral/archive-report.md` §"Hallazgos de diseño".
      **Piezas ya decididas**: el remate lleva knockback/launch (dato puro: `ComboStep.knockback/launch/launchImpulse`)
      y **stagger garantizado** en enemigos comunes (`EnemyController.Stagger()` es público y no depende del daño).
      `ComboRole.Closer` existe, da +15 % y hoy nunca se cobra: conectarlo es parte del rediseño.
      **Estado 2026-11-19 (sin commitear)**: `Combo_1` de 13 nodos a 3 (`L→L→P`), atajo `L→P`, 2 filas `Closer`
      con `role: 2`, `chainSlots` 6→4, Tarkatana 0.28/0.20, stagger movido **antes** del impulso, assets muertos
      borrados, evento `ender` en telemetría. Validado en log: `len=3` (115/115), `ok=1` (38 cadenas), `ph` hasta 3.
      **Falta**: probar el empuje contra un Embestidor en `Dungeon` (`ender.d > 0.5`). Los jefes no empujan por diseño.
- [ ] **Ventana de cancel: 65 % de las cadenas mueren en el paso 1** — tras el rediseño, 75 de 115 cadenas de
      `Combo_1` juegan un solo paso y solo 92 de 319 presses en combo encadenan (28,8 %). La ventana es 0,2 s
      (Mantis) / 0,28 s (Tarkatana) y los datos propios dicen que se encadena bien hasta ~300 ms. Números a mover:
      `connectWindowSeconds` / `whiffWindowSeconds` en las dos matrices.
- [ ] **Orochi como muro de dificultad** — en `playtest/sesion-20261117-152231`: 6 muertes en 95 s, 530 de daño
      recibido (12× el Guardián), 182 s contra 120 de presupuesto. El Guardián, con presupuesto menor, no mató
      ninguna vez. Es lo peor medido del juego y conviene atacarlo antes que el balance fino de combos.
- [ ] **Interrupción de jefes con el remate** — hoy `BossController` no hereda de `EnemyController` y **no tiene
      poise ni stagger**: sólo recibe daño (`OnDamaged`) y maneja `Invulnerable` + el rugido de fase. Para que un remate
      interrumpa a un jefe hace falta un sistema de poise por fase (resistencia, ventana de rotura, feedback y si
      Guardián y Orochi comparten la regla). **Fuera del rediseño de combos**: es un change de jefes.
      Guardado por pedido explícito del usuario el 2026-11-17.
- [ ] Validar las medidas de las 15 salas caminando el greybox sin enemigos (`mazmorra-02-salas.md` §6).
- [ ] Definir el peso exacto del `Grabbable` de la placa (Sala Palanca 1).
- [ ] Tiempos de retracción de plataformas: arrancar en 4 s y medir con el reporte.

## Arte

- [ ] Las 4 fichas de ambiente de las referencias (cripta gris, santuario de lava) están sin modelar.
