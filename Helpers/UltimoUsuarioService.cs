using System;
using System.IO;

namespace PoderJudicial.Helpers
{
    public static class UltimoUsuarioService
    {
        private static string RutaArchivo
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "PoderJudicial", "ultimo_usuario.txt");
            }
        }

        public static string Cargar()
        {
            try
            {
                return File.Exists(RutaArchivo)
                    ? File.ReadAllText(RutaArchivo).Trim()
                    : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        public static void Guardar(string usuario)
        {
            try
            {
                string carpeta = Path.GetDirectoryName(RutaArchivo);
                Directory.CreateDirectory(carpeta);
                File.WriteAllText(RutaArchivo, usuario == null ? string.Empty : usuario.Trim());
            }
            catch
            {
                // Recordar el nombre es auxiliar; nunca debe impedir el login.
            }
        }
    }
}
