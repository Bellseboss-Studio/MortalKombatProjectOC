using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace MK.Rebuild.Telemetry
{
    /// <summary>
    /// Canal de grabación. Cada canal habilitado es un archivo propio con el mismo prefijo de
    /// sesión y el mismo origen de tiempo (TEL-7), de modo que un analista cruza los dos archivos
    /// por <c>t</c> sin heurísticas.
    /// </summary>
    public enum Channel
    {
        /// <summary>Posición 4 Hz, salas, dwell, reset, idle, verbos, marcas, checkpoints y objetivo.</summary>
        Level = 0,

        /// <summary>Combos, daño infligido y recibido, muertes y fases de jefe.</summary>
        Combat = 1
    }

    /// <summary>
    /// Destino de un canal: un <see cref="StreamWriter"/> append-only sobre
    /// <c>sesion-YYYYMMDD-HHmmss.&lt;canal&gt;.jsonl</c>, con buffer grande y volcado periódico.
    ///
    /// El sink es el ÚNICO lugar que toca disco y el único que puede apagarse solo: ante el primer
    /// fallo de IO (apertura, escritura o volcado) captura la excepción, deja UN aviso por consola
    /// y queda cerrado sin reintentar por evento (TEL-5). El gameplay nunca ve la excepción y el
    /// otro canal sigue grabando (TEL-7).
    /// </summary>
    internal sealed class JsonlSink
    {
        /// <summary>16 KB: un volcado por segundo con ~10 eventos/s entra en un solo flush.</summary>
        private const int StreamBufferSize = 16384;

        private readonly Channel channel;
        private readonly string filePath;

        private StreamWriter writer;
        private bool failed;

        public JsonlSink(Channel channel, string filePath)
        {
            this.channel = channel;
            this.filePath = filePath;
        }

        public Channel ChannelKind => channel;

        /// <summary>Ruta completa del archivo del canal.</summary>
        public string FilePath => filePath;

        /// <summary>True sólo con el archivo abierto y sin fallo de IO.</summary>
        public bool IsOpen => writer != null;

        /// <summary>True una vez que el canal se apagó por un fallo de IO (no reintenta).</summary>
        public bool Failed => failed;

        /// <summary>
        /// Crea los directorios faltantes y abre el archivo en modo append (jamás trunca ni
        /// renombra) con UTF-8 sin BOM. Un fallo deja el canal apagado con un solo aviso.
        /// </summary>
        public bool Open()
        {
            if (failed || writer != null) return writer != null;

            try
            {
                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

                // append: true => FileMode.Append. Un archivo previo con el mismo nombre (dos
                // ejecuciones en el mismo segundo) se anexa en vez de pisarse.
                writer = new StreamWriter(filePath, true, new UTF8Encoding(false), StreamBufferSize)
                {
                    AutoFlush = false
                };

                return true;
            }
            catch (Exception error)
            {
                Disable("no se pudo abrir el archivo", error);
                return false;
            }
        }

        /// <summary>Escribe una línea ya armada y su <c>\n</c>. False si el canal está apagado.</summary>
        public bool Write(string line)
        {
            if (writer == null) return false;

            try
            {
                // Media línea partida es peor que media línea perdida: se escribe completa y el
                // volcado siguiente la manda al disco.
                writer.Write(line);
                writer.Write('\n');
                return true;
            }
            catch (Exception error)
            {
                Disable("no se pudo escribir", error);
                return false;
            }
        }

        /// <summary>Vacía el buffer. Un fallo aquí apaga el canal (las líneas ya escritas quedan).</summary>
        public void Flush()
        {
            if (writer == null) return;

            try
            {
                writer.Flush();
            }
            catch (Exception error)
            {
                Disable("no se pudo volcar", error);
            }
        }

        /// <summary>Vuelca y cierra. Idempotente: se llama en el cierre de sesión y en OnDestroy.</summary>
        public void Close()
        {
            if (writer == null) return;

            try
            {
                writer.Flush();
                writer.Dispose();
                writer = null;
            }
            catch (Exception error)
            {
                Disable("no se pudo cerrar", error);
            }
        }

        /// <summary>Sufijo del archivo por canal: <c>.level</c> / <c>.combat</c>.</summary>
        public static string SuffixOf(Channel channel)
        {
            return channel == Channel.Combat ? ".combat" : ".level";
        }

        /// <summary>Nombre corto del canal para mensajes (no para el archivo).</summary>
        public static string NameOf(Channel channel)
        {
            return channel == Channel.Combat ? "combat" : "level";
        }

        /// <summary>
        /// Autoapagado: un aviso, sin reintentos y sin propagar. El writer se suelta ANTES de
        /// disponerlo para que un fallo del Dispose no vuelva a entrar acá.
        /// </summary>
        private void Disable(string what, Exception error)
        {
            if (failed) return;
            failed = true;

            StreamWriter dying = writer;
            writer = null;

            if (dying != null)
            {
                try
                {
                    dying.Dispose();
                }
                catch
                {
                    // Ya estaba roto: no hay nada que hacer y no puede escalar.
                }
            }

            Debug.LogWarning(
                $"[Telemetry] Canal '{NameOf(channel)}' desactivado: {what} " +
                $"({error.GetType().Name}: {error.Message}). El juego sigue sin telemetría en ese canal.");
        }
    }
}
