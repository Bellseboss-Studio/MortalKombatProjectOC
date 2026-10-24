# Legacy reference audit — _MKRebuild

Read-only scan of every dependency of the `_MKRebuild` scenes and prefabs.
Any path listed here is still outside `_MKRebuild`; the baseline is expected to
fail before WU2-WU4 move the legacy art (the rebuild Leena prefab is the known case).

- Scanned scenes: 5
- Scanned prefabs: 31
- External dependencies: 30

## Counts by extension

| Extension | Count |
|---|---:|
| `.anim` | 2 |
| `.fbx` | 13 |
| `.mp3` | 3 |
| `.ogg` | 11 |
| `.wav` | 1 |

## Offending paths

| Dependency | Referenced by |
|---|---|
| `Assets/Audio/Music/Combat_Low.mp3` | Assets/_MKRebuild/Scenes/Playground.unity |
| `Assets/Audio/Music/Combat_medium_extended.mp3` | Assets/_MKRebuild/Scenes/Playground.unity |
| `Assets/Audio/Music/Mortal Kombat Project Oc - Soundtrack 5.mp3` | Assets/_MKRebuild/Scenes/Playground.unity |
| `Assets/Audio/Sfx/Characters/Footsteps/Dirt/Footsteps_Gravel_M_001.ogg` | Assets/_MKRebuild/Scenes/Playground.unity |
| `Assets/Audio/Sfx/Characters/Footsteps/Dirt/Footsteps_Gravel_M_002.ogg` | Assets/_MKRebuild/Scenes/Playground.unity |
| `Assets/Audio/Sfx/Characters/Footsteps/Dirt/Footsteps_Gravel_M_003.ogg` | Assets/_MKRebuild/Scenes/Playground.unity |
| `Assets/Audio/Sfx/Environment/Ambiences_Birds001_MainsScene_MortalKombat.ogg` | Assets/_MKRebuild/Scenes/Playground.unity |
| `Assets/Audio/Sfx/Environment/Ambiences_Birds002_MainsScene_MortalKombat.ogg` | Assets/_MKRebuild/Scenes/Playground.unity |
| `Assets/Audio/Sfx/Environment/Ambiences_Birds003_MainsScene_MortalKombat.ogg` | Assets/_MKRebuild/Scenes/Playground.unity |
| `Assets/Audio/Sfx/Tools/2_Pop.wav` | Assets/_MKRebuild/Scenes/Playground.unity |
| `Assets/Audio/Sfx/UI/Ambience/DarkWind/BaseAmbience_Wind006_UI_MortalKombat.ogg` | Assets/_MKRebuild/Scenes/Playground.unity |
| `Assets/Audio/Sfx/UI/Ambience/Thunder/Ambience_Thunder009_UI_MortalKombat.ogg` | Assets/_MKRebuild/Scenes/Playground.unity |
| `Assets/Audio/Sfx/UI/Ambience/Thunder/Ambience_Thunder010_UI_MortalKombat.ogg` | Assets/_MKRebuild/Scenes/Playground.unity |
| `Assets/Audio/Sfx/UI/Ambience/Wind/Ambiences_Wind001_MainsScene_MortalKombat.ogg` | Assets/_MKRebuild/Scenes/Playground.unity |
| `Assets/Audio/Sfx/UI/Ambience/Wind/Ambiences_Wind003_MainsScene_MortalKombat.ogg` | Assets/_MKRebuild/Scenes/Playground.unity |
| `Assets/_Animations/A_Characters/anim_Leena/anim_Leena_Damaged/anim_leena_Damaged_bounce_getup.anim` | Assets/_MKRebuild/Prefabs/Player.prefab<br>Assets/_MKRebuild/Scenes/Gym.unity<br>Assets/_MKRebuild/Scenes/Playground.unity |
| `Assets/_Animations/A_Characters/anim_Leena/anim_Leena_Damaged/anim_leena_Damaged_third_hit_spin.anim` | Assets/_MKRebuild/Prefabs/Player.prefab<br>Assets/_MKRebuild/Scenes/Gym.unity<br>Assets/_MKRebuild/Scenes/Playground.unity |
| `Assets/_Animations/mixiamo/X Bot@Cross Punch.fbx` | Assets/_MKRebuild/Prefabs/Boss/BossEncounter.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/Boss_Brute.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/Embestidor.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/Esbirro.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/Punalada.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/Tirador.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/XBotVisual.prefab<br>Assets/_MKRebuild/Prefabs/World/EnemySpawner.prefab<br>Assets/_MKRebuild/Scenes/Playground.unity |
| `Assets/_Animations/mixiamo/X Bot@Idle.fbx` | Assets/_MKRebuild/Prefabs/Boss/BossEncounter.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/Boss_Brute.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/Embestidor.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/Esbirro.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/Punalada.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/Tirador.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/XBotVisual.prefab<br>Assets/_MKRebuild/Prefabs/World/EnemySpawner.prefab<br>Assets/_MKRebuild/Scenes/Playground.unity |
| `Assets/_Animations/mixiamo/X Bot@Roar.fbx` | Assets/_MKRebuild/Prefabs/Boss/BossEncounter.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/Boss_Brute.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/Embestidor.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/Esbirro.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/Punalada.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/Tirador.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/XBotVisual.prefab<br>Assets/_MKRebuild/Prefabs/World/EnemySpawner.prefab<br>Assets/_MKRebuild/Scenes/Playground.unity |
| `Assets/_Animations/mixiamo/X Bot@Running.fbx` | Assets/_MKRebuild/Prefabs/Boss/BossEncounter.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/Boss_Brute.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/Embestidor.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/Esbirro.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/Punalada.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/Tirador.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/XBotVisual.prefab<br>Assets/_MKRebuild/Prefabs/World/EnemySpawner.prefab<br>Assets/_MKRebuild/Scenes/Playground.unity |
| `Assets/_Animations/mixiamo/X Bot@Sneak Walk.fbx` | Assets/_MKRebuild/Prefabs/Enemies/Punalada.prefab<br>Assets/_MKRebuild/Scenes/Playground.unity |
| `Assets/_Animations/mixiamo/X Bot@Stabbing.fbx` | Assets/_MKRebuild/Prefabs/Boss/BossEncounter.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/Boss_Brute.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/Embestidor.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/Esbirro.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/Punalada.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/Tirador.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/XBotVisual.prefab<br>Assets/_MKRebuild/Prefabs/World/EnemySpawner.prefab<br>Assets/_MKRebuild/Scenes/Playground.unity |
| `Assets/_Animations/mixiamo/X Bot@Stand Up.fbx` | Assets/_MKRebuild/Prefabs/Boss/BossEncounter.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/Boss_Brute.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/Embestidor.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/Esbirro.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/Punalada.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/Tirador.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/XBotVisual.prefab<br>Assets/_MKRebuild/Prefabs/World/EnemySpawner.prefab<br>Assets/_MKRebuild/Scenes/Playground.unity |
| `Assets/_Animations/mixiamo/X Bot@Standing Aim Idle 01.fbx` | Assets/_MKRebuild/Prefabs/Boss/BossEncounter.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/Boss_Brute.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/Embestidor.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/Esbirro.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/Punalada.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/Tirador.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/XBotVisual.prefab<br>Assets/_MKRebuild/Prefabs/World/EnemySpawner.prefab<br>Assets/_MKRebuild/Scenes/Playground.unity |
| `Assets/_Animations/mixiamo/X Bot@Standing Aim Recoil.fbx` | Assets/_MKRebuild/Prefabs/Boss/BossEncounter.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/Boss_Brute.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/Embestidor.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/Esbirro.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/Punalada.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/Tirador.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/XBotVisual.prefab<br>Assets/_MKRebuild/Prefabs/World/EnemySpawner.prefab<br>Assets/_MKRebuild/Scenes/Playground.unity |
| `Assets/_Animations/mixiamo/X Bot@Standing Death Right 01.fbx` | Assets/_MKRebuild/Prefabs/Boss/BossEncounter.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/Boss_Brute.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/Embestidor.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/Esbirro.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/Punalada.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/Tirador.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/XBotVisual.prefab<br>Assets/_MKRebuild/Prefabs/World/EnemySpawner.prefab<br>Assets/_MKRebuild/Scenes/Playground.unity |
| `Assets/_Animations/mixiamo/X Bot@T-Pose.fbx` | Assets/_MKRebuild/Prefabs/Boss/BossEncounter.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/Boss_Brute.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/Embestidor.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/Esbirro.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/Punalada.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/Tirador.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/XBotVisual.prefab<br>Assets/_MKRebuild/Prefabs/World/EnemySpawner.prefab<br>Assets/_MKRebuild/Scenes/Playground.unity |
| `Assets/_Animations/mixiamo/X Bot@Walking.fbx` | Assets/_MKRebuild/Prefabs/Boss/BossEncounter.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/Boss_Brute.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/Embestidor.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/Esbirro.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/Punalada.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/Tirador.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/XBotVisual.prefab<br>Assets/_MKRebuild/Prefabs/World/EnemySpawner.prefab<br>Assets/_MKRebuild/Scenes/Playground.unity |
| `Assets/_Animations/mixiamo/X Bot@Zombie Reaction Hit.fbx` | Assets/_MKRebuild/Prefabs/Boss/BossEncounter.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/Boss_Brute.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/Embestidor.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/Esbirro.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/Punalada.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/Tirador.prefab<br>Assets/_MKRebuild/Prefabs/Enemies/XBotVisual.prefab<br>Assets/_MKRebuild/Prefabs/World/EnemySpawner.prefab<br>Assets/_MKRebuild/Scenes/Playground.unity |
