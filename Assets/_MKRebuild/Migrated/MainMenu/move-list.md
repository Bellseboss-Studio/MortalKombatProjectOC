# MainMenu move list — WU4a (curated)

Scope: **curate and move only**. No scene rebuild, no `ProjectSettings`, no `Packages`, no
`_MKRebuild/Scenes/*` edits. Every destination is under
`Assets/_MKRebuild/Migrated/MainMenu/`. Assets are moved with
`AssetDatabase.MoveAsset` so the `.meta`/GUID travel with the file.

Source of truth for the pre-move ledger: `Assets/_MKRebuild/Migrated/manifest.md` (152 rows:
111 `share`, 37 `replace`, 4 `drop`).

## Counts

| Class | Rows | Notes |
|---|---:|---|
| MOVE (this slice) | 78 | 78 manifest `share` rows; includes the 3 shader graphs under `Materials/` |
| MOVE (already done in WU3) | 2 | `F_PunkRocker.ttf`, `TIMELINE_intro01_Text.playable` |
| DROP (this slice) | 38 | 28 `share` + 10 `replace` reclassified |
| DROP (already in manifest) | 4 | FMOD/PlayFab rows already `drop` |
| REPLACE (untouched) | 27 | scripts, scenes, legacy canvas/scripts, bake |
| OUT OF SCOPE / pending | 3 | Intro00/Intro01 leftovers |
| **Total manifest rows** | **152** | 80 moved · 42 dropped · 27 replace · 3 pending |

After this slice the manifest should read:

| Class | Rows |
|---|---:|
| `share` → `moved` | 80 |
| `drop` → `dropped` | 42 |
| `replace` | 27 |
| pending (out of scope) | 3 |

## UI inspection (what the menu actually uses)

Read back by opening `Assets/_Scenes/MainMenu/mainMenu_.unity` additively with Unity MCP.

| Item | Finding | Decision |
|---|---|---|
| Text | **99 `TMP_Text` / 0 `UnityEngine.UI.Text`** | TMP asset closure is required |
| Fonts | `Name_Character_in_game` (constan SDF, TMP_SDF) and `LiberationSans SDF` (TMP_SDF-Mobile) + its fallback `LiberationSans SDF - Fallback - EXP PLAYER` | Move both font assets, the fallback, their TTFs and both TMP shaders |
| Button art | `Image` sprites `Play02`, `Options02`, `Extras02`, `Credits02`, `Quit02` (plus `*`, `*On` variants in the prefab) | Move all 15 PNGs |
| Logo art | 4 `Image`s using the outline graph materials (`Mortal-Kombat-Logos0 2`, `Name`, `Project Oc 1`, `Rectangle 4`) | Move the 4 PNGs + 4 `Shader Graphs_Outline_Sprite*.mat` |
| Background | `background.PNG` on the main-menu `Background` image | Move |
| Amuleto | `Group 5.png` on `Amuleto01` | Move |
| Environment | 1 Terrain + 2 terrain layers + Pyramid/Obelisk meshes + 7 flames + HDRI skybox (`sky_nublado.mat`) | Move environment closure |
| Camera | `CinemachineCamera` on `Camera`, shot vcam exposed ref `cfd661ef44127194ea35e2d1ae934037` | No asset to move (scene vcam) |
| Volumes | 2 `Volume` components, **both profiles null**; `CM vcam1 Profile.asset` is a **PPv2 `PostProcessProfile`** | Drop the PPv2 profile; nothing URP to move |
| Video | `VideoPlayer` exists but has **no clip and no URL**; `VideoMK.renderTexture` is unused legacy leftovers | Drop |
| Legacy panels | Options/Credits/Extras/Rebind UI + `CanvasMenu_pery` prefab use Icons, Dragons, `pixel.png`, panel backgrounds | Drop those art pieces |

## MOVE — 78 assets

Destination root: `Assets/_MKRebuild/Migrated/MainMenu/`.

### Audio (3)

