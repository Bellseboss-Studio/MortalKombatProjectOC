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
- [ ] Encadenado de combos roto (Combo_1.siguiente vs Stance_Mantis.rows) — cambio aparte
- [ ] **Gesto del jugador en la palanca** — hoy usa el estado `LedgeGrab` como interino.
      Falta el clip real de accionar.

## Diseño

- [ ] Validar las medidas de las 15 salas caminando el greybox sin enemigos (`mazmorra-02-salas.md` §6).
- [ ] Definir el peso exacto del `Grabbable` de la placa (Sala Palanca 1).
- [ ] Tiempos de retracción de plataformas: arrancar en 4 s y medir con el reporte.

## Arte

- [ ] Las 4 fichas de ambiente de las referencias (cripta gris, santuario de lava) están sin modelar.
