using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using PoderJudicial.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using A = DocumentFormat.OpenXml.Drawing;

namespace PoderJudicial.Helpers
{
    public static class EtiquetaEjecucionPowerPointService
    {
        private const string NombrePlantilla = "EtiquetaEjecucion.pptx";

        public static string GenerarArchivoTemporal(
            EtiquetaEjecucionData datos,
            int cantidadConjuntos)
        {
            if (datos == null)
                throw new ArgumentNullException(nameof(datos));

            if (cantidadConjuntos < 1)
                cantidadConjuntos = 1;

            string rutaPlantilla = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Resources",
                "Plantillas",
                NombrePlantilla);

            if (!File.Exists(rutaPlantilla))
            {
                throw new FileNotFoundException(
                    $"No se encontró la plantilla {NombrePlantilla}.\n\n" +
                    "Verifica que esté en Resources\\Plantillas y que tenga:\n" +
                    "Build Action = Content\n" +
                    "Copy to Output Directory = Copy if newer.",
                    rutaPlantilla);
            }

            string carpetaTemporal = ObtenerCarpetaTemporal();
            Directory.CreateDirectory(carpetaTemporal);

            string identificador =
                !string.IsNullOrWhiteSpace(datos.Expediente)
                    ? datos.Expediente
                    : datos.Causa;

            string rutaSalida =
                ObtenerRutaTemporalDisponible(
                    carpetaTemporal,
                    identificador);

            File.Copy(rutaPlantilla, rutaSalida, true);

            using (PresentationDocument documento =
                   PresentationDocument.Open(rutaSalida, true))
            {
                PresentationPart presentationPart =
                    documento.PresentationPart
                    ?? throw new InvalidOperationException(
                        "La plantilla no contiene una presentación válida.");

                SlideIdList slideIdList =
                    presentationPart.Presentation.SlideIdList
                    ?? throw new InvalidOperationException(
                        "La plantilla no contiene diapositivas.");

                SlideId slideIdBase =
                    slideIdList.Elements<SlideId>().FirstOrDefault()
                    ?? throw new InvalidOperationException(
                        "La plantilla no contiene una diapositiva base.");

                SlidePart slideBase =
                    (SlidePart)presentationPart.GetPartById(
                        slideIdBase.RelationshipId!);

                Dictionary<string, string> reemplazos =
                    CrearReemplazos(datos);

                ReemplazarMarcadores(slideBase, reemplazos);

                // Cada conjunto adicional = otra diapositiva con las 2 etiquetas.
                for (int i = 1; i < cantidadConjuntos; i++)
                {
                    SlidePart nuevaSlide =
                        ClonarSlideDentroDePresentacion(
                            presentationPart,
                            slideBase,
                            slideIdList);

                    // La diapositiva clonada ya lleva los valores sustituidos,
                    // porque se clona después de reemplazar la diapositiva base.
                    nuevaSlide.Slide.Save();
                }

                presentationPart.Presentation.Save();
            }

            return rutaSalida;
        }