| Class | Source | Destination | GUID | Reason |
|---|---|---|---|---|
| move | `Assets/Audio/Music/Music_MK_Menu_bpm80_4-4.ogg` | `Audio/Music_MK_Menu_bpm80_4-4.ogg` | `3c090518c4a80534a94548279dc32765` | `TL_Menu` AudioTrack (2) |
| move | `Assets/Audio/Sfx/UI/Ambience/Thunder/Ambience_Thunder006_UI_MortalKombat.ogg` | `Audio/Ambience_Thunder006_UI_MortalKombat.ogg` | `5466dca3a2aac4cecb554c173664cb3f` | `TL_Menu` AudioTrack (1) |
| move | `Assets/_Audio/Sound_PlaceHolder/Cinematic_sound/intro00_sound_pyramid.wav` | `Audio/intro00_sound_pyramid.wav` | `e8a71e5acad46bd48bbd116f6f073f43` | `TL_Menu` AudioTrack (0) |

### Models (3)

| Class | Source | Destination | GUID | Reason |
|---|---|---|---|---|
| move | `Assets/_Models/Characters_Models/Leena_Model.fbx` | `Models/Leena_Model.fbx` | `0b97a069269890d40a1f8760fae6e78d` | `Leena_Model.prefab` + scene SkinnedMeshes |
| move | `Assets/_Models/Environments/Edenian_Ruins/Pyramid.fbx` | `Models/Pyramid.fbx` | `4b4e34f9ea8852547bda0f46212e3e93` | Edenia flythrough |
| move | `Assets/_Models/Environments/Edenian_Ruins/Obelisk.fbx` | `Models/Obelisk.fbx` | `51129a765e51c4043982424e22f9442b` | Edenia flythrough |

### Animations (2)

| Class | Source | Destination | GUID | Reason |
|---|---|---|---|---|
| move | `Assets/_Animations/A_Cinematics/MainMenu_animations/Arm_Leena_menu.fbx` | `Animations/Arm_Leena_menu.fbx` | `6d402449976c4bc42ab212d1e54242b5` | `anim_Leena_menu_loop` used by `TL_Menu` track #14 |
| move | `Assets/_Animations/A_Cinematics/MainMenu_animations/Leena_menu.controller` | `Animations/Leena_menu.controller` | `b02eb4a1757e397478d15475474b7064` | Animator on scene `Leena_Model` |

### Materials and shader graphs (12)

| Class | Source | Destination | GUID | Reason |
|---|---|---|---|---|
| move | `Assets/Materials/Characters_Mat/Leena_Mat/M_Leena_Body.mat` | `Materials/M_Leena_Body.mat` | `85b316c853acf534cbe00a3344e0e01e` | Leena SkinnedMeshes |
| move | `Assets/Materials/Characters_Mat/Leena_Mat/M_Leena_Clothes_DoubleSide.mat` | `Materials/M_Leena_Clothes_DoubleSide.mat` | `a28c583cba37ad545a95ab85f27154be` | Leena clothes |
| move | `Assets/Materials/Environment_mat/Edenia_Ruins/Pyramid/M_PyramidBase.mat` | `Materials/M_PyramidBase.mat` | `7e9868339b6aea046ad7fb74618ca495` | Pyramid |
| move | `Assets/Materials/Environment_mat/Edenia_Ruins/Pyramid/M_PyramidTrim.mat` | `Materials/M_PyramidTrim.mat` | `0420a75eb236bdc49802871c022b0fff` | Obelisk + pyramid trim |
| move | `Assets/Shaders/shader_fire_C/Flame_B.mat` | `Materials/Flame_B.mat` | `1e472657d0e0fd541a7420f5e2ef79d5` | 7 scene flames |
| move | `Assets/Shaders/Shader Graphs_Outline_Sprite.mat` | `Materials/Shader Graphs_Outline_Sprite.mat` | `dedd9b7217e6ea54595a5dc752e451dc` | Logo image outline |
| move | `Assets/Shaders/Shader Graphs_Outline_SpriteArrow.mat` | `Materials/Shader Graphs_Outline_SpriteArrow.mat` | `5137c926ea6a16c4d8ffad7f542d258a` | Logo arrow outline |
| move | `Assets/Shaders/Shader Graphs_Outline_SpriteName.mat` | `Materials/Shader Graphs_Outline_SpriteName.mat` | `655978f453fdabd4c86291f2abec9795` | `Name` logo outline |
| move | `Assets/Shaders/Shader Graphs_Outline_SpriteSubTitle.mat` | `Materials/Shader Graphs_Outline_SpriteSubTitle.mat` | `3f444a548449a04479b3fa1a79a33950` | Subtitle outline |
| move | `Assets/Shaders/Master_DoubleSide_SG.shadergraph` | `Materials/Master_DoubleSide_SG.shadergraph` | `86a7d7795fa126f4685464e5879f72b7` | `M_Leena_Clothes_DoubleSide` |
| move | `Assets/Shaders/SG_Master.shadergraph` | `Materials/SG_Master.shadergraph` | `d6af6e2a827158a4486aa1c22c559717` | Pyramid + Leena body |
| move | `Assets/Shaders/shader_fire_C/S_Flame_A.shadergraph` | `Materials/S_Flame_A.shadergraph` | `e73843be577b719418a1f0225f42c95f` | `Flame_B` |

