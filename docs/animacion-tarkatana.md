# Pautas de animación — Tarkatana

> Documento de referencia para **autorizar o rechazar** las animaciones finales del estilo
> Tarkatana. No contiene código ni modifica assets: es la vara con la que se juzgan los clips.
> Épica `combo-grammar` · issue **#421**.

## 1. Alcance

Tarkatana todavía no tiene animaciones propias: hoy comparte los clips de ataque del jugador
retimeados. La diferencia entre estilos se transmite **por cadencia y por ventanas**, y esta
guía describe qué deberían hacer las animaciones finales para que ese ritmo se sostenga cuando
existan.

Queda fuera de alcance: cualquier cambio a `StanceComboMatrix`, a los `ComboDefinition`, al
retime o a la lógica de la gramática.

## 2. Qué está simulando la cadencia 0.80

`styleSpeedScale = 0.80` significa: el mismo clip se reproduce al **80 %** de la velocidad que
tendría con cadencia neutra. Contra Mantis (1.15), un paso de Tarkatana dura aproximadamente
**44 % más**.

Eso está simulando tres cosas concretas de un arma pesada:

| Idea | Cómo se lee en pantalla | Qué la hace creíble |
|---|---|---|
| **Peso** | La hoja arrastra; el cuerpo se desplaza con el arma | El centro de masa viaja con el golpe, no se queda atrás |
| **Compromiso** | El personaje se entrega al swing y no puede corregirlo | La anticipación es larga y la extensión es máxima al contacto |
| **Recuperación lenta** | Después del golpe cuesta volver a guardia | La desaceleración ocupa la mitad final del paso |

La cadencia **no** simula torpeza: Tarkatana debe sentirse lenta pero decidida, no trabada.
Un clip que se vea "arrastrado" (poses intermedias sin intención) no sirve aunque respete los
tiempos.

## 3. Ritmo esperado por rol

Cada paso declara su rol en la matriz del estilo. El rol cambia el peso narrativo del clip,
no su duración.

| Rol | Movimiento | Qué debe comunicar | Energía |
|---|---|---|---|
| **Opener** | `Combo_H` (pesado neutral) | Anuncia el estilo: un golpe de apertura con peso, que deja al rival en desventaja | Alta anticipación, contacto contundente, recuperación que invita a seguir |
| **Filler** | `Combo_1` (ligero neutral) | Mantiene la presión sin cortar el ritmo: es el "pega y sigue" | La más corta en gesto, la más encadenable |
| **Closer** | `Combo_Launcher_L` (lanzador) | Cierra la cadena: es el golpe que el jugador recuerda | Máxima extensión, follow-through largo, es el **único** con bonus de daño (+15 %) |

Regla de lectura: el opener presenta, el filler sostiene, el closer corona. Si los tres clips
se sienten intercambiables, el estilo no tiene identidad.

## 4. Timing real por paso

Valores calculados con los assets actuales y el tuning por defecto (`attackSpeedScale = 1.5`).
Los tiempos son de **reproducción efectiva** (clip retimeado), no del clip crudo.

| Paso | Clip autorado | Impacto (`hitNorm.y`) | Paso efectivo | Ventana conectada | Ventana whiff | Recuperación |
|---|---|---|---|---|---|---|
| Opener — `Combo_H#0` | 2.5 s | 40 % → **0.175 s** | **0.439 s** | 0.175 → 0.375 s (200 ms) | 0.175 → 0.275 s (100 ms) | 0.275 → 0.439 s |
| Filler — `Combo_1#0` | 2.5 s | 35 % → **0.154 s** | **0.439 s** | 0.154 → 0.354 s (200 ms) | 0.154 → 0.254 s (100 ms) | 0.254 → 0.439 s |
| Closer — `Combo_Launcher_L#0` | 1.2 s | 40 % → **0.067 s** | **0.167 s** | 0.067 → 0.167 s (100 ms) | 0.067 → 0.167 s (100 ms) | 0.067 → 0.167 s |

Contraste con Mantis (mismos clips, `styleSpeedScale = 1.15`): los pasos duran 0.305 s /
0.260 s / 0.305 s, y el impacto del opener llega a los 0.107 s. Es decir, en Tarkatana el
contacto visual llega **~68 ms más tarde** y el paso completo **~0.13 s más tarde**.

Notas sobre la tabla:

- La **ventana de encadenado abre exactamente en el impacto**, no en el final del clip. Todo lo
  que el jugador haga para seguir la cadena ocurre entre el contacto y 200 ms después (100 ms
  si falló). El clip debe aceptar un corte en ese punto.
