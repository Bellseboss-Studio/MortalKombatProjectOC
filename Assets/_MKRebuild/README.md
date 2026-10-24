# _MKRebuild — Nuevo punto de partida

Este folder es la nueva raíz aislada del rebuild. Todo lo nuevo vive acá;
el legacy queda congelado como solo-lectura.

## Estructura

| Carpeta | Contenido |
|---------|-----------|
| `Code/` | Código nuevo bajo el namespace `MK.Rebuild.*` |
| `Scenes/` | Escenas nuevas (fuera de Build Settings hasta el bootstrap) |
| `Prefabs/` | Prefabs nuevos |

## Reglas duras

- [ ] **Legacy congelado**: `Scripts/`, `_Scripts/`, `_Scenes/`, `Bellseboss/` son
      solo-lectura. Se mira, no se toca.
- [ ] **Copiar, nunca mover**: al rescatar algo del legacy se copia el `.cs`
      JUNTO con su `.meta` (conserva el GUID). Mover rompe el juego actual.
- [ ] **Terceros se referencian, no se duplican**: nada de copiar `Plugins/`,
      `PlayFab/` ni SDKs acá adentro.
- [ ] **`ProjectSettings/`, `Packages/`, `Settings/` intocables.**
- [ ] **Namespace futuro**: todo el código nuevo usa `MK.Rebuild.*`.
- [ ] **Servicios afuera**: sin PlayFab, sin FMOD, sin Wwise. Se parte de cero;
      si FMOD vuelve algún día, se reintegra desde cero vía IA.