### Textures (21)

| Class | Source | Destination | GUID | Reason |
|---|---|---|---|---|
| move | `Assets/_Textures/Characters_Textures/Fighters/Leena_Textures/T_CLothes_ARM.png` | `Textures/T_CLothes_ARM.png` | `f149234a6c7c11a44b8b44216b87e502` | Leena clothes |
| move | `Assets/_Textures/Characters_Textures/Fighters/Leena_Textures/T_CLothes_BaseColor.png` | `Textures/T_CLothes_BaseColor.png` | `b840c5a5d3262584cb8970a48ced6048` | Leena clothes |
| move | `Assets/_Textures/Characters_Textures/Fighters/Leena_Textures/T_CLothes_Normal.png` | `Textures/T_CLothes_Normal.png` | `cd2c8bac9d840bd44a88271cfdf0471d` | Leena clothes |
| move | `Assets/_Textures/Characters_Textures/Fighters/Leena_Textures/T_Lina_ARM.png` | `Textures/T_Lina_ARM.png` | `a493efdaa2dd0744490323c493e29cad` | Leena body |
| move | `Assets/_Textures/Characters_Textures/Fighters/Leena_Textures/T_Lina_BaseColor.png` | `Textures/T_Lina_BaseColor.png` | `24e5952f4be7f6c4d83d294215be535f` | Leena body |
| move | `Assets/_Textures/Characters_Textures/Fighters/Leena_Textures/T_Lina_Normal.png` | `Textures/T_Lina_Normal.png` | `eb704ce5c728ca1478aa311a7dcbf558` | Leena body |
| move | `Assets/_Textures/Props_Textures/Recipent_life/glass_Opacity.jpg` | `Textures/glass_Opacity.jpg` | `f4ef017c32f39ff4280807459b000ae7` | Dependency of `Leena_Model.prefab` / `M_Leena_Body` |
| move | `Assets/_Textures/Environment_Textures/VFX_textures/Flame_C/Flame_D.png` | `Textures/Flame_D.png` | `317b733af1122d04da8ae595c5952e9b` | `Flame_B` |
| move | `Assets/_Textures/Environment_Textures/Edenian_Textures/Edenian_Ruins_Desert/Desert_Ground/T_DesertGround_Mask_Map.tga` | `Textures/T_DesertGround_Mask_Map.tga` | `0410a6e5fa138d549a0a65ebedb9454e` | Terrain layer |
| move | `Assets/_Textures/Environment_Textures/Edenian_Textures/Edenian_Ruins_Desert/Desert_Ground/T_DesertGround_basecolor.tga` | `Textures/T_DesertGround_basecolor.tga` | `aa3fd127ebbc5834f88693c5d07969a3` | Terrain layer |
| move | `Assets/_Textures/Environment_Textures/Edenian_Textures/Edenian_Ruins_Desert/Desert_Ground/T_DesertGround_normal.tga` | `Textures/T_DesertGround_normal.tga` | `18aa367f775b48c4781ac48b1ca61c58` | Terrain layer |
| move | `Assets/_Textures/Environment_Textures/Edenian_Textures/Edenian_Ruins_Desert/Pyramid_argus_textures/T_PyramidBase_ARM.tga` | `Textures/T_PyramidBase_ARM.tga` | `ea19aeab8c8f8394983dc0f537f3a725` | `M_PyramidBase` |
| move | `Assets/_Textures/Environment_Textures/Edenian_Textures/Edenian_Ruins_Desert/Pyramid_argus_textures/T_PyramidBase_basecolor.tga` | `Textures/T_PyramidBase_basecolor.tga` | `563cfdbda3b63434087f311cc6a1492a` | `M_PyramidBase` |
| move | `Assets/_Textures/Environment_Textures/Edenian_Textures/Edenian_Ruins_Desert/Pyramid_argus_textures/T_PyramidBase_normal.tga` | `Textures/T_PyramidBase_normal.tga` | `eb3febf8afc7ac447b1c1d950eedbcb6` | `M_PyramidBase` |
| move | `Assets/_Textures/Environment_Textures/Edenian_Textures/Edenian_Ruins_Desert/Pyramid_argus_textures/T_PyramidTrim_ARM.tga` | `Textures/T_PyramidTrim_ARM.tga` | `9ed592fbae47a3a4c8c3f57663b130c0` | `M_PyramidTrim` |
| move | `Assets/_Textures/Environment_Textures/Edenian_Textures/Edenian_Ruins_Desert/Pyramid_argus_textures/T_PyramidTrim_basecolor.tga` | `Textures/T_PyramidTrim_basecolor.tga` | `0b04e48576b6cb54790ad06b4febe802` | `M_PyramidTrim` |
| move | `Assets/_Textures/Environment_Textures/Edenian_Textures/Edenian_Ruins_Desert/Pyramid_argus_textures/T_PyramidTrim_normal.tga` | `Textures/T_PyramidTrim_normal.tga` | `30fba331ab527cc4ba6c309151d733fc` | `M_PyramidTrim` |
| move | `Assets/_PorSiAcaso/Layers/TexturesCom_Rock_CliffDesert3_1K_albedo.tif` | `Textures/TexturesCom_Rock_CliffDesert3_1K_albedo.tif` | `1f479801cbd4eab499d581105f0c9daa` | Terrain layer |
| move | `Assets/_PorSiAcaso/Layers/TexturesCom_Rock_CliffDesert3_1K_normal.tif` | `Textures/TexturesCom_Rock_CliffDesert3_1K_normal.tif` | `68dd87d13fc1c564b82010580c958519` | Terrain layer |
| move | `Assets/HDRI/DaySkyHDRI038B_2K-TONEMAPPED.jpg` | `Textures/DaySkyHDRI038B_2K-TONEMAPPED.jpg` | `9a4ed01dca2085c4e8beff5c17f6bec1` | `sky_nublado.mat` |
| move | `Assets/Camera Composition/Demo/Skyboxes/texture_skybox_blue.png` | `Textures/texture_skybox_blue.png` | `822745777082202409b18b6077220549` | **Curated exception**: runtime skybox dependency of `sky_nublado.mat`, not editor tooling; dropping it would leave the moved skybox material dangling |