        public static void AbrirArchivo(string rutaArchivo)
        {
            if (string.IsNullOrWhiteSpace(rutaArchivo) ||
                !File.Exists(rutaArchivo))
            {
                throw new FileNotFoundException(
                    "No se encontró el archivo de etiqueta.",
                    rutaArchivo);
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = rutaArchivo,
                UseShellExecute = true
            });
        }

        public static void Consolidar(
            string rutaEtiquetaNueva,
            string rutaPowerPointDestino)
        {
            if (string.IsNullOrWhiteSpace(rutaEtiquetaNueva) ||
                !File.Exists(rutaEtiquetaNueva))
            {
                throw new FileNotFoundException(
                    "No se encontró el archivo de etiqueta generado.",
                    rutaEtiquetaNueva);
            }

            if (string.IsNullOrWhiteSpace(rutaPowerPointDestino) ||
                !File.Exists(rutaPowerPointDestino))
            {
                throw new FileNotFoundException(
                    "No se encontró el PowerPoint seleccionado.",
                    rutaPowerPointDestino);
            }

            string origenCompleto =
                Path.GetFullPath(rutaEtiquetaNueva);

            string destinoCompleto =
                Path.GetFullPath(rutaPowerPointDestino);

            if (string.Equals(
                    origenCompleto,
                    destinoCompleto,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "El archivo de etiqueta y el PowerPoint destino no pueden ser el mismo archivo.");
            }

            VerificarArchivoNoBloqueado(destinoCompleto);

            object? appObject = null;
            object? destinoObject = null;

            try
            {
                Type? tipoPowerPoint =
                    Type.GetTypeFromProgID("PowerPoint.Application");

                if (tipoPowerPoint == null)
                {
                    throw new InvalidOperationException(
                        "Microsoft PowerPoint no está instalado o no está registrado correctamente en Windows.");
                }

                appObject = Activator.CreateInstance(tipoPowerPoint);

                if (appObject == null)
                {
                    throw new InvalidOperationException(
                        "No fue posible iniciar Microsoft PowerPoint.");
                }

                dynamic app = appObject;

                // Los tres valores 0 equivalen a msoFalse:
                // ReadOnly = false, Untitled = false, WithWindow = false.
                destinoObject = app.Presentations.Open(
                    destinoCompleto,
                    0,
                    0,
                    0);

                if (destinoObject == null)
                {
                    throw new InvalidOperationException(
                        "No fue posible abrir el PowerPoint destino.");
                }

                dynamic destino = destinoObject;

                int insertarDespuesDe =
                    (int)destino.Slides.Count;

                destino.Slides.InsertFromFile(
                    origenCompleto,
                    insertarDespuesDe);

                destino.Save();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "No fue posible consolidar la etiqueta en el PowerPoint seleccionado.\n\n" +
                    "Verifica que Microsoft PowerPoint esté instalado, que el archivo destino esté cerrado " +
                    "y que ambos archivos sean presentaciones válidas.\n\n" +
                    "Detalle: " + ex.Message,
                    ex);
            }
            finally
            {
                if (destinoObject != null)
                {
                    try
                    {
                        ((dynamic)destinoObject).Close();
                    }
                    catch
                    {
                    }

                    if (System.Runtime.InteropServices.Marshal.IsComObject(destinoObject))
                    {
                        System.Runtime.InteropServices.Marshal.FinalReleaseComObject(
                            destinoObject);
                    }

                    destinoObject = null;
                }

                if (appObject != null)
                {
                    try
                    {
                        ((dynamic)appObject).Quit();
                    }
                    catch
                    {
                    }

                    if (System.Runtime.InteropServices.Marshal.IsComObject(appObject))
                    {
                        System.Runtime.InteropServices.Marshal.FinalReleaseComObject(
                            appObject);
                    }

                    appObject = null;
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        public static void LimpiarEtiquetasTemporalesAnteriores()
        {
            string carpetaTemporal = ObtenerCarpetaTemporal();

            if (!Directory.Exists(carpetaTemporal))
                return;

            DateTime hoy = DateTime.Today;

            foreach (string archivo in
                     Directory.GetFiles(
                         carpetaTemporal,
                         "EtiquetaEjecucion_*.pptx"))
            {
                try
                {
                    DateTime fecha = File.GetCreationTime(archivo);

                    if (fecha.Date < hoy)
                        File.Delete(archivo);
                }
                catch
                {
                    // Si está abierto, bloqueado o no puede eliminarse,
                    // se conserva y se intentará en otra ocasión.
                }
            }
        }

        private static Dictionary<string, string> CrearReemplazos(
            EtiquetaEjecucionData datos)
        {
            // Los marcadores de tu plantilla usan una mezcla de mayúsculas
            // y minúsculas. El reemplazo se hace sin distinguir mayúsculas.
            return new Dictionary<string, string>
            {
                ["{{imputado}}"] = datos.Imputado ?? string.Empty,
                ["{{delito}}"] = datos.Delito ?? string.Empty,
                ["{{victima}}"] = datos.Victima ?? string.Empty,
                ["{{tipoAudiencia}}"] = datos.TipoAudiencia ?? string.Empty,
                ["{{expediente}}"] = datos.Expediente ?? string.Empty,
                ["{{causa}}"] = datos.Causa ?? string.Empty,
                ["{{Juzgado}}"] = datos.Juzgado ?? string.Empty,
                ["{{fechaAudiencia}}"] = datos.FechaAudiencia ?? string.Empty,
                ["{{horaTermino}}"] = datos.HoraTermino ?? string.Empty,
                ["{{juez}}"] = datos.Juez ?? string.Empty
            };
        }

        private static string ObtenerCarpetaTemporal()
        {
            return Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "PoderJudicial",
                "EtiquetasTemporales");
        }

        private static string ObtenerRutaTemporalDisponible(
            string carpeta,
            string identificador)
        {
            string limpio = LimpiarNombreArchivo(identificador);

            for (int i = 1; i <= 9999; i++)
            {
                string nombre =
                    $"EtiquetaEjecucion_{limpio}_{i:000}.pptx";

                string ruta = Path.Combine(carpeta, nombre);

                if (!File.Exists(ruta))
                    return ruta;
            }

            throw new IOException(
                "No fue posible obtener un nombre disponible para la etiqueta.");
        }

        private static string LimpiarNombreArchivo(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return "SinExpediente";

            string resultado = texto.Trim();

            foreach (char c in Path.GetInvalidFileNameChars())
                resultado = resultado.Replace(c, '-');

            resultado = resultado.Replace("/", "-");

            return resultado;
        }

        private static void ReemplazarMarcadores(
            SlidePart slidePart,
            Dictionary<string, string> reemplazos)
        {
            foreach (A.Paragraph paragraph in
                     slidePart.Slide.Descendants<A.Paragraph>())
            {
                foreach (KeyValuePair<string, string> item in reemplazos)
                {
                    ReemplazarEnParrafo(
                        paragraph,
                        item.Key,
                        item.Value);
                }
            }

            slidePart.Slide.Save();
        }

        // PowerPoint puede dividir {{marcador}} en varios runs.
        private static void ReemplazarEnParrafo(
            A.Paragraph paragraph,
            string marcador,
            string valor)
        {
            while (true)
            {
                List<A.Text> textos =
                    paragraph.Descendants<A.Text>().ToList();

                if (textos.Count == 0)
                    return;

                string completo =
                    string.Concat(
                        textos.Select(x => x.Text ?? string.Empty));

                int inicio = completo.IndexOf(
                    marcador,
                    StringComparison.OrdinalIgnoreCase);

                if (inicio < 0)
                    return;

                int fin = inicio + marcador.Length;
                int posicion = 0;
                bool insertado = false;

                foreach (A.Text texto in textos)
                {
                    string actual = texto.Text ?? string.Empty;
                    int nodoInicio = posicion;
                    int nodoFin = posicion + actual.Length;

                    if (nodoFin <= inicio || nodoInicio >= fin)
                    {
                        posicion = nodoFin;
                        continue;
                    }

                    int corteInicio =
                        Math.Max(0, inicio - nodoInicio);

                    int corteFin =
                        Math.Min(
                            actual.Length,
                            fin - nodoInicio);

                    string antes =
                        actual.Substring(0, corteInicio);

                    string despues =
                        actual.Substring(corteFin);

                    if (!insertado)
                    {
                        texto.Text = antes + valor + despues;
                        insertado = true;
                    }
                    else
                    {
                        texto.Text = antes + despues;
                    }

                    posicion = nodoFin;
                }
            }
        }

        private static SlidePart ClonarSlideDentroDePresentacion(
            PresentationPart presentationPart,
            SlidePart slideOrigen,
            SlideIdList slideIdList)
        {
            SlidePart nuevaSlide =
                presentationPart.AddNewPart<SlidePart>();

            CopiarContenidoSlide(slideOrigen, nuevaSlide);

            slideIdList.Append(new SlideId
            {
                Id = ObtenerSiguienteSlideId(slideIdList),
                RelationshipId =
                    presentationPart.GetIdOfPart(nuevaSlide)
            });

            return nuevaSlide;
        }

        private static void ImportarSlide(
            PresentationPart presentationPartDestino,
            SlidePart slideOrigen,
            SlideIdList slideIdListDestino)
        {
            SlidePart nuevaSlide =
                presentationPartDestino.AddNewPart<SlidePart>();

            CopiarContenidoSlide(slideOrigen, nuevaSlide);

            slideIdListDestino.Append(new SlideId
            {
                Id = ObtenerSiguienteSlideId(slideIdListDestino),
                RelationshipId =
                    presentationPartDestino.GetIdOfPart(nuevaSlide)
            });
        }

        private static void CopiarContenidoSlide(
            SlidePart slideOrigen,
            SlidePart slideDestino)
        {
            using (Stream origen =
                   slideOrigen.GetStream(
                       FileMode.Open,
                       FileAccess.Read))
            using (Stream destino =
                   slideDestino.GetStream(
                       FileMode.Create,
                       FileAccess.Write))
            {
                origen.CopyTo(destino);
            }

            foreach (IdPartPair parte in slideOrigen.Parts)
            {
                slideDestino.AddPart(
                    parte.OpenXmlPart,
                    parte.RelationshipId);
            }

            foreach (HyperlinkRelationship relacion
                     in slideOrigen.HyperlinkRelationships)
            {
                slideDestino.AddHyperlinkRelationship(
                    relacion.Uri,
                    relacion.IsExternal,
                    relacion.Id);
            }

            foreach (ExternalRelationship relacion
                     in slideOrigen.ExternalRelationships)
            {
                slideDestino.AddExternalRelationship(
                    relacion.RelationshipType,
                    relacion.Uri,
                    relacion.Id);
            }
        }

        private static uint ObtenerSiguienteSlideId(
            SlideIdList slideIdList)
        {
            uint maximo =
                slideIdList.Elements<SlideId>()
                    .Where(x => x.Id != null)
                    .Select(x => x.Id!.Value)
                    .DefaultIfEmpty(255U)
                    .Max();

            return maximo + 1U;
        }

        private static SlideIdList CrearSlideIdList(
            PresentationPart presentationPart)
        {
            SlideIdList lista = new SlideIdList();
            presentationPart.Presentation.Append(lista);
            return lista;
        }

        private static void VerificarArchivoNoBloqueado(
            string rutaArchivo)
        {
            try
            {
                using FileStream stream = new FileStream(
                    rutaArchivo,
                    FileMode.Open,
                    FileAccess.ReadWrite,
                    FileShare.None);
            }
            catch (IOException)
            {
                throw new IOException(
                    "El PowerPoint destino parece estar abierto o bloqueado.\n\n" +
                    "Ciérralo en PowerPoint y vuelve a intentar la consolidación.");
            }
        }
    }
}