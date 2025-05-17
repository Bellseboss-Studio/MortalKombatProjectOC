uniform sampler2D noiseTex;       // Textura de ruido
uniform sampler2D diffuseTex;     // Textura difusa de la lava
uniform sampler2D normalTex;      // Mapa de normales de la lava
uniform float time;               // Tiempo para animación
uniform float flowSpeed1;         // Velocidad del primer flujo
uniform float flowSpeed2;         // Velocidad del segundo flujo
uniform float distortionStrength; // Intensidad de la distorsión

void main() {
    vec2 uv = vUv; // Coordenadas UV originales

    // Muestrea la textura de ruido en dos posiciones desplazadas
    vec2 noiseUV1 = uv + vec2(time * flowSpeed1, 0.0);
    vec2 noiseUV2 = uv + vec2(time * flowSpeed2, 0.0);
    vec2 distortion1 = texture2D(noiseTex, noiseUV1).rg * 2.0 - 1.0;
    vec2 distortion2 = texture2D(noiseTex, noiseUV2).rg * 2.0 - 1.0;

    // Promedia las distorsiones para un efecto turbulento
    vec2 totalDistortion = (distortion1 + distortion2) * 0.5;

    // Aplica la distorsión a las UV
    vec2 distortedUV = uv + totalDistortion * distortionStrength;

    // Muestrea las texturas con las UV distorsionadas
    vec4 diffuseColor = texture2D(diffuseTex, distortedUV);
    vec3 normal = texture2D(normalTex, distortedUV).xyz;

    // Realiza cálculos de iluminación con el normal distorsionado
    // (Aquí iría tu código de iluminación, dependiendo del modelo que uses)

    // Salida del color final
    gl_FragColor = diffuseColor; // Reemplaza con el resultado de iluminación
}