### Environment (4)

| Class | Source | Destination | GUID | Reason |
|---|---|---|---|---|
| move | `Assets/Terrain/Desert_Edenian_Ruins.asset` | `Environment/Desert_Edenian_Ruins.asset` | `f3a5593e1fd643c4fb94244892f6a360` | Scene `Terrain` |
| move | `Assets/_PorSiAcaso/Layers/Desert.terrainlayer` | `Environment/Desert.terrainlayer` | `b8d5209af041ca2468e759ae5d364412` | Terrain layer |
| move | `Assets/_PorSiAcaso/Layers/DesertTest.terrainlayer` | `Environment/DesertTest.terrainlayer` | `bfcc9adaf0ec0114f9bf34b80f272832` | Terrain layer |
| move | `Assets/HDRI/sky_nublado.mat` | `Environment/sky_nublado.mat` | `5c36428700fdd554a8d5e66300834e57` | Scene `RenderSettings.skybox` |

### Prefabs (2)

| Class | Source | Destination | GUID | Reason |
|---|---|---|---|---|
| move | `Assets/_Prefabs/P_Characters/Fighters/Leena_Model.prefab` | `Prefabs/Leena_Model.prefab` | `d0f10c8e1dcc48642b60399336612be4` | Scene `Leena_Model` instance |
| move | `Assets/_Prefabs/P_Environment/Realms/Edenia/Complete_Structures/Edenian_Ruins_Desert_PREFABS/Obelisk.prefab` | `Prefabs/Obelisk.prefab` | `5ef616f96c6d99746a69ca4f5f5a7d69` | Scene `Obelisk` instances |

