using System;
using System.IO;
using System.Text.Json;

namespace PoderJudicial.Helpers
{
    /// <summary>
    /// Configuración persistente de la base de datos (ruta del .accdb).
    /// Se guarda en %AppData%\PoderJudicial\config.json — carpeta de usuario,
    /// siempre escribible sin permisos de administrador, a diferencia de la
    /// carpeta de instalación del .exe.
    /// El campo "Proveedor" ya queda guardado desde ahora (aunque hoy solo
    /// exista "Access") para no tener que migrar el formato del archivo el
    /// día que se agregue otro proveedor (ej. SQL Server).
    /// </summary>
    public class ConfiguracionBD
    {
        public string Proveedor { get; set; } = "Access";
        public string RutaArchivo { get; set; } = "";

        public bool RespaldosAutomaticos { get; set; } = true;
        public int FrecuenciaRespaldoDias { get; set; } = 15;
        public int MaximoRespaldos { get; set; } = 6;

        /// <summary>
        /// Vacía usa la subcarpeta "Respaldos" junto a la base activa.
        /// </summary>
        public string CarpetaRespaldos { get; set; } = "";

        private static readonly string CarpetaConfig =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "PoderJudicial");

        private static readonly string RutaConfig =
            Path.Combine(CarpetaConfig, "config.json");

        /// <summary>
        /// Carga la configuración guardada. Devuelve null si todavía no
        /// existe (primera ejecución) o si el archivo está corrupto — en
        /// ambos casos el llamador debe tratarlo como "no configurado".
        /// </summary>
        public static ConfiguracionBD Cargar()
        {
            try
            {
                if (!File.Exists(RutaConfig)) return null;

                string json = File.ReadAllText(RutaConfig);
                ConfiguracionBD config = JsonSerializer.Deserialize<ConfiguracionBD>(json);

                if (string.IsNullOrWhiteSpace(config?.RutaArchivo))
                    return null;

                if (config.FrecuenciaRespaldoDias < 1)
                    config.FrecuenciaRespaldoDias = 15;

                if (config.MaximoRespaldos < 1)
                    config.MaximoRespaldos = 6;

                return config;
            }
            catch
            {
                // Archivo corrupto o ilegible: se trata igual que "no configurado"
                // en vez de tumbar la aplicación al iniciar.
                return null;
            }
        }

        /// <summary>Guarda esta configuración en disco, creando la carpeta si hace falta.</summary>
        public void Guardar()
        {
            Directory.CreateDirectory(CarpetaConfig);

            string json = JsonSerializer.Serialize(
                this, new JsonSerializerOptions { WriteIndented = true });

            File.WriteAllText(RutaConfig, json);
        }
    }
}
