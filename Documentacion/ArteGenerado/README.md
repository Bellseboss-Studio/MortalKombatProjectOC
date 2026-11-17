# Arte generado (MCP)

Carpeta única para todo el arte de diseño que generamos con el MCP de paint.
No es arte final de juego: es material de diseño/iteración sobre el que discutimos y después modelamos.

## Convención de nombres

```
<tema>-<variante>-<NN>.png          ej: mazmorra-base-02.png
<tema>-<variante>-<NN>.paint        fuente editable del paint MCP (raster, capas)
<tema>-<variante>-<NN>-vector.svg   fuente vectorial (SVG, se abre en Figma/Inkscape)
<tema>-<variante>-<NN>-vector.png   render del SVG a 1x
```

### Raster vs vector

| | paint MCP (raster) | svg MCP (vector) |
|---|---|---|
| Mapas, diagramas, GDD | funciona, pero la tipografía es pixel art | **gana**: texto real, jerarquía, escalable |
| Sprites, texturas, pixel art | **gana**: está hecho para eso | no sirve |
| Editar después | capas en `.paint` | SVG en Figma/Inkscape/Illustrator |
| Resolución | fija | cualquiera, sin pérdida |

Regla: **mapas y diagramas → SVG. Sprites y texturas → paint.**

- `NN` sube cuando cambia el layout. Nunca se sobrescribe la versión anterior: el historial es parte del diseño.
- Un PNG por versión; el `.paint` sólo del último que esté vivo.

## Lienzo base

- 2000x1200 px, lienzo de papel `#F5F0E4`, tinta `#22262E`.
- Grilla de 80 px al 5% de opacidad, marco doble, leyenda abajo a la derecha.
- Paleta por tipo de zona:
  - tránsito / pasillo `#E2DAC6`
  - nodo / arena central `#F0D9A4`
  - sala de palanca `#CFE2E6`
  - arena de jefe `#EEC7BF`
  - entrada / puerta `#22262E`

## Mazmorra

`mazmorra-base-01` — esquema de flujo de la mazmorra:

```
[ENTRADA] --> (NODO 3) --> [BOSS 1] --> [BOSS 2]
                 ^
                 +-- PALANCA 1 (arriba)
                 +-- PALANCA 2 (abajo)
```

Sobre esta base se agregan ramas, atajos y puertas con llave sin romper el eje principal.

`mazmorra-base-02` — expansión a **11:10 de contenido** (demo técnica). 2 palancas, entrada por el sur.

```
[LLEGADA] -> [VESTIBULO] -> (NODO 3) -> [GRAN PUERTA] -> [BOSS 1] -> [DESCENSO] -> [BOSS 2]
                                 |
                    +------------+-------------+
                    |                          |
             ALA ESTE (combate)        ALA OESTE (traversal)
         Patio -> Ecos -> Galeria ->    Lanzas -> Abismo -> Pozo ->
              Sala Palanca 2                Sala Palanca 1
```

Beats y presupuesto de tiempo:

| Beat | Zona | Tiempo |
|---|---|---|
| 1 | Llegada | 1:00 |
| 2 | Vestíbulo | 1:00 |
| 3 | Nodo 3 | 0:20 |
| 4 | Ala Este (13 enemigos) | 2:30 |
| 5 | Ala Oeste (8 enemigos) | 2:30 |
| 6 | Gran puerta + Boss 1 | 1:30 |
| 7 | Descenso | 0:20 |
| 8 | Boss 2 | 2:00 |

Los números de salto y velocidades salen de `PlayerParams.asset` y `Data/Enemy_*.asset`, no son estimaciones.