### Timeline (1)

| Class | Source | Destination | GUID | Reason |
|---|---|---|---|---|
| move | `Assets/Timelines/TL_Menu/TL_Menu.playable` | `Timelines/TL_Menu.playable` | `c2bf21a228f65664e98e13e3825442e3` | 331.55 s / 15-track menu cinematic; contains 7 embedded `Recorded*` clips + CinemachineShot |

### Fonts and TMP (9)

| Class | Source | Destination | GUID | Reason |
|---|---|---|---|---|
| move | `Assets/Fonts/TextMesh Pro/Fonts SDF/Name_Character_in_game.asset` | `Fonts/Name_Character_in_game.asset` | `e3ddf1d4e67b8314e9846fc92874c336` | Used by menu TMP (`constan SDF`) |
| move | `Assets/Fonts/TextMesh Pro/Fonts/LiberationSans.ttf` | `Fonts/LiberationSans.ttf` | `e3265ab4bf004d28a9537516768c1c75` | Source of the fallback TMP asset |
| move | `Assets/Fonts/TextMesh Pro/Fonts/constan.ttf` | `Fonts/constan.ttf` | `8c8d46ea782df38478cb6bd7cb2b02d1` | Source of `Name_Character_in_game` |
| move | `Assets/Fonts/TextMesh Pro/Fonts/constanb.ttf` | `Fonts/constanb.ttf` | `54a1ca8fcf8ccfc4db71df4b4f2982db` | TMP source family |
| move | `Assets/Fonts/TextMesh Pro/Fonts/constani.ttf` | `Fonts/constani.ttf` | `f16b3438a6af33a4dba8554cbf66f503` | TMP source family |
| move | `Assets/Fonts/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback - EXP PLAYER.asset` | `Fonts/LiberationSans SDF - Fallback - EXP PLAYER.asset` | `2e498d1c8094910479dc3e1b768306a4` | Fallback of `LiberationSans SDF` |
| move | `Assets/Fonts/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset` | `Fonts/LiberationSans SDF.asset` | `8f586378b4e144a9851e7b34d9b748ee` | Used by menu TMP |
| move | `Assets/Fonts/TextMesh Pro/Shaders/TMP_SDF-Mobile.shader` | `Fonts/TMP_SDF-Mobile.shader` | `fe393ace9b354375a9cb14cdbbc28be4` | `LiberationSans SDF` material |
| move | `Assets/Fonts/TextMesh Pro/Shaders/TMP_SDF.shader` | `Fonts/TMP_SDF.shader` | `68e6db2ebdc24f95958faec2be5558d6` | `Name_Character_in_game` material |

### UI art (21)

