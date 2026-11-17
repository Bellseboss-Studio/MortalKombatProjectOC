using System;
using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>Respuestas del jugador que el ataque declara como válidas.</summary>
    [Flags]
    public enum BossResponse
    {
        None = 0,
        Parry = 1,
        Dash = 2,
        Jump = 4,
    }

    /// <summary>Lista cerrada de comportamientos especiales de un ataque (CAT-8).</summary>
    [Flags]
    public enum BossAttackFlags
    {
        None = 0,

        /// <summary>El ataque arranca fuera de rango y el boss se acerca primero.</summary>
        ClosesDistance = 1,

        /// <summary>El ataque acciona un mecanismo del escenario en vez de golpear.</summary>
        Mechanism = 2,

        /// <summary>Puede interrumpirse con parry/desarme/rugido.</summary>
        Interruptible = 4,

        /// <summary>Deja el arma clavada: ventana de castigo larga si falla.</summary>
        WeaponStuck = 8,

        /// <summary>Si falla, el boss queda aturdido por el propio ataque.</summary>
        SelfStun = 16,

        /// <summary>Avanza girando con inercia (Berserker Spin).</summary>
        Spin = 32,
    }

    /// <summary>Por qué el boss quedó expuesto. Es el vocabulario cerrado de EXP-4.</summary>
    public enum BossExposureReason
    {
        None = 0,
        Parry = 1,
        Disarm = 2,
        Roar = 3,
        Error = 4,
    }

    /// <summary>
    /// Geometría de un beat de ataque. Los seis campos significan lo mismo —"dónde y cuánto
    /// ocupa el ataque"— y por eso viven agrupados y no como seis floats sueltos en el ataque.
    /// Cero en todos = no aplica.
    /// </summary>
    [Serializable]
    public sealed class BossAttackMotion
    {
        [Tooltip("Radio del área de golpe (Slam, barrido). 0 = golpe por contacto.")]
        public float radius;

        [Tooltip("Radio de impacto de la embestida. 0 = no aplica.")]
        public float hitRadius;

        [Tooltip("Velocidad de desplazamiento del ataque, en m/s.")]
        public float speed;

        [Tooltip("Distancia recorrida por el ataque, en metros.")]
        public float travel;

        [Tooltip("Duración del desplazamiento, en segundos (giro, salto).")]
        public float duration;

        [Tooltip("Radio de la marca en el suelo (Execution Leap).")]
        public float markRadius;
    }

    /// <summary>
    /// Un ataque del catálogo. Es data: el controller no conoce ningún id ni número de ataque
    /// (CAT-1). Vive en un array dentro de <see cref="BossDefinition"/> y no en un asset por
    /// ataque, porque casi todas las reglas del catálogo son de tabla (daño vs tell, rachas,
    /// variantes, clip vs duración) y el validador tiene que ver la tabla completa.
    /// </summary>
    [Serializable]
    public sealed class BossAttackDefinition
    {
        [Tooltip("Id estable y único, E01..E08.")]
        public string id = "E00";

        [Tooltip("Nombre de presentación.")]
        public string displayName = string.Empty;

        [Tooltip("Fases en las que el ataque entra al pool.")]
        public int[] phases = { 1 };

        [Tooltip("Anticipación antes del golpe, en segundos. Debe ser >= minTell.")]
        public float tell = 0.8f;

        [Tooltip("Daño aplicado. 0 sólo si el ataque es de mecanismo.")]
        public int damage;

        [Tooltip("Ventana de castigo después del ataque, en segundos.")]
        public float punish = 1.2f;

        [Tooltip("Respuestas válidas del jugador.")]
        public BossResponse responses = BossResponse.Parry | BossResponse.Dash;

        [Tooltip("Estados de CharacterVisual que reproduce el ataque, en orden.")]
        public string[] clips = Array.Empty<string>();

        [Tooltip("Estado de CharacterVisual durante la ventana de castigo.")]
        public string punishClip = string.Empty;

        [Tooltip("Comportamientos especiales del ataque.")]
        public BossAttackFlags flags = BossAttackFlags.None;

        [Tooltip("Distancia máxima desde la que el ataque puede iniciarse.")]
        public float range = 3f;

        [Tooltip("Peso relativo de selección.")]
        public float weight = 1f;

        [Tooltip("Id del ataque base del que éste es variante de fase 2. Vacío sólo en mecanismos.")]
        public string variantOf = string.Empty;

        [Tooltip("Slot del mecanismo que acciona. Obligatorio si flags incluye Mechanism.")]
        public string mechanismSlot = string.Empty;

        [Tooltip("Geometría del beat.")]
        public BossAttackMotion motion = new BossAttackMotion();

        /// <summary>True cuando el ataque acciona un mecanismo en vez de golpear.</summary>
        public bool IsMechanism => (flags & BossAttackFlags.Mechanism) != 0;

        /// <summary>True cuando el ataque puede interrumpirse.</summary>
        public bool IsInterruptible => (flags & BossAttackFlags.Interruptible) != 0;
    }

    /// <summary>
    /// Perfil de una fase: cuánto guarda el boss y cuánto dura la ventana del jugador.
    /// Vive en data para que el cerebro no tenga números de balance (ID-1).
    /// </summary>
    [Serializable]
    public sealed class BossPhaseProfile
    {
        [Tooltip("Probabilidad [0..1] de que el turno del boss sea Guardia en vez de ataque.")]
        [Range(0f, 1f)] public float guardChance = 0.55f;

        [Tooltip("Duración de la ventana del jugador al empezar esta fase, en segundos.")]
        public float playerWindow = 2.5f;
    }
}
