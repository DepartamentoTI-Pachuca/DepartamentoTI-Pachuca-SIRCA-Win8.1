using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace PoderJudicial.Helpers
{
    /// <summary>
    /// Maneja:
    /// 1) Consolidación de Simples + Auténticas.
    /// 2) Alta o actualización del informe del día dentro del Word anual.
    /// 3) Detección de informes ya archivados.
    /// 4) Invalidación del consolidado cuando se regenera un informe.
    /// 5) Limpieza segura de temporales de días anteriores.
    ///
    /// El documento anual usa marcadores OCULTOS por fecha:
    /// PJ_INFORME_INICIO_yyyy-MM-dd
    /// PJ_INFORME_FIN_yyyy-MM-dd
    ///
    /// Gracias a esos marcadores, actualizar el 07/08/2026
    /// NO borra ni modifica 06/08/2026, 05/08/2026, etc.
    /// </summary>
    public static class InformeCopiasService
    {
        private const string PrefijoInicio = "PJ_INFORME_INICIO_";
        private const string PrefijoFin = "PJ_INFORME_FIN_";

        // Compatibilidad con una versión anterior del proyecto.
        private const string PrefijoLegacy = "PJ_INFORME_DIARIO_";

        // ═══════════════════════════════════════════════════════════════
        // 1. CONSOLIDAR INFORMES DEL DÍA
        // ═══════════════════════════════════════════════════════════════

        public static string ConsolidarInformeDelDia(DateTime fecha)
        {
            RutasInformes.CrearEstructura();

            string rutaSimples =
                RutasInformes.ObtenerRutaSimples(fecha);

            string rutaAutenticas =
                RutasInformes.ObtenerRutaAutenticas(fecha);

            string rutaConsolidado =
                RutasInformes.ObtenerRutaConsolidado(fecha);

            if (!File.Exists(rutaSimples))
            {
                throw new FileNotFoundException(
                    "No existe el informe de copias simples del día.",
                    rutaSimples);
            }

            if (!File.Exists(rutaAutenticas))
            {
                throw new FileNotFoundException(
                    "No existe el informe de copias auténticas del día.",
                    rutaAutenticas);
            }

            if (File.Exists(rutaConsolidado))
            {
                File.Delete(rutaConsolidado);
            }

            using WordprocessingDocument destino =
                WordprocessingDocument.Create(
                    rutaConsolidado,
                    WordprocessingDocumentType.Document);

            MainDocumentPart main =
                destino.AddMainDocumentPart();

            main.Document =
                new Document(new Body());

            Body cuerpo =
                main.Document.Body!;

            CopiarContenidoDocumento(
                rutaSimples,
                cuerpo);

            InsertarAntesDeSeccion(
                cuerpo,
                CrearSaltoDePagina());

            CopiarContenidoDocumento(
                rutaAutenticas,
                cuerpo);

            main.Document.Save();

            return rutaConsolidado;
        }

        // ═══════════════════════════════════════════════════════════════
        // 2. AGREGAR O REEMPLAZAR ÚNICAMENTE EL DÍA ACTUAL
        // ═══════════════════════════════════════════════════════════════

        public static string AgregarOActualizarInformeAnual(DateTime fecha)
        {
            RutasInformes.CrearEstructura();

            string rutaConsolidado =
                RutasInformes.ObtenerRutaConsolidado(fecha);

            if (!File.Exists(rutaConsolidado))
            {
                throw new FileNotFoundException(
                    "No existe el informe consolidado del día.",
                    rutaConsolidado);
            }

            string rutaAnual =
                RutasInformes.ObtenerRutaInformeAnual(fecha.Year);

            CrearDocumentoAnualSiNoExiste(rutaAnual);

            using WordprocessingDocument anual =
                WordprocessingDocument.Open(
                    rutaAnual,
                    true);

            MainDocumentPart main =
                anual.MainDocumentPart
                ?? throw new InvalidOperationException(
                    "El documento anual no contiene una parte principal válida.");

            main.Document ??=
                new Document(new Body());

            main.Document.Body ??=
                new Body();

            Body cuerpo =
                main.Document.Body;

            bool yaExistia =
                ContieneInformeFecha(cuerpo, fecha);

            if (yaExistia)
            {
                EliminarBloqueFecha(
                    cuerpo,
                    fecha);

                QuitarSaltosFinalesHuerfanos(
                    cuerpo);
            }

            bool tieneContenidoAnterior =
                cuerpo.ChildElements.Any(
                    elemento =>
                        elemento is not SectionProperties);

            if (tieneContenidoAnterior)
            {
                InsertarAntesDeSeccion(
                    cuerpo,
                    CrearSaltoDePagina());
            }

            // Inicio invisible del bloque del día.
            InsertarAntesDeSeccion(
                cuerpo,
                CrearMarcadorOculto(
                    ObtenerMarcadorInicio(fecha)));

            // Contenido real: consolidado del día.
            CopiarContenidoDocumento(
                rutaConsolidado,
                cuerpo);

            // Fin invisible del bloque del día.
            InsertarAntesDeSeccion(
                cuerpo,
                CrearMarcadorOculto(
                    ObtenerMarcadorFin(fecha)));

            main.Document.Save();

            return rutaAnual;
        }

        /// <summary>
        /// Alias para no romper código anterior que todavía llame
        /// AgregarAlInformeAnual().
        /// </summary>
        public static string AgregarAlInformeAnual(DateTime fecha)
        {
            return AgregarOActualizarInformeAnual(fecha);
        }

        // ═══════════════════════════════════════════════════════════════
        // 3. ESTADO DEL INFORME ANUAL
        // ═══════════════════════════════════════════════════════════════

        public static bool EstaAgregadoAlAnual(DateTime fecha)
        {
            string rutaAnual =
                RutasInformes.ObtenerRutaInformeAnual(
                    fecha.Year);

            if (!File.Exists(rutaAnual))
                return false;

            try
            {
                using WordprocessingDocument anual =
                    WordprocessingDocument.Open(
                        rutaAnual,
                        false);

                Body? cuerpo =
                    anual.MainDocumentPart?
                        .Document?
                        .Body;

                return cuerpo != null &&
                       ContieneInformeFecha(
                           cuerpo,
                           fecha);
            }
            catch (IOException)
            {
                // Si Word está bloqueando el archivo, no inventamos el estado.
                return false;
            }
        }

        public static bool RequiereActualizarAnual(DateTime fecha)
        {
            if (!EstaAgregadoAlAnual(fecha))
                return false;

            string rutaAnual =
                RutasInformes.ObtenerRutaInformeAnual(
                    fecha.Year);

            string rutaConsolidado =
                RutasInformes.ObtenerRutaConsolidado(
                    fecha);

            // Si se regeneró Simples/Auténticas,
            // ReportesView invalida/elimina el consolidado anterior.
            if (!File.Exists(rutaConsolidado))
            {
                return File.Exists(
                           RutasInformes.ObtenerRutaSimples(fecha))
                       ||
                       File.Exists(
                           RutasInformes.ObtenerRutaAutenticas(fecha));
            }

            if (!File.Exists(rutaAnual))
                return false;

            return File.GetLastWriteTimeUtc(rutaConsolidado) >
                   File.GetLastWriteTimeUtc(rutaAnual);
        }

        public static int ContarInformesEnAnual(int anio)
        {
            string rutaAnual =
                RutasInformes.ObtenerRutaInformeAnual(
                    anio);

            if (!File.Exists(rutaAnual))
                return 0;

            using WordprocessingDocument anual =
                WordprocessingDocument.Open(
                    rutaAnual,
                    false);

            Body? cuerpo =
                anual.MainDocumentPart?
                    .Document?
                    .Body;

            if (cuerpo == null)
                return 0;

            int nuevos =
                cuerpo
                    .Descendants<Text>()
                    .Count(t =>
                        (t.Text ?? string.Empty)
                            .StartsWith(
                                PrefijoInicio,
                                StringComparison.Ordinal));

            int legacy =
                cuerpo
                    .Descendants<Text>()
                    .Count(t =>
                        (t.Text ?? string.Empty)
                            .StartsWith(
                                PrefijoLegacy,
                                StringComparison.Ordinal));

            return nuevos + legacy;
        }

        // ═══════════════════════════════════════════════════════════════
        // 4. SI REGENERAS SIMPLES/AUTÉNTICAS, EL CONSOLIDADO YA NO SIRVE
        // ═══════════════════════════════════════════════════════════════

        public static void InvalidarConsolidado(DateTime fecha)
        {
            string ruta =
                RutasInformes.ObtenerRutaConsolidado(
                    fecha);

            if (!File.Exists(ruta))
                return;

            try
            {
                File.Delete(ruta);
            }
            catch (IOException)
            {
                throw new IOException(
                    "No se pudo invalidar el consolidado anterior porque está abierto en Word.");
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // 5. TEMPORALES: VÁLIDOS HASTA TERMINAR EL DÍA
        // ═══════════════════════════════════════════════════════════════

        public static List<string> ArchivarTemporalesVencidos(
            DateTime fechaActual)
        {
            RutasInformes.CrearEstructura();

            var movidos =
                new List<string>();

            foreach (string archivo in
                     Directory.GetFiles(
                         RutasInformes.CarpetaTemporales,
                         "*.docx"))
            {
                DateTime? fechaArchivo =
                    ObtenerFechaDesdeNombre(
                        Path.GetFileName(archivo));

                if (!fechaArchivo.HasValue)
                    continue;

                // Los archivos de HOY siguen siendo temporales válidos.
                if (fechaArchivo.Value.Date >=
                    fechaActual.Date)
                {
                    continue;
                }

                try
                {
                    // Si el día anterior ya quedó en el anual,
                    // sus temporales ya no son necesarios.
                    if (EstaAgregadoAlAnual(
                            fechaArchivo.Value))
                    {
                        File.Delete(archivo);
                        continue;
                    }

                    // Si no fue archivado, no se pierde:
                    // se mueve a Pendientes.
                    string destino =
                        Path.Combine(
                            RutasInformes.CarpetaPendientes,
                            Path.GetFileName(archivo));

                    destino =
                        ObtenerRutaDisponible(destino);

                    File.Move(
                        archivo,
                        destino);

                    movidos.Add(destino);
                }
                catch (IOException)
                {
                    // Si está abierto, se deja donde está.
                }
                catch (UnauthorizedAccessException)
                {
                    // Si no hay permisos, se deja donde está.
                }
            }

            return movidos;
        }

        // ═══════════════════════════════════════════════════════════════
        // DETECCIÓN Y REEMPLAZO DEL BLOQUE DE UNA FECHA
        // ═══════════════════════════════════════════════════════════════

        private static bool ContieneInformeFecha(
            Body cuerpo,
            DateTime fecha)
        {
            string inicio =
                ObtenerMarcadorInicio(fecha);

            string legacy =
                ObtenerMarcadorLegacy(fecha);

            return cuerpo
                .Descendants<Text>()
                .Any(t =>
                    string.Equals(
                        t.Text,
                        inicio,
                        StringComparison.Ordinal)
                    ||
                    string.Equals(
                        t.Text,
                        legacy,
                        StringComparison.Ordinal));
        }

        private static void EliminarBloqueFecha(
            Body cuerpo,
            DateTime fecha)
        {
            string inicioTexto =
                ObtenerMarcadorInicio(fecha);

            string finTexto =
                ObtenerMarcadorFin(fecha);

            List<OpenXmlElement> elementos =
                cuerpo.ChildElements.ToList();

            int inicio = -1;
            int fin = -1;

            for (int i = 0;
                 i < elementos.Count;
                 i++)
            {
                string texto =
                    ObtenerTextoElemento(
                        elementos[i]);

                if (texto.IndexOf(
                        inicioTexto,
                        StringComparison.Ordinal) >= 0)
                {
                    inicio = i;
                    continue;
                }

                if (inicio >= 0 &&
                    texto.IndexOf(
                        finTexto,
                        StringComparison.Ordinal) >= 0)
                {
                    fin = i;
                    break;
                }
            }

            if (inicio >= 0)
            {
                if (fin < 0)
                {
                    throw new InvalidOperationException(
                        "Se encontró el inicio del informe del día, pero no su marcador final. " +
                        "No se modificó el documento anual para evitar pérdida de información.");
                }

                // Si inmediatamente antes hay un salto de página generado
                // por nosotros, también se elimina para no dejar hojas vacías.
                int indiceAnterior =
                    inicio - 1;

                bool quitarSaltoAnterior =
                    indiceAnterior >= 0 &&
                    EsParrafoSoloSaltoPagina(
                        elementos[indiceAnterior]);

                for (int i = fin;
                     i >= inicio;
                     i--)
                {
                    if (elementos[i]
                        is SectionProperties)
                    {
                        continue;
                    }

                    elementos[i].Remove();
                }

                if (quitarSaltoAnterior &&
                    elementos[indiceAnterior].Parent != null)
                {
                    elementos[indiceAnterior].Remove();
                }

                return;
            }

            // Compatibilidad con documentos creados por una versión previa.
            EliminarBloqueLegacy(
                cuerpo,
                fecha);
        }

        private static void EliminarBloqueLegacy(
            Body cuerpo,
            DateTime fecha)
        {
            string marcadorLegacy =
                ObtenerMarcadorLegacy(fecha);

            List<OpenXmlElement> elementos =
                cuerpo.ChildElements.ToList();

            int inicio = -1;

            for (int i = 0;
                 i < elementos.Count;
                 i++)
            {
                if (ObtenerTextoElemento(
                        elementos[i])
                    .IndexOf(
                        marcadorLegacy,
                        StringComparison.Ordinal) >= 0)
                {
                    inicio = i;
                    break;
                }
            }

            if (inicio < 0)
                return;

            int fin =
                elementos.Count - 1;

            // Un bloque legacy terminaba donde empezaba el siguiente
            // bloque administrado por el sistema.
            for (int i = inicio + 1;
                 i < elementos.Count;
                 i++)
            {
                string texto =
                    ObtenerTextoElemento(
                        elementos[i]);

                if (texto.IndexOf(
                        PrefijoLegacy,
                        StringComparison.Ordinal) >= 0
                    ||
                    texto.IndexOf(
                        PrefijoInicio,
                        StringComparison.Ordinal) >= 0)
                {
                    fin = i - 1;
                    break;
                }
            }

            int indiceAnterior =
                inicio - 1;

            bool quitarSaltoAnterior =
                indiceAnterior >= 0 &&
                EsParrafoSoloSaltoPagina(
                    elementos[indiceAnterior]);

            for (int i = fin;
                 i >= inicio;
                 i--)
            {
                if (elementos[i]
                    is SectionProperties)
                {
                    continue;
                }

                elementos[i].Remove();
            }

            if (quitarSaltoAnterior &&
                elementos[indiceAnterior].Parent != null)
            {
                elementos[indiceAnterior].Remove();
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // OPEN XML HELPERS
        // ═══════════════════════════════════════════════════════════════

        private static void CrearDocumentoAnualSiNoExiste(
            string ruta)
        {
            if (File.Exists(ruta))
                return;

            string? carpeta =
                Path.GetDirectoryName(ruta);

            if (!string.IsNullOrWhiteSpace(carpeta))
                Directory.CreateDirectory(carpeta);

            using WordprocessingDocument documento =
                WordprocessingDocument.Create(
                    ruta,
                    WordprocessingDocumentType.Document);

            MainDocumentPart main =
                documento.AddMainDocumentPart();

            main.Document =
                new Document(new Body());

            main.Document.Save();
        }

        private static void CopiarContenidoDocumento(
            string rutaOrigen,
            Body cuerpoDestino)
        {
            using WordprocessingDocument origen =
                WordprocessingDocument.Open(
                    rutaOrigen,
                    false);

            Body? cuerpoOrigen =
                origen.MainDocumentPart?
                    .Document?
                    .Body;

            if (cuerpoOrigen == null)
            {
                throw new InvalidOperationException(
                    $"El documento '{Path.GetFileName(rutaOrigen)}' no contiene un cuerpo válido.");
            }

            foreach (OpenXmlElement elemento in
                     cuerpoOrigen.ChildElements)
            {
                if (elemento
                    is SectionProperties)
                {
                    continue;
                }

                InsertarAntesDeSeccion(
                    cuerpoDestino,
                    elemento.CloneNode(true));
            }
        }

        private static Paragraph CrearSaltoDePagina()
        {
            return new Paragraph(
                new Run(
                    new Break
                    {
                        Type =
                            BreakValues.Page
                    }));
        }

        private static Paragraph CrearMarcadorOculto(
            string marcador)
        {
            return new Paragraph(
                new Run(
                    new RunProperties(
                        new Vanish()),
                    new Text(marcador)));
        }

        private static void InsertarAntesDeSeccion(
            Body cuerpo,
            OpenXmlElement elemento)
        {
            SectionProperties? seccion =
                cuerpo
                    .Elements<SectionProperties>()
                    .LastOrDefault();

            if (seccion != null)
            {
                cuerpo.InsertBefore(
                    elemento,
                    seccion);
            }
            else
            {
                cuerpo.Append(elemento);
            }
        }

        private static string ObtenerTextoElemento(
            OpenXmlElement elemento)
        {
            return string.Concat(
                elemento
                    .Descendants<Text>()
                    .Select(x => x.Text));
        }

        private static bool EsParrafoSoloSaltoPagina(
            OpenXmlElement elemento)
        {
            if (elemento
                is not Paragraph parrafo)
            {
                return false;
            }

            bool tieneSalto =
                parrafo
                    .Descendants<Break>()
                    .Any(b =>
                        b.Type == null ||
                        b.Type.Value ==
                        BreakValues.Page);

            string texto =
                string.Concat(
                    parrafo
                        .Descendants<Text>()
                        .Select(t => t.Text));

            return tieneSalto &&
                   string.IsNullOrWhiteSpace(texto);
        }

        private static void QuitarSaltosFinalesHuerfanos(
            Body cuerpo)
        {
            while (true)
            {
                OpenXmlElement? ultimo =
                    cuerpo.ChildElements
                        .LastOrDefault(
                            e =>
                                e is not SectionProperties);

                if (ultimo == null ||
                    !EsParrafoSoloSaltoPagina(
                        ultimo))
                {
                    break;
                }

                ultimo.Remove();
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // MARCADORES
        // ═══════════════════════════════════════════════════════════════

        private static string ObtenerMarcadorInicio(
            DateTime fecha) =>
            $"{PrefijoInicio}{fecha:yyyy-MM-dd}";

        private static string ObtenerMarcadorFin(
            DateTime fecha) =>
            $"{PrefijoFin}{fecha:yyyy-MM-dd}";

        private static string ObtenerMarcadorLegacy(
            DateTime fecha) =>
            $"{PrefijoLegacy}{fecha:yyyy-MM-dd}";

        // ═══════════════════════════════════════════════════════════════
        // HELPERS DE ARCHIVOS
        // ═══════════════════════════════════════════════════════════════

        private static DateTime? ObtenerFechaDesdeNombre(
            string nombreArchivo)
        {
            Match match =
                Regex.Match(
                    nombreArchivo ?? string.Empty,
                    @"\d{4}-\d{2}-\d{2}");

            if (!match.Success)
                return null;

            if (DateTime.TryParseExact(
                    match.Value,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime fecha))
            {
                return fecha;
            }

            return null;
        }

        private static string ObtenerRutaDisponible(
            string rutaBase)
        {
            if (!File.Exists(rutaBase))
                return rutaBase;

            string carpeta =
                Path.GetDirectoryName(
                    rutaBase)
                ?? string.Empty;

            string nombre =
                Path.GetFileNameWithoutExtension(
                    rutaBase);

            string extension =
                Path.GetExtension(
                    rutaBase);

            int contador = 1;
            string candidato;

            do
            {
                candidato =
                    Path.Combine(
                        carpeta,
                        $"{nombre}_{contador}{extension}");

                contador++;
            }
            while (File.Exists(candidato));

            return candidato;
        }
    }
}