| Class | Source | Destination | GUID | Reason |
|---|---|---|---|---|
| move | `Assets/Fonts/Credits.png` | `UI/Buttons/Credits.png` | `f88c036b72503c346a8acfb1b8f20580` | Credits button |
| move | `Assets/Fonts/Credits02.png` | `UI/Buttons/Credits02.png` | `6a7996efd6fb9eb44a2477b1bd85a02d` | Credits button |
| move | `Assets/Fonts/CreditsOn.png` | `UI/Buttons/CreditsOn.png` | `19cc7ca876c50a54c9ff70bb97276d68` | Credits button |
| move | `Assets/Fonts/Extras.png` | `UI/Buttons/Extras.png` | `443ec188c93046348ac9944c14715250` | Extras button |
| move | `Assets/Fonts/Extras02.png` | `UI/Buttons/Extras02.png` | `a368b91e3a57da9438f961396c80eed5` | Extras button |
| move | `Assets/Fonts/ExtrasOn.png` | `UI/Buttons/ExtrasOn.png` | `77126be11ceea1a499e557d1014df2e2` | Extras button |
| move | `Assets/Fonts/Options.png` | `UI/Buttons/Options.png` | `8f8c7bf84fda3ef4d9889ecebde8acab` | Options button |
| move | `Assets/Fonts/Options02.png` | `UI/Buttons/Options02.png` | `95bdd51ebcc3a0d46ab231667d9721b6` | Options button |
| move | `Assets/Fonts/OptionsOn.png` | `UI/Buttons/OptionsOn.png` | `9a8e40c116fbecd428d5247b76d31496` | Options button |
| move | `Assets/Fonts/Play.png` | `UI/Buttons/Play.png` | `26955f25e27c3cd4d905963d28da2853` | Play button |
| move | `Assets/Fonts/Play02.png` | `UI/Buttons/Play02.png` | `8cbfa499f74048848b1cc05fa8191a6a` | Play button |
| move | `Assets/Fonts/PlayOn.png` | `UI/Buttons/PlayOn.png` | `25cbb78e7efb1c04b9d0259288c58369` | Play button |
| move | `Assets/Fonts/Quit.png` | `UI/Buttons/Quit.png` | `6621e69d684f56e45b98b0c97d58c071` | Quit button |
| move | `Assets/Fonts/Quit02.png` | `UI/Buttons/Quit02.png` | `0f03f53deb012894495c5b9a9bf49204` | Quit button |
| move | `Assets/Fonts/QuitOn.png` | `UI/Buttons/QuitOn.png` | `58e343e88a21e82489ebdfe37bdc9b64` | Quit button |
| move | `Assets/_Sprites/UI/Menus/Main_Menu/Logo&Tittle&subtitle/Mortal-Kombat-Logos0 2.png` | `UI/Logo/Mortal-Kombat-Logos0 2.png` | `3ac4dab875f8e464f83528ed778ba2d3` | Logo |
| move | `Assets/_Sprites/UI/Menus/Main_Menu/Logo&Tittle&subtitle/Name.png` | `UI/Logo/Name.png` | `b7ba843302cf71e488526ef85fd860f2` | Logo |
| move | `Assets/_Sprites/UI/Menus/Main_Menu/Logo&Tittle&subtitle/Project Oc 1.png` | `UI/Logo/Project Oc 1.png` | `b48928dc9a3ae224c8adc9e24a3e7d4d` | Logo |
| move | `Assets/_Sprites/UI/Menus/Main_Menu/Logo&Tittle&subtitle/Rectangle 4.png` | `UI/Logo/Rectangle 4.png` | `80d44bd8731eeed4ca069db3042c686e` | Logo |
| move | `Assets/_Sprites/UI/Menus/Main_Menu/icon/Group 5.png` | `UI/Amuleto/Group 5.png` | `84cf810e6c3b40d4dafd24a0008f57d0` | `Amuleto01` |
| move | `Assets/_Sprites/UI/Menus/Main_Menu/background.PNG` | `UI/Background/background.PNG` | `2bda4e586472b5f47815e5b55aae9fa5` | Main-menu background |

## DROP — 38 assets (do not move)

