# Cómo pasarme tu sesión

Gracias por probar. Esto es una **demo técnica**: no hay que ganar nada, hay que jugar y que el
juego me cuente qué pasó.

## Antes de jugar

1. Descomprimí la **carpeta completa** en algún lugar con permiso de escritura (Escritorio o
   Documentos). No muevas el `.exe` solo: necesita lo que está al lado.
2. Abrí el juego y jugá **entre 8 y 10 minutos**. Sin apuro, sin buscar el camino "correcto".

## Lo único que te pido

**Si algo te molesta, apretá F12 en ese segundo exacto.**

No te va a preguntar nada, no pausa, no abre ningún menú. Sólo anota "acá, ahora". Podés apretarla
las veces que quieras, incluso con el menú de pausa abierto.

Ese "acá, ahora" es lo más valioso de todo: después yo cruzo esa marca con los 20 segundos previos
(qué enemigos había, cuánto llevabas en la sala, si venías de morir) y sé de qué se trataba sin
tener que preguntarte.

## Tres cosas para intentar

1. **Encadenar ataques**: atacá y volvé a atacar rápido, sin esperar a que termine la animación.
2. **Cruzar los fosos**: tirate, caete, volvé a intentar. El fracaso también es dato.
3. **Buscar la salida** de cada sala. Si te perdés, marcá con F12.

No hace falta que lo hagas perfecto ni que expliques nada mientras jugás.

## Al terminar

**Cerrá el juego** (Alt+F4 o el botón de salir). Ahí se escribe el final del archivo.

Después buscá esta carpeta:

```
playtest/          ← al lado del .exe
```

Adentro hay un archivo por sesión, con fecha y hora en el nombre:

```
sesion-20261117-135541.level.jsonl
sesion-20261117-135541.combat.jsonl
```

**Comprimí la carpeta `playtest` entera en un ZIP y nombralo con tu nombre.** Eso es todo.

## Si querés jugar más de una vez

Perfecto. Cada vez que abrís el juego se crea un archivo nuevo, con la hora en el nombre, así que
se ordenan solos. No borres los anteriores: la comparación entre sesiones es justamente lo que
sirve.

## Qué se guarda y qué no

- Se guarda **sólo lo que pasa en el juego**: posición, salas, muertes, marcas, combos.
- **No** se guarda nada personal, no hay cuenta, no hay internet: todo queda en tu máquina hasta
  que me mandes el ZIP.
- Si querés jugar sin dejar registro, agregá `-nolog` al acceso directo y no escribe nada.
