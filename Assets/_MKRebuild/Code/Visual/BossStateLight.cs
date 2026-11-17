using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// Placeholder de telegraph por COLOR, mientras el Executioner no tenga arte propio.
    ///
    /// Es una LUZ de estado en un objeto aparte, no un tint del cuerpo: el flash de
    /// <see cref="HitReceiver"/> escribe <c>material.color</c>, y cualquier tint por
    /// <c>MaterialPropertyBlock</c> lo taparía (y al restaurar dejaría el color equivocado).
    ///
    /// Lee <see cref="ExecutionerController.CurrentBeat"/>; no escribe nada en el boss.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BossStateLight : MonoBehaviour
    {
        [Tooltip("Boss cuyo beat se muestra. Null = el primero de la escena.")]
        [SerializeField] private ExecutionerController boss;

        [Tooltip("Luz que cambia de color. Null = la del mismo GameObject.")]
        [SerializeField] private Light target;

        [Header("Color por beat")]
        [SerializeField] private Color windup = new Color(1f, 0.55f, 0.1f);
        [SerializeField] private Color strike = new Color(1f, 0.15f, 0.1f);
        [SerializeField] private Color punish = new Color(0.65f, 0.25f, 1f);
        [SerializeField] private Color guard = new Color(0.2f, 0.85f, 1f);
        [SerializeField] private Color idle = new Color(0.55f, 0.55f, 0.55f);

        [Tooltip("Intensidad mientras el boss está en un beat de ataque.")]
        [SerializeField] private float activeIntensity = 2.5f;

        [Tooltip("Intensidad en la ventana del jugador (el boss no amenaza).")]
        [SerializeField] private float idleIntensity = 0.6f;

        private void Awake()
        {
            if (target == null) target = GetComponent<Light>();
        }

        private void Update()
        {
            if (boss == null)
            {
                boss = FindFirstObjectByType<ExecutionerController>();
                if (boss == null) return;
            }

            if (target == null) return;

            string beat = boss.CurrentBeat;
            switch (beat)
            {
                case "Windup":
                    target.color = windup;
                    target.intensity = activeIntensity;
                    break;
                case "Strike":
                    target.color = strike;
                    target.intensity = activeIntensity;
                    break;
                case "Punish":
                    target.color = punish;
                    target.intensity = activeIntensity;
                    break;
                case "Guard":
                    target.color = guard;
                    target.intensity = activeIntensity;
                    break;
                default:
                    target.color = idle;
                    target.intensity = idleIntensity;
                    break;
            }
        }
    }
}