| Class | Source | GUID | Reason |
|---|---|---|---|
| drop | `Assets/Bellseboss/LIGHT/mainMenu_Profiles/CM vcam1 Profile.asset` | `90f4c3c4bf82ec140adfdc46f8ed7236` | **PPv2** `PostProcessProfile`; the project is URP and the scene Volumes have null profiles |
| drop | `Assets/Camera Composition/Scripts/Resources/Overlays/overlays.diagonal.png` | `c9d95680f7765924597092167a372bb2` | Editor-only Camera Composition overlay |
| drop | `Assets/Camera Composition/Scripts/Resources/Overlays/overlays.goldenratio.png` | `79b9b80aefa75f64b9d5a30ea126eea5` | Editor-only Camera Composition overlay |
| drop | `Assets/Camera Composition/Scripts/Resources/Overlays/overlays.goldenspiral.png` | `1d42720af587c634194e68d63a6ab372` | Editor-only Camera Composition overlay |
| drop | `Assets/Camera Composition/Scripts/Resources/Overlays/overlays.ruleofthirds.png` | `73733fe54b9af5542add1b1dcda3fe6e` | Editor-only Camera Composition overlay |
| drop | `Assets/Camera Composition/Scripts/CompositionOverlay.cs` | `98b784d50a6597540a070c89dc4b72d0` | Editor-only Camera Composition tooling |
| drop | `Assets/HDRI/desertMK.png` | `a543131990aa88f4abeef86bd5583935` | Only referenced by the dropped PPv2 profiles (+ unrelated `Water/Global Volume Profile`) |
| drop | `Assets/Resources/Images/Dragon_LEft.png` | `ab4dffbd29481624587abd81ac012a4c` | Legacy Options/Credits panel decoration |
| drop | `Assets/Resources/Images/Dragon_Right.png` | `4624a267fb7125c4e85c68b8d722cae4` | Legacy Options/Credits panel decoration |
| drop | `Assets/Resources/VideoMK.renderTexture` | `36666833dd1fa924795e0967a7538fbc` | Legacy `CanvasMenu_pery` VideoPlayer target; scene VideoPlayer has no clip/URL |
| drop | `Assets/Resources/pixel.png` | `203dc77fec4106a44aeee41d60883b1d` | Legacy canvas/panel filler sprite |
| drop | `Assets/_Animations/A_Canvas/UI/MainMenu/Extras.controller` | `0a8e68df1f0f2df408189d4f43ca0cb5` | Legacy canvas animation; canvas is rebuilt |
| drop | `Assets/_Animations/A_Canvas/UI/MainMenu/Extras/ContainerOfExtra.anim` | `a8dd747ba6188554ca61ac3694174cdb` | Legacy canvas animation |
| drop | `Assets/_Animations/A_Canvas/UI/MainMenu/Extras/ExtrasContainer.controller` | `33439856724ebfc4d8cb85018e06f181` | Legacy canvas animation |
| drop | `Assets/_Animations/A_Canvas/UI/MainMenu/Extras/HideExtras.anim` | `610a2bc3a13e1b5438cb5e762fcfc898` | Legacy canvas animation |
| drop | `Assets/_Animations/A_Canvas/UI/MainMenu/Extras/ShowExtras.anim` | `27e2631bf6ae1ac49b2807174974150c` | Legacy canvas animation |
| drop | `Assets/_Animations/A_Canvas/UI/MainMenu/HideData.anim` | `6596e0c64bffec14e9b0066235fc1b63` | Legacy canvas animation |
| drop | `Assets/_Animations/A_Canvas/UI/MainMenu/HideOptions.anim` | `5b9d67ad27024604bafd1aa74fea0dc3` | Legacy canvas animation |
| drop | `Assets/_Animations/A_Canvas/UI/MainMenu/Idle.anim` | `314eb3acb12f9ab41bc35af03befb6f9` | Legacy canvas animation |
| drop | `Assets/_Animations/A_Canvas/UI/MainMenu/MainMenu.anim` | `0b1c5469e3e0a4b47b7631629d50f59e` | Legacy canvas animation |
| drop | `Assets/_Animations/A_Canvas/UI/MainMenu/Principal.controller` | `80b36ffe00bd3814ebd553914f42a814` | Legacy canvas animation |
| drop | `Assets/_Animations/A_Canvas/UI/MainMenu/ShowData.anim` | `0e2b465ee7af0a34eb378eaf300d8aa8` | Legacy canvas animation |
| drop | `Assets/_Animations/A_Canvas/UI/MainMenu/ViewOptions.anim` | `fa68eda928081304ea5233c1527b99ea` | Legacy canvas animation |
| drop | `Assets/_Sprites/UI/HUD/Rage-Bar_Sprites/symbol_prods.png` | `0ff4245df432a6a47942da14084274f5` | HUD/rage bar, not menu cinema |
| drop | `Assets/_Sprites/UI/Menus/Main_Menu/Icons/Back0.png` | `3093976e748648746a7f7fc02e591a59` | Legacy Options/Credits/Rebind panel icon |
| drop | `Assets/_Sprites/UI/Menus/Main_Menu/Icons/Drop.png` | `2179f665b00a6224b947ec11a2fae889` | Legacy panel icon |
| drop | `Assets/_Sprites/UI/Menus/Main_Menu/Icons/Union.png` | `ebbddb422480d2e4caba0cbdc0680c89` | Legacy panel icon |
| drop | `Assets/_Sprites/UI/Menus/Main_Menu/Panels/Background.png` | `107c7b1c01096914db4aa24d0523a035` | Legacy panel background |
| drop | `Assets/_Sprites/UI/Menus/Main_Menu/Panels/Background_Reference.png` | `6af55585b5e8cb343911c766cf957eda` | Legacy panel background |
| drop | `Assets/Bellseboss/Angel/KeyRebinding/RebindUIPrefabAngel.prefab` | `4aeb1921ecab7504e97113d8033e0f9c` | Legacy rebind UI prefab |
| drop | `Assets/Bellseboss/Ivan/prefabs/MainMenu_Audio.prefab` | `68e2cef0784f948d8b3d02c1a445eccc` | FMOD menu audio prefab |
| drop | `Assets/Bellseboss/Ivan/scripts/UIButtonsSound.cs` | `12cd91da0abe84c03918838717c834de` | FMOD UI sound script |
| drop | `Assets/Bellseboss/Ivan/scripts/SettingsVolumeController.cs` | `7abd95e56ae0040e1ac1b12c3212eb7b` | FMOD volume script |
| drop | `Assets/Bellseboss/Pery/InputV2/FanGameMortalKombatV2.inputactions` | `0d6eea66530063541af3d22de808b3ab` | Legacy input actions; rebuild uses `_MKRebuild/Input/UI.inputactions` |
| drop | `Assets/Bellseboss/Pery/Prefabs/Finals/CanvasMenu_pery.prefab` | `5904bf2487aae2d4b85597acfbc0088b` | Legacy canvas prefab; canvas is rebuilt |
| drop | `Assets/Bellseboss/Pery/Prefabs/Finals/PanelExtra.prefab` | `0c48599cfc45c2243931ca71b7624de8` | Legacy extras panel |
| drop | `Assets/PostProccesing/Post_Menu.asset` | `9764131318b41a742b4e785baa2d1902` | PPv2 profile; project is URP |
| drop | `Assets/PostProccesing/Post_Menu_Local_Leena.asset` | `801f3e0ab22371f4dbed6797f13df045` | PPv2 profile; project is URP |

