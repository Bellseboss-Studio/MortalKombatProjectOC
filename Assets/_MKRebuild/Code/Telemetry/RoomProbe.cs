using UnityEngine;

namespace MK.Rebuild.Telemetry
{
    /// <summary>
    /// Volumen de una sala. NO decide nada: avisa al <see cref="PlaytestLog"/> que el jugador
    /// entró o salió, y el servicio es el dueño de la atribución.
    ///
    /// El volumen lo crea el builder como hijo <c>Probe_&lt;id&gt;</c> de cada <c>Zone_*</c> y le
    /// copia el tamaño del <see cref="BoxCollider"/> de la zona (una sola fuente de verdad para la
    /// geometría). <c>descenso</c> no tiene <c>CameraZone</c>: el builder lo crea aparte.
    ///
    /// Dedupe igual que <see cref="CameraZone"/>: el jugador tiene DOS colliders (cápsula raíz +
    /// trigger <c>Face</c>), así que sin el flag <c>occupied</c> cada visita emitiría dos
    /// <c>room.enter</c>.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class RoomProbe : MonoBehaviour
    {
        [Tooltip("Id de la sala en el catálogo cerrado (atrio, lanzas, pozo, guardia, galeria, pesos, camara-peso, guardian, orochi, descenso). Lo escribe el builder.")]
        [SerializeField] private string roomId = string.Empty;

        [Tooltip("Presupuesto del doc 02 en segundos. Es un espejo informativo para el Inspector: el runtime lee PlaytestRooms.")]
        [SerializeField] private float budget;

        private Collider volume;
        private bool occupied;

        /// <summary>Id que viaja al log. Vacío = el builder no cableó el probe (no emite).</summary>
        public string RoomId => roomId;

        /// <summary>Presupuesto en segundos de esta sala (-1 si no tiene).</summary>
        public float Budget => budget;

        /// <summary>
        /// Test de volumen sin esperar un paso de física: lo usa el servicio para re-atribuir
        /// después de un teleport, un respawn o la pausa.
        /// </summary>
        public bool Contains(Vector3 worldPoint)
        {
            return volume != null && volume.bounds.Contains(worldPoint);
        }

        private void Awake()
        {
            volume = GetComponent<Collider>();
        }

        private void Start()
        {
            // El jugador puede estar YA dentro al cargar la escena: se siembra occupied (mismo
            // patrón que CameraZone.Start) para que el OnTriggerExit posterior no se pierda y la
            // visita nunca quede abierta para siempre.
            if (!occupied && PlayerInside()) occupied = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (occupied) return;
            if (other == null) return;
            if (other.GetComponentInParent<PlayerDamage>() == null) return; // boss, props, Face

            occupied = true;
            PlaytestLog.Instance?.RoomEntered(roomId);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!occupied) return;
            if (other == null) return;
            if (other.GetComponentInParent<PlayerDamage>() == null) return;

            occupied = false;
            PlaytestLog.Instance?.RoomLeft(roomId);
        }

        private bool PlayerInside()
        {
            PlayerMotor player = FindFirstObjectByType<PlayerMotor>();
            return player != null && Contains(player.transform.position);
        }
    }
}