- El **closer no tiene recuperación cancelable**: su ventana termina con el paso (100 ms). El
  follow-through largo del lanzador se reproduce completo.
- Después de cerrar la cadena, la memoria se limpia a los **0.45 s** sin input. No es un tiempo
  de animación: es de sistema.

## 5. Poses y anticipación

Por rol, los momentos que un clip final **debe** tener:

**Opener (`Combo_H`)**
1. *Guardia de entrada* (0 %): pose compatible con el clip anterior o con la locomoción; el
   crossfade dura 0.05 s, así que el primer 10 % tiene que parecerse a un estado neutro.
2. *Carga* (0–40 %): el arma se lleva atrás; el peso del cuerpo pasa a la pierna trasera.
3. *Contacto* (40 %): máxima extensión, cadera y hombro alineados con la hoja. Es el frame que
   el jugador lee como "pegó".
4. *Recuperación* (40–100 %): el arma sigue su inercia y vuelve a guardia. La desaceleración
   tiene que ser visible: es el peso del estilo.

**Filler (`Combo_1`)**
1. *Enlace* (0–35 %): anticipación corta, casi un gesto continuo desde el golpe anterior.
2. *Contacto* (35 %): corte limpio, sin pausa.
3. *Retorno* (35–100 %): el arma vuelve lista para el siguiente gesto. Debe poder cortarse al
   80 % del paso (fin de la ventana conectada) sin que el personaje quede en una pose rara.

**Closer (`Combo_Launcher_L`)**
1. *Compromiso* (0–40 %): el cuerpo baja y carga el lanzamiento; es el gesto más grande de la
   cadena.
2. *Contacto* (40 %): impacto ascendente, máxima extensión vertical.
3. *Follow-through* (40–100 %): la hoja termina arriba y el cuerpo queda abierto. No hay
   siguiente paso: el clip se reproduce completo.

## 6. Reglas de retime (cómo leer los tiempos)

- El juego escala el clip por `styleSpeedScale × attackSpeedScale` y lo mismo usa para calcular
  ventanas y fin de paso. **El clip y la lógica nunca se desfasan** si se respeta la estructura
  normalizada.
- Por eso lo que se autora es la **posición normalizada del contacto** (35–40 % del clip). Si
  una animación final mueve el contacto visual, `hitNorm` deja de coincidir con la imagen y el
  golpe "pega" en el aire o tarde.
- Los valores absolutos de la tabla asumen `attackSpeedScale = 1.5`. Ese valor es tuning en vivo
  (slider de debug): si cambia, cambian los segundos, **no** las fracciones.

## 7. Criterios para autorizar un clip final

Un clip se **autoriza** sólo si cumple todo esto:

1. **Contacto en el frame correcto**: la máxima extensión cae en el 35 % (filler) o 40 %
   (opener/closer) del clip, con tolerancia de un frame del clip crudo.
2. **Anticipación legible a 5.7×**: al retimear, la carga se distingue. Si hay que congelar el
   frame para entender qué hace, no sirve.
3. **Corte en la ventana**: el personaje puede abandonar el clip en el contacto +200 ms sin
   pop visual; la pose en ese punto es una transición, no una pose única.
4. **Retorno a guardia al 100 %**: el clip termina en un estado compatible con el siguiente
   inicio (crossfade de 0.05 s).
5. **Sin root motion**: el desplazamiento lo pone el sistema; los pies deben aceptar el desliz
   sin saltos.
6. **Peso coherente con 0.80**: la desaceleración de la segunda mitad se siente; el arma no
   "flota" ni se detiene en seco.
7. **Identidad de rol**: se distingue a simple vista si es opener, filler o closer.
8. **Sin pausas muertas**: no hay mesetas donde el personaje queda quieto esperando el fin del
   clip.

## 8. Criterios de rechazo

Rechazar (y devolver con el motivo) si:

- El contacto visual no coincide con el frame normalizado declarado (± 1 frame).
- La anticipación desaparece al retimear o se lee como "teletransporte" de la hoja.
- El clip necesita llegar al final para verse bien: la cadena se corta en la ventana, no al
  final.
- Hay root motion horneado o desplazamiento que compita con el avance del sistema.
- El follow-through deja al personaje trabado más allá del fin del paso.
- Se siente igual de liviano que Mantis: el estilo pierde su razón de ser.

## 9. Validación

1. El clip se revisa en el paso real del estilo (Tarkatana, cadencia 0.80) y con el tuning por
   defecto.
2. Se compara contra Mantis con la misma secuencia de botones: la diferencia de ritmo debe
   notarse **sin mirar números**.
3. Se deja constancia de la decisión en el issue **#421** con el motivo (autorizado / rechazado
   y por qué criterio).