## Already moved in WU3 (do not touch)

| Class | Source | Destination | GUID |
|---|---|---|---|
| moved | `Assets/Fonts/F_PunkRocker.ttf` | `Assets/_MKRebuild/Migrated/Intro01/Fonts/F_PunkRocker.ttf` | `2c4891e3984c01d478d0570943e2b569` |
| moved | `Assets/Timelines/TL_intros/TIMELINE_intro01_Text.playable` | `Assets/_MKRebuild/Migrated/Intro01/Timelines/TIMELINE_intro01_Text.playable` | `c4c50d88e9ad00f419c80ef8eabfd3dd` |

## Out of scope / pending (leave untouched)

| Class | Source | GUID | Why |
|---|---|---|---|
| pending | `Assets/Timelines/TL_intros/TIMELINE_intro00_goroWalk_BELLSEBOSS.playable` | `007909bee35aa7d42bff15ba0218e394` | Intro00 deferred its fade timeline in WU2; not MainMenu |
| pending | `Assets/_Prefabs/Behavours/RuleOfCinematic.prefab` | `31eea0017d9cf314a8caf58654468910` | Intro01 dependency; not MainMenu |
| pending | `Assets/_Scenes/intros/ScreenTextured.renderTexture` | `8ba3379c6ff4fea4c86be13040ebcb4d` | Intro00 dependency; not MainMenu |

## Replace (27 rows, untouched)

The remaining 27 `replace` rows are scripts, legacy scenes, the baked lighting folder, and the
rebuilt canvas/scripts. They stay at their legacy paths and are **not** moved. The legacy
`mainMenu_` scene, its scripts and the legacy canvas are rebuilt in `_MKRebuild/` in WU4b/WU5.
They may break when the moved assets leave their old paths — expected and accepted.
