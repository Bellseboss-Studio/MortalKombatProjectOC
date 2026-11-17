using System.Globalization;
using System.Text;

namespace MK.Rebuild.Telemetry
{
    /// <summary>
    /// Arma UNA línea JSON (un objeto, sin anidar) por evento. No hay Newtonsoft en el proyecto y
    /// <c>JsonUtility</c> no garantiza orden estable de claves, así que el formato se escribe a mano:
    /// claves en el orden de la tabla de la spec, números invariantes y strings escapados.
    ///
    /// Un solo <see cref="StringBuilder"/> reusado: <see cref="Begin"/> limpia y
    /// <see cref="End"/> devuelve la línea completa (una alloc corta por evento, ~10/s).
    /// </summary>
    internal sealed class JsonLineBuilder
    {
        private readonly StringBuilder buffer = new StringBuilder(256);

        /// <summary>Arranca la línea con <c>{"e":"&lt;evento&gt;"</c>. Nunca se ordenan las claves.</summary>
        public void Begin(string eventName)
        {
            buffer.Clear();
            buffer.Append("{\"e\":\"");
            AppendEscaped(eventName);
            buffer.Append('"');
        }

        /// <summary>Campo entero (<c>t</c>, <c>run</c>, <c>dmg</c>, <c>hp</c>, <c>ph</c>, <c>sec</c>…).</summary>
        public void Int(string key, int value)
        {
            Key(key);
            buffer.Append(value.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>Campo decimal con <c>0.###</c> invariante: siempre <c>1.25</c>, jamás <c>1,25</c>.</summary>
        public void Num(string key, float value)
        {
            Key(key);
            AppendNumber(value);
        }

        /// <summary>Campo string: ids, escena, versión, identificadores de boss.</summary>
        public void Str(string key, string value)
        {
            Key(key);
            buffer.Append('"');
            AppendEscaped(value);
            buffer.Append('"');
        }

        /// <summary>Cierra el objeto y devuelve la línea. El llamador escribe el <c>\n</c>.</summary>
        public string End()
        {
            buffer.Append('}');
            return buffer.ToString();
        }

        private void Key(string key)
        {
            buffer.Append(",\"");
            buffer.Append(key);
            buffer.Append("\":");
        }

        /// <summary>
        /// NaN/Infinity no son JSON válido y una velocidad rota no puede tirar la grabación:
        /// se escriben como 0.
        /// </summary>
        private void AppendNumber(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) value = 0f;
            buffer.Append(value.ToString("0.###", CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Escapa <c>"</c>, <c>\</c> y todo control (&lt; 0x20) como <c>\u00XX</c>. Los ids son
        /// literales del catálogo; <c>m</c>, <c>sc</c> y <c>boss</c> vienen del sistema y pueden
        /// traer cualquier cosa.
        /// </summary>
        private void AppendEscaped(string value)
        {
            if (string.IsNullOrEmpty(value)) return;

            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                switch (c)
                {
                    case '"':
                        buffer.Append("\\\"");
                        continue;
                    case '\\':
                        buffer.Append("\\\\");
                        continue;
                }

                if (c < 0x20)
                {
                    buffer.Append("\\u");
                    buffer.Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    continue;
                }

                buffer.Append(c);
            }
        }
    }
}
