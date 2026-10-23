# Migrated — assets owned by the rebuild

Everything under this folder is moved here with its `.meta` intact, so GUIDs and every internal
reference survive. The point is ownership: after migration `_MKRebuild/` must build without any
`Assets/` path outside it, because the legacy tree is meant to be deleted.

## Layout

| Folder | Content |
|---|---|
| `Intro00/` | `Models` `Materials` `Textures` `Animations` `Audio` `Prefabs` `Timelines` |
| `Intro01/` | `Models` `Materials` `Textures` `Animations` `Audio` `Prefabs` `Timelines` |
| `MainMenu/` | `Models` `Materials` `Textures` `Animations` `Audio` `Prefabs` `Timelines` `Environment` |

Leaves are created empty in WU1; assets are moved per work unit (WU2-WU4), never copied.
`Video/` and `Fonts/` are added where those work units need them.

## Rules

- Every asset here MUST be referenced only from `_MKRebuild` code, scenes, prefabs and data.
- Never copy an asset here: a copy without its `.meta` gets a new GUID and breaks every internal
  reference. Use `AssetDatabase.MoveAsset` (moves the `.meta` atomically).
- The generated reports are the migration ledger, not documentation to hand-edit:
  - `manifest.md` — pre-move ledger (`MK/Migration/Generate Move Manifest`); reverse the
    `share` rows to roll a move back.
  - `legacy-references.md` — current outside-`_MKRebuild` dependency audit
    (`MK/Migration/Audit Legacy References`).
