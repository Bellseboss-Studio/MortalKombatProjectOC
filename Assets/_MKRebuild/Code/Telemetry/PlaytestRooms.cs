namespace MK.Rebuild.Telemetry
{
    /// <summary>
    /// Catálogo CERRADO de salas: es la única tabla de ids, zona de origen en la escena y
    /// presupuesto del doc 02. Agregar una sala es agregar una fila acá; nada más se toca.
    ///
    /// El orden de <see cref="All"/> es el del embudo (10 filas) y también el orden de resolución
    /// de atribución, para que un solapamiento de volúmenes no dependa del orden de
    /// <c>FindObjectsByType</c>.
    /// </summary>
    public static class PlaytestRooms
    {
        /// <summary>Atribución virtual para corredores y volúmenes no cubiertos.</summary>
        public const string NoRoom = "sin-sala";

        /// <summary>Una fila del catálogo.</summary>
        public readonly struct Room
        {
            /// <summary>Id que viaja en el log (<c>r</c>, <c>from</c>, <c>to</c>).</summary>
            public readonly string Id;

            /// <summary>
            /// Nombre del GameObject de la zona en la escena (<c>Zone_Atrio</c>…). Vacío = el
            /// builder crea el volumen aparte porque no hay <c>CameraZone</c> para esa sala.
            /// </summary>
            public readonly string ZoneName;

            /// <summary>Presupuesto del doc 02 en segundos. Negativo = sin presupuesto.</summary>
            public readonly float Budget;

            public Room(string id, string zoneName, float budget)
            {
                Id = id;
                ZoneName = zoneName;
                Budget = budget;
            }
        }

        /// <summary>
        /// Las 9 <c>CameraZone</c> más <c>descenso</c>, en el orden del embudo. Los presupuestos
        /// son los de la spec (TEL-4) y no se negocian en otro lado.
        /// </summary>
        public static readonly Room[] All =
        {
            new Room("atrio", "Zone_Atrio", 20f),
            new Room("lanzas", "Zone_Lanzas", 30f),
            new Room("pozo", "Zone_Pozo", 45f),
            new Room("guardia", "Zone_Guardia", 45f),
            new Room("galeria", "Zone_Galeria", 45f),
            new Room("pesos", "Zone_Pesos", 60f),
            new Room("camara-peso", "Zone_CamaraPeso", 40f),
            new Room("guardian", "Zone_Guardian", 90f),
            new Room("orochi", "Zone_Orochi", 120f),

            // Sin CameraZone en la escena: el corredor de descenso es el único volumen que el
            // builder tiene que crear de cero (5 x 8 x 14 en z 49..63).
            new Room("descenso", string.Empty, 20f)
        };

        /// <summary>Busca una fila por id. False = id fuera del catálogo cerrado.</summary>
        public static bool TryGet(string id, out Room room)
        {
            for (int i = 0; i < All.Length; i++)
            {
                if (All[i].Id != id) continue;
                room = All[i];
                return true;
            }

            room = default;
            return false;
        }

        /// <summary>Presupuesto en segundos, o -1 si la sala no tiene (incluido <c>sin-sala</c>).</summary>
        public static float BudgetOf(string id)
        {
            return TryGet(id, out Room room) ? room.Budget : -1f;
        }

        /// <summary>True si el id pertenece al catálogo (o es la atribución virtual).</summary>
        public static bool IsKnown(string id)
        {
            if (id == NoRoom) return true;
            return TryGet(id, out _);
        }
    }
}
