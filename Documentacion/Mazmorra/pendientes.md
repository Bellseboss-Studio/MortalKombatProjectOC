# Pendientes

Lista viva. Lo que está acá **no está construido**, aunque esté especificado.

## Código

- [ ] **`CombatRoom`** — trigger de entrada → reja delantera → oleadas encadenadas → abrir.
      Es el único bloque nuevo de la arquitectura de salas. Ver `mazmorra-02-salas.md` §4.
      Estado: `EnemySpawner` hace una sola oleada; `ArenaGate` está cableado a `BossSpawner`.
- [x] **Telemetría de playtest** — entregada en el change `playtest-observability`
      (`PlaytestLog` + `tools/playtest.py`, validada con 4 sesiones reales).
      Ver `.atl/sdd/playtest-observability/archive-report.md`.
- [ ] **Guardar la escena con el rig de telemetría** — `MK/Level/Build Dungeon` deja `Dungeon.unity` sucia en
      memoria (el builder no llama a `SaveScene`): en disco no están `Telemetry` ni `Probe_*`, así que un
      checkout limpio no graba. Guardar la escena y verificar con `grep "m_Name: Probe_"`.
- [x] **Encadenado de combos roto** (`Combo_1.siguiente` vs `Stance_Mantis.rows`) — resuelto en `combo-grammar-neutral`:
      el press neutro resuelve primero contra el asset. Validado en juego: 186/401 presses encadenaron (46,4 %) y
      `Combo_1` llegó a 5 pasos jugados. Ver `.atl/sdd/combo-grammar-neutral/archive-report.md`.
- [ ] **Gesto del jugador en la palanca** — hoy usa el estado `LedgeGrab` como interino.
      Falta el clip real de accionar.

## Diseño

- [ ] **Rediseño de combos** — el encadenado ya funciona (`combo-grammar-neutral`), pero el combo no se siente
      completo. Tres causas medidas en `playtest/sesion-20261117-162127`:
      1. **Sin remate:** `Combo_1` ramifica, no tiene ruta lineal completa (`ok = 0`, `len = -1` siempre). La ruta de
         Ligero (`#0 → #1 → #2 → #6 → #7`) muere en una hoja: el jugador encadena y el juego nunca le confirma
         "combo terminado". Falta un golpe final/Closer que cierre la cadena.
      2. **Sin cancel-out:** una vez en el paso 2 quedan ~800 ms obligatorios (mediana 782 ms en 19 cadenas). No se
         puede ceder a defensa, movimiento ni a otro ataque: el jugador queda comprometido con la animación.
      3. **Distribución bimodal:** de 85 cadenas de `Combo_1`, 36 jugaron 1 paso y 31 jugaron 4; los largos de 2–3
         casi no existen (6 y 7). O no se entra en la ventana o la cadena se va hasta el tope del asset.
      Contexto: ventana empírica de cancel ≈ 0–300 ms (60 % encadena en 0–100); cadenas de mediana 0,52 s y p90 1,25 s.
      Ver `.atl/sdd/combo-grammar-neutral/archive-report.md` §"Hallazgos de diseño".
- [ ] Validar las medidas de las 15 salas caminando el greybox sin enemigos (`mazmorra-02-salas.md` §6).
- [ ] Definir el peso exacto del `Grabbable` de la placa (Sala Palanca 1).
- [ ] Tiempos de retracción de plataformas: arrancar en 4 s y medir con el reporte.

## Arte

- [ ] Las 4 fichas de ambiente de las referencias (cripta gris, santuario de lava) están sin modelar.
