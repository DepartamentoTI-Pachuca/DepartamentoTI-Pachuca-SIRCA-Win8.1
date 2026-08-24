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
    public static class EtiquetaPowerPointService
    {
        public static string GenerarArchivoTemporal(
            EtiquetaRegistroData datos,
            int cantidadConjuntos)
        {
            if (datos == null)
                throw new ArgumentNullException(nameof(datos));

            if (cantidadConjuntos < 1)
                cantidadConjuntos = 1;

            string nombrePlantilla = ObtenerNombrePlantilla(datos.TipoCausa);

            string rutaPlantilla = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Resources",
                "Plantillas",
                nombrePlantilla);

            if (!File.Exists(rutaPlantilla))
            {
                throw new FileNotFoundException(
                    $"No se encontró la plantilla {nombrePlantilla}.\n\n" +
                    "Verifica que el archivo esté en Resources\\Plantillas y que " +
                    "Copy to Output Directory esté configurado como Copy if newer.",
                    rutaPlantilla);
            }

            string carpetaTemporal = ObtenerCarpetaTemporal();
            Directory.CreateDirectory(carpetaTemporal);

            string rutaSalida = ObtenerRutaTemporalDisponible(
                carpetaTemporal,
                datos.NoCausa);

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
                        "La plantilla no contiene una lista de diapositivas.");

                SlideId slideIdBase =
                    slideIdList.Elements<SlideId>().FirstOrDefault()
                    ?? throw new InvalidOperationException(
                        "La plantilla no contiene una diapositiva base.");

                SlidePart slideBase =
                    (SlidePart)presentationPart.GetPartById(
                        slideIdBase.RelationshipId!);

                Dictionary<string, string> reemplazos =
                    CrearReemplazos(datos);

                // La plantilla ya contiene una diapositiva con las dos etiquetas.
                ReemplazarMarcadores(slideBase, reemplazos);

                // Cada conjunto adicional equivale a otra diapositiva idéntica.
                for (int i = 1; i < cantidadConjuntos; i++)
                {
                    ClonarSlideDentroDePresentacion(
                        presentationPart,
                        slideBase,
                        slideIdList);
                }

                presentationPart.Presentation.Save();
            }

            return rutaSalida;
        }

        private static string ObtenerNombrePlantilla(string tipoCausa)
        {
            string tipo = (tipoCausa ?? string.Empty).Trim().ToUpperInvariant();

            switch (tipo)
            {
                case "JO":
                    return "EtiquetaJO.pptx";
                case "CP":
                    return "EtiquetaCP.pptx";
                case "C":
                    return "EtiquetaC.pptx";
                default:
                    if (TipoCausaHelper.MuestraNoCausaJuicio(tipoCausa))
                        return "EtiquetaJO.pptx";
                    return "EtiquetaC.pptx";
            }
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

        public static DateTime ObtenerUltimaModificacion(string rutaArchivo)
        {
            if (string.IsNullOrWhiteSpace(rutaArchivo) ||
                !File.Exists(rutaArchivo))
            {
                return DateTime.MinValue;
            }

            return File.GetLastWriteTime(rutaArchivo);
        }

        private static Dictionary<string, string> CrearReemplazos(
            EtiquetaRegistroData datos)
        {
            return new Dictionary<string, string>
            {
                ["{{Imputado}}"] = datos.Imputado ?? string.Empty,
                ["{{Delito}}"] = datos.Delito ?? string.Empty,
                ["{{Agraviado}}"] = datos.Agraviado ?? string.Empty,
                ["{{TipoAudiencia}}"] = datos.TipoAudiencia ?? string.Empty,
                ["{{NoCausa}}"] = datos.NoCausa ?? string.Empty,
                ["{{NoCausaJuicio}}"] = datos.NoCausaJuicio ?? string.Empty,
                ["{{NUC}}"] = datos.NUC ?? string.Empty,
                ["{{FeAudiencia}}"] = datos.FechaAudiencia ?? string.Empty,
                ["{{Hora conclusión}}"] = datos.HoraConclusion ?? string.Empty,
                ["{{Juez}}"] = datos.Juez ?? string.Empty
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
            string noCausa)
        {
            string causaLimpia = LimpiarNombreArchivo(noCausa);

            for (int i = 1; i <= 9999; i++)
            {
                string nombre =
                    $"Etiqueta_{causaLimpia}_{i:000}.pptx";

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
                return "SinCausa";

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
                foreach (KeyValuePair<string, string> reemplazo in reemplazos)
                {
                    ReemplazarEnParrafo(
                        paragraph,
                        reemplazo.Key,
                        reemplazo.Value);
                }
            }

            slidePart.Slide.Save();
        }

        // PowerPoint puede dividir un marcador en varios runs.
        // Este método reemplaza el texto aunque {{Imputado}} esté fragmentado.
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

                string textoCompleto =
                    string.Concat(textos.Select(x => x.Text ?? string.Empty));

                int indice =
                    textoCompleto.IndexOf(
                        marcador,
                        StringComparison.Ordinal);

                if (indice < 0)
                    return;

                int finMarcador = indice + marcador.Length;
                int posicion = 0;
                bool valorInsertado = false;

                foreach (A.Text texto in textos)
                {
                    string contenido = texto.Text ?? string.Empty;
                    int inicioNodo = posicion;
                    int finNodo = posicion + contenido.Length;

                    if (finNodo <= indice || inicioNodo >= finMarcador)
                    {
                        posicion = finNodo;
                        continue;
                    }

                    int corteInicial =
                        Math.Max(0, indice - inicioNodo);

                    int corteFinal =
                        Math.Min(
                            contenido.Length,
                            finMarcador - inicioNodo);

                    string antes =
                        contenido.Substring(0, corteInicial);

                    string despues =
                        contenido.Substring(corteFinal);

                    if (!valorInsertado)
                    {
                        texto.Text = antes + valor + despues;
                        valorInsertado = true;
                    }
                    else
                    {
                        texto.Text = antes + despues;
                    }

                    posicion = finNodo;
                }
            }
        }

        private static void ClonarSlideDentroDePresentacion(
            PresentationPart presentationPart,
            SlidePart slideOrigen,
            SlideIdList slideIdList)
        {
            SlidePart nuevaSlide =
                presentationPart.AddNewPart<SlidePart>();

            CopiarContenidoSlide(slideOrigen, nuevaSlide);

            uint nuevoId = ObtenerSiguienteSlideId(slideIdList);

            slideIdList.Append(new SlideId
            {
                Id = nuevoId,
                RelationshipId =
                    presentationPart.GetIdOfPart(nuevaSlide)
            });
        }

        private static void ImportarSlide(
            PresentationPart presentationPartDestino,
            SlidePart slideOrigen,
            SlideIdList slideIdListDestino)
        {
            SlidePart nuevaSlide =
                presentationPartDestino.AddNewPart<SlidePart>();

            CopiarContenidoSlide(slideOrigen, nuevaSlide);

            uint nuevoId =
                ObtenerSiguienteSlideId(slideIdListDestino);

            slideIdListDestino.Append(new SlideId
            {
                Id = nuevoId,
                RelationshipId =
                    presentationPartDestino.GetIdOfPart(nuevaSlide)
            });
        }

        private static void CopiarContenidoSlide(
            SlidePart slideOrigen,
            SlidePart slideDestino)
        {
            using (Stream origen =
                   slideOrigen.GetStream(FileMode.Open, FileAccess.Read))
            using (Stream destino =
                   slideDestino.GetStream(FileMode.Create, FileAccess.Write))
            {
                origen.CopyTo(destino);
            }

            // Importa layout, imágenes, logos y demás partes relacionadas.
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

        private static void VerificarArchivoNoBloqueado(string rutaArchivo)
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
		
	public static void LimpiarEtiquetasTemporalesAnteriores()
{
    string carpetaTemporal = ObtenerCarpetaTemporal();

    if (!Directory.Exists(carpetaTemporal))
        return;

    DateTime hoy = DateTime.Today;

    foreach (string archivo in Directory.GetFiles(carpetaTemporal, "*.pptx"))
    {
        try
        {
            DateTime fechaArchivo = File.GetCreationTime(archivo);

            if (fechaArchivo.Date < hoy)
                File.Delete(archivo);
        }
        catch
        {
            // Si está abierto o bloqueado, se conserva.
        }
    }
}







    }
}