using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using PoderJudicial.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;
using WP = DocumentFormat.OpenXml.Wordprocessing;

namespace PoderJudicial.Helpers
{
    public static class WordExporter
    {
        private const string CargoEntrega =
            "Jefe de Unidad de Informática del Juzgado en Materia Penal " +
            "Acusatorio y Oral para el Circuito de Pachuca de Soto, Hidalgo.";

        private const string CargoRecibeAutenticas =
            "Jefe de Unidad de Causa del Juzgado en Materia Penal " +
            "Acusatorio y Oral para el Circuito de Pachuca de Soto, Hidalgo.";

        private const string CargoRecibeSimples =
            "Encargado de Atención Ciudadana del Juzgado en Materia Penal " +
            "Acusatorio y Oral para el Circuito de Pachuca de Soto, Hidalgo.";

        public static void GenerarInformeCopias(
            List<RegistroCopia> datos,
            string tipoTitulo,
            string tipoDvd,
            string entrego,
            IEnumerable<string> recibieron,
            string rutaArchivo,
            DateTime fechaInforme)
        {
            if (datos == null || datos.Count == 0)
            {
                throw new ArgumentException(
                    "No existen registros para generar el informe.",
                    nameof(datos));
            }

            if (string.IsNullOrWhiteSpace(entrego))
            {
                throw new ArgumentException(
                    "Debe indicar quién realizó la entrega.",
                    nameof(entrego));
            }

            List<string> listaRecibieron =
                (recibieron ?? Enumerable.Empty<string>())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

            if (listaRecibieron.Count == 0)
            {
                throw new ArgumentException(
                    "Debe indicar al menos una persona que recibió.",
                    nameof(recibieron));
            }

            string? carpeta =
                Path.GetDirectoryName(rutaArchivo);

            if (!string.IsNullOrWhiteSpace(carpeta))
                Directory.CreateDirectory(carpeta);

            if (File.Exists(rutaArchivo))
                File.Delete(rutaArchivo);

            using WordprocessingDocument documento =
                WordprocessingDocument.Create(
                    rutaArchivo,
                    WordprocessingDocumentType.Document);

            MainDocumentPart partePrincipal =
    documento.AddMainDocumentPart();

            partePrincipal.Document =
                new Document(new Body());

            Body cuerpo =
                partePrincipal.Document.Body!;

            // ======================================================
            // ENCABEZADO CON LOGOS
            // ======================================================

            string rutaLogo1 = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Resources",
                "logo1.png");

            string rutaLogo2 = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Resources",
                "logo2.png");

            AgregarEncabezadoConLogos(
                partePrincipal,
                cuerpo,
                rutaLogo1,
                rutaLogo2);

            // ======================================================
            // CONTENIDO NORMAL DEL DOCUMENTO
            // ======================================================

            AgregarSeccion(
                cuerpo,
                datos,
                tipoTitulo,
                tipoDvd,
                entrego.Trim(),
                listaRecibieron,
                fechaInforme);

            partePrincipal.Document.Save();
        }

        private static void AgregarSeccion(
            Body cuerpo,
            List<RegistroCopia> datos,
            string tipoTitulo,
            string tipoDvd,
            string entrego,
            IReadOnlyCollection<string> recibieron,
            DateTime fechaInforme)
        {
            CultureInfo cultura =
                new CultureInfo("es-MX");

            int total =
                datos.Sum(
                    x => x.TotDiscosEntregados ?? 0);

            AgregarParrafo(
                cuerpo,
                $"Entrega de {tipoDvd} de {tipoTitulo}",
                true,
                24,
                JustificationValues.Center);

            AgregarParrafo(
                cuerpo,
                "Unidad de Informática",
                true,
                24,
                JustificationValues.Center);

            AgregarParrafo(
                cuerpo,
                $"Pachuca de Soto, Hgo., " +
                $"{fechaInforme.ToString("d 'de' MMMM 'de' yyyy", cultura)}",
                true,
                20,
                JustificationValues.Right);

            string introduccion =
                $"Se hace entrega de {total} {tipoDvd} ({total} {tipoTitulo}),";

            string continuacion =
                " que contiene la videograbación de audiencias del Juzgado en Materia " +
                "Penal Acusatorio y Oral para el Circuito de Pachuca de Soto, Hidalgo, " +
                "debidamente revisado y etiquetado, para los efectos legales que correspondan " +
                "a solicitud del Jefe de Unidad de Causa o las Partes.";

            AgregarParrafoMixto(
                cuerpo,
                introduccion,
                continuacion,
                18,
                JustificationValues.Both);

            AgregarParrafo(
                cuerpo,
                "Información General de la Video Audiencia:",
                true,
                18,
                JustificationValues.Left);

            bool esCopiasSimples = string.Equals(
                tipoTitulo,
                "Copias Simples",
                StringComparison.OrdinalIgnoreCase);

            Table tabla =
                CrearTablaPrincipal(esCopiasSimples);

            var encabezados = new List<string>
            {
                "Causa",
                "NUC",
                "Fecha",
                "Tipo de Copia",
                "Solicita",
                "Discos",
                "Observaciones"
            };

            if (esCopiasSimples)
                encabezados.Add("Nombre y Firma de quien recibe / Fecha");

            List<OpenXmlElement> celdasEncabezado = encabezados
                .Select(x => (OpenXmlElement)CrearCeldaEncabezado(x))
                .ToList();
            if (esCopiasSimples)
                AplicarAnchosCopiasSimples(celdasEncabezado);

            tabla.Append(new TableRow(celdasEncabezado));

            foreach (RegistroCopia copia in datos)
            {
                var celdas = new List<OpenXmlElement>
                {
                        CrearCeldaDato(copia.NoCausa),
                        CrearCeldaDato(copia.NUC),
                        CrearCeldaDato(
                            copia.FeAudiencia?
                                .ToString(
                                    "dddd, d 'de' MMMM 'de' yyyy",
                                    cultura)
                            ?? string.Empty),
                        CrearCeldaDato(copia.TipoDisco),
                        CrearCeldaDato(copia.AQuienSeEntrega),
                        CrearCeldaDato(
                            (copia.TotDiscosEntregados ?? 0)
                                .ToString()),
                        CrearCeldaDato(copia.Observaciones)
                };

                if (esCopiasSimples)
                {
                    celdas.Add(CrearCeldaDato(string.Empty));
                    AplicarAnchosCopiasSimples(celdas);
                }

                tabla.Append(new TableRow(celdas));
            }

            var celdasTotal = new List<OpenXmlElement>
            {
                    CrearCeldaTotal(""),
                    CrearCeldaTotal(""),
                    CrearCeldaTotal(""),
                    CrearCeldaTotal(""),
                    CrearCeldaTotal("TOTAL"),
                    CrearCeldaTotal(total.ToString()),
                    CrearCeldaTotal("")
            };

            if (esCopiasSimples)
            {
                celdasTotal.Add(CrearCeldaTotal(string.Empty));
                AplicarAnchosCopiasSimples(celdasTotal);
            }

            tabla.Append(new TableRow(celdasTotal));

            cuerpo.Append(tabla);

            AgregarEspacio(
                cuerpo,
                12);

            string cargoRecibe =
                ObtenerCargoRecibe(tipoTitulo);

            AgregarTablaEntregaRecibe(
                cuerpo,
                entrego,
                recibieron,
                cargoRecibe);
        }

        private static Table CrearTablaPrincipal(bool esCopiasSimples)
        {
            var propiedades =
                new TableProperties(
                    new TableWidth
                    {
                        Type = TableWidthUnitValues.Dxa,
                        Width = "11066"
                    },
                    new TableJustification
                    {
                        Val = TableRowAlignmentValues.Center
                    },
                    new TableLayout
                    {
                        Type = esCopiasSimples
                            ? TableLayoutValues.Fixed
                            : TableLayoutValues.Autofit
                    },
                    new TableBorders(
                        new TopBorder
                        {
                            Val = BorderValues.Single,
                            Size = 6
                        },
                        new BottomBorder
                        {
                            Val = BorderValues.Single,
                            Size = 6
                        },
                        new LeftBorder
                        {
                            Val = BorderValues.Single,
                            Size = 6
                        },
                        new RightBorder
                        {
                            Val = BorderValues.Single,
                            Size = 6
                        },
                        new InsideHorizontalBorder
                        {
                            Val = BorderValues.Single,
                            Size = 4
                        },
                        new InsideVerticalBorder
                        {
                            Val = BorderValues.Single,
                            Size = 4
                        }));

            var tabla = new Table(propiedades);

            if (esCopiasSimples)
            {
                tabla.Append(new TableGrid(
                    new GridColumn { Width = "1160" },
                    new GridColumn { Width = "1450" },
                    new GridColumn { Width = "2070" },
                    new GridColumn { Width = "1260" },
                    new GridColumn { Width = "1000" },
                    new GridColumn { Width = "860" },
                    new GridColumn { Width = "1420" },
                    new GridColumn { Width = "1846" }));
            }

            return tabla;
        }

        private static void AplicarAnchosCopiasSimples(
            IReadOnlyList<OpenXmlElement> celdas)
        {
            string[] anchos =
            {
                "1160", "1450", "2070", "1260",
                "1000", "860", "1420", "1846"
            };

            for (int i = 0; i < celdas.Count && i < anchos.Length; i++)
            {
                if (celdas[i] is not TableCell celda)
                    continue;

                celda.TableCellProperties ??= new TableCellProperties();
                celda.TableCellProperties.PrependChild(
                    new TableCellWidth
                    {
                        Type = TableWidthUnitValues.Dxa,
                        Width = anchos[i]
                    });
            }
        }

        private static TableCell CrearCeldaEncabezado(
            string texto)
        {
            var runProps =
                CrearPropiedadesTexto(
                    20,
                    true);

            var parrafo =
                CrearParrafoTabla(
                    texto,
                    runProps);

            return new TableCell(
                new TableCellProperties(
                    new Shading
                    {
                        Val = ShadingPatternValues.Clear,
                        Color = "auto",
                        Fill = "C0C0C0"
                    },
                    new TableCellVerticalAlignment
                    {
                        Val = TableVerticalAlignmentValues.Center
                    }),
                parrafo);
        }

        private static TableCell CrearCeldaDato(
            string texto)
        {
            var runProps =
                CrearPropiedadesTexto(
                    12,
                    false);

            var parrafo =
                CrearParrafoTabla(
                    texto,
                    runProps);

            return new TableCell(
                new TableCellProperties(
                    new TableCellVerticalAlignment
                    {
                        Val = TableVerticalAlignmentValues.Center
                    }),
                parrafo);
        }

        private static TableCell CrearCeldaTotal(
            string texto)
        {
            var runProps =
                CrearPropiedadesTexto(
                    18,
                    true);

            var parrafo =
                CrearParrafoTabla(
                    texto,
                    runProps);

            // Sin sombreado.
            return new TableCell(
                new TableCellProperties(
                    new TableCellVerticalAlignment
                    {
                        Val = TableVerticalAlignmentValues.Center
                    }),
                parrafo);
        }

        private static Paragraph CrearParrafoTabla(
            string texto,
            RunProperties propiedadesTexto)
        {
            return new Paragraph(
                new ParagraphProperties(
                    new Justification
                    {
                        Val = JustificationValues.Center
                    },
                    new SpacingBetweenLines
                    {
                        Before = "0",
                        After = "0"
                    }),
                new Run(
                    propiedadesTexto,
                    new Text(texto ?? string.Empty)
                    {
                        Space = SpaceProcessingModeValues.Preserve
                    }));
        }

        private static RunProperties CrearPropiedadesTexto(
            int tamanoOpenXml,
            bool negrita)
        {
            var propiedades =
                new RunProperties(
                    new RunFonts
                    {
                        Ascii = "Calibri",
                        HighAnsi = "Calibri",
                        EastAsia = "Calibri"
                    },
                    new FontSize
                    {
                        Val = tamanoOpenXml.ToString()
                    },
                    new FontSizeComplexScript
                    {
                        Val = tamanoOpenXml.ToString()
                    });

            if (negrita)
                propiedades.Append(new Bold());

            return propiedades;
        }

        private static void AgregarTablaEntregaRecibe(
            Body cuerpo,
            string entrego,
            IReadOnlyCollection<string> recibieron,
            string cargoRecibe)
        {
            var tabla =
                new Table(
                    new TableProperties(
                        new TableWidth
                        {
                            Type = TableWidthUnitValues.Pct,
                            Width = "5000"
                        },
                        new TableJustification
                        {
                            Val = TableRowAlignmentValues.Center
                        },
                        new TableLayout
                        {
                            Type = TableLayoutValues.Fixed
                        },
                        new TableBorders(
                            new TopBorder
                            {
                                Val = BorderValues.Nil
                            },
                            new BottomBorder
                            {
                                Val = BorderValues.Nil
                            },
                            new LeftBorder
                            {
                                Val = BorderValues.Nil
                            },
                            new RightBorder
                            {
                                Val = BorderValues.Nil
                            },
                            new InsideHorizontalBorder
                            {
                                Val = BorderValues.Nil
                            },
                            new InsideVerticalBorder
                            {
                                Val = BorderValues.Nil
                            })));

            var fila =
                new TableRow();

            fila.Append(
                CrearCeldaFirma(
                    "Entrega",
                    new[] { entrego },
                    CargoEntrega),
                CrearCeldaFirma(
                    "Recibe",
                    recibieron,
                    cargoRecibe));

            tabla.Append(fila);
            cuerpo.Append(tabla);
        }

        private static TableCell CrearCeldaFirma(
            string titulo,
            IEnumerable<string> nombres,
            string cargo)
        {
            var celda =
                new TableCell(
                    new TableCellProperties(
                        new TableCellWidth
                        {
                            Type = TableWidthUnitValues.Pct,
                            Width = "2500"
                        },
                        new TableCellVerticalAlignment
                        {
                            Val = TableVerticalAlignmentValues.Top
                        },
                        new TableCellBorders(
                            new TopBorder
                            {
                                Val = BorderValues.Nil
                            },
                            new BottomBorder
                            {
                                Val = BorderValues.Nil
                            },
                            new LeftBorder
                            {
                                Val = BorderValues.Nil
                            },
                            new RightBorder
                            {
                                Val = BorderValues.Nil
                            },
                            new InsideHorizontalBorder
                            {
                                Val = BorderValues.Nil
                            },
                            new InsideVerticalBorder
                            {
                                Val = BorderValues.Nil
                            })));

            celda.Append(
                CrearParrafoFirma(
                    titulo,
                    true,
                    16));

            foreach (string nombre in
                     (nombres ?? Enumerable.Empty<string>())
                         .Where(x => !string.IsNullOrWhiteSpace(x))
                         .Select(x => x.Trim())
                         .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                celda.Append(
                    CrearParrafoFirma(
                        nombre,
                        false,
                        16));
            }

            celda.Append(
                CrearParrafoFirma(
                    cargo,
                    false,
                    16));

            return celda;
        }

        private static Paragraph CrearParrafoFirma(
            string texto,
            bool negrita,
            int tamanoOpenXml)
        {
            RunProperties props =
                CrearPropiedadesTexto(
                    tamanoOpenXml,
                    negrita);

            return new Paragraph(
                new ParagraphProperties(
                    new Justification
                    {
                        Val = JustificationValues.Center
                    },
                    new SpacingBetweenLines
                    {
                        Before = "0",
                        After = "0"
                    }),
                new Run(
                    props,
                    new Text(texto ?? string.Empty)
                    {
                        Space = SpaceProcessingModeValues.Preserve
                    }));
        }

        private static string ObtenerCargoRecibe(
            string tipoTitulo)
        {
            string normalizado =
                NormalizarTexto(tipoTitulo);

            return normalizado.Contains("AUT")
                ? CargoRecibeAutenticas
                : CargoRecibeSimples;
        }

        private static string NormalizarTexto(
            string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return string.Empty;

            string texto =
                valor
                    .Trim()
                    .ToUpperInvariant()
                    .Normalize(
                        System.Text.NormalizationForm.FormD);

            IEnumerable<char> caracteres =
                texto.Where(c =>
                    CharUnicodeInfo.GetUnicodeCategory(c)
                    != UnicodeCategory.NonSpacingMark);

            return new string(caracteres.ToArray())
                .Normalize(
                    System.Text.NormalizationForm.FormC);
        }

        private static void AgregarEspacio(
            Body cuerpo,
            int tamanoOpenXml)
        {
            cuerpo.Append(
                new Paragraph(
                    new Run(
                        new RunProperties(
                            new FontSize
                            {
                                Val = tamanoOpenXml.ToString()
                            }),
                        new Text(" "))));
        }

        private static void AgregarParrafo(
            Body cuerpo,
            string texto,
            bool negrita,
            int tamanoOpenXml,
            JustificationValues alineacion)
        {
            RunProperties props =
                CrearPropiedadesTexto(
                    tamanoOpenXml,
                    negrita);

            cuerpo.Append(
                new Paragraph(
                    new ParagraphProperties(
                        new Justification
                        {
                            Val = alineacion
                        },
                        new SpacingBetweenLines
                        {
                            Before = "0",
                            After = "0"
                        }),
                    new Run(
                        props,
                        new Text(texto ?? string.Empty)
                        {
                            Space = SpaceProcessingModeValues.Preserve
                        })));
        }

        private static void AgregarParrafoMixto(
            Body cuerpo,
            string textoNegrita,
            string textoNormal,
            int tamanoOpenXml,
            JustificationValues alineacion)
        {
            RunProperties propsNegrita =
                CrearPropiedadesTexto(
                    tamanoOpenXml,
                    true);

            RunProperties propsNormal =
                CrearPropiedadesTexto(
                    tamanoOpenXml,
                    false);

            cuerpo.Append(
                new Paragraph(
                    new ParagraphProperties(
                        new Justification
                        {
                            Val = alineacion
                        },
                        new SpacingBetweenLines
                        {
                            Before = "0",
                            After = "0"
                        }),
                    new Run(
                        propsNegrita,
                        new Text(textoNegrita ?? string.Empty)
                        {
                            Space = SpaceProcessingModeValues.Preserve
                        }),
                    new Run(
                        propsNormal,
                        new Text(textoNormal ?? string.Empty)
                        {
                            Space = SpaceProcessingModeValues.Preserve
                        })));
        }


        private static void AgregarEncabezadoConLogos(
    MainDocumentPart partePrincipal,
    Body cuerpo,
    string rutaLogo1,
    string rutaLogo2)
        {
            if (!File.Exists(rutaLogo1))
                throw new FileNotFoundException(
                    "No se encontró el logo 1.",
                    rutaLogo1);

            if (!File.Exists(rutaLogo2))
                throw new FileNotFoundException(
                    "No se encontró el logo 2.",
                    rutaLogo2);

            HeaderPart headerPart =
                partePrincipal.AddNewPart<HeaderPart>();

            ImagePart imagePartLogo1 =
                headerPart.AddImagePart(ImagePartType.Png);

            using (FileStream stream =
                   new FileStream(
                       rutaLogo1,
                       FileMode.Open,
                       FileAccess.Read))
            {
                imagePartLogo1.FeedData(stream);
            }

            ImagePart imagePartLogo2 =
                headerPart.AddImagePart(ImagePartType.Png);

            using (FileStream stream =
                   new FileStream(
                       rutaLogo2,
                       FileMode.Open,
                       FileAccess.Read))
            {
                imagePartLogo2.FeedData(stream);
            }

            string relacionLogo1 =
                headerPart.GetIdOfPart(imagePartLogo1);

            string relacionLogo2 =
                headerPart.GetIdOfPart(imagePartLogo2);

            // ==================================================
            // MEDIDAS EXACTAS DE LOS LOGOS
            //
            // 1 cm = 360000 EMU
            //
            // Logo 1:
            // ancho = 2.86 cm
            // alto  = 2.29 cm
            //
            // Logo 2:
            // ancho = 2.90 cm
            // alto  = 2.38 cm
            // ==================================================

            long anchoLogo1 = 1029600L;
            long altoLogo1 = 824400L;

            long anchoLogo2 = 1044000L;
            long altoLogo2 = 856800L;

            Drawing dibujoLogo1 =
                CrearImagenEncabezado(
                    relacionLogo1,
                    "Logo Poder Judicial",
                    1U,
                    anchoLogo1,
                    altoLogo1);

            Drawing dibujoLogo2 =
                CrearImagenEncabezado(
                    relacionLogo2,
                    "Logo Consejo de la Judicatura",
                    2U,
                    anchoLogo2,
                    altoLogo2);

            // ==================================================
            // TABLA DEL ENCABEZADO
            // Izquierda: logo1
            // Derecha: logo2
            // Sin bordes
            // ==================================================

            Table tablaEncabezado =
                new Table(
                    new TableProperties(
                        new TableWidth
                        {
                            Type = TableWidthUnitValues.Pct,
                            Width = "5000"
                        },
                        new TableLayout
                        {
                            Type = TableLayoutValues.Fixed
                        },
                        new TableBorders(
                            new TopBorder
                            {
                                Val = BorderValues.Nil
                            },
                            new BottomBorder
                            {
                                Val = BorderValues.Nil
                            },
                            new LeftBorder
                            {
                                Val = BorderValues.Nil
                            },
                            new RightBorder
                            {
                                Val = BorderValues.Nil
                            },
                            new InsideHorizontalBorder
                            {
                                Val = BorderValues.Nil
                            },
                            new InsideVerticalBorder
                            {
                                Val = BorderValues.Nil
                            })));

            TableRow fila =
                new TableRow();

            // --------------------------------------------------
            // CELDA IZQUIERDA
            // --------------------------------------------------

            TableCell celdaIzquierda =
                new TableCell(
                    new TableCellProperties(
                        new TableCellWidth
                        {
                            Type = TableWidthUnitValues.Pct,
                            Width = "2500"
                        },
                        new TableCellVerticalAlignment
                        {
                            Val = TableVerticalAlignmentValues.Center
                        }),
                    new Paragraph(
                        new ParagraphProperties(
                            new Justification
                            {
                                Val = JustificationValues.Left
                            },
                            new SpacingBetweenLines
                            {
                                Before = "0",
                                After = "0"
                            }),
                        new Run(dibujoLogo1)));

            // --------------------------------------------------
            // CELDA DERECHA
            // --------------------------------------------------

            TableCell celdaDerecha =
                new TableCell(
                    new TableCellProperties(
                        new TableCellWidth
                        {
                            Type = TableWidthUnitValues.Pct,
                            Width = "2500"
                        },
                        new TableCellVerticalAlignment
                        {
                            Val = TableVerticalAlignmentValues.Center
                        }),
                    new Paragraph(
                        new ParagraphProperties(
                            new Justification
                            {
                                Val = JustificationValues.Right
                            },
                            new SpacingBetweenLines
                            {
                                Before = "0",
                                After = "0"
                            }),
                        new Run(dibujoLogo2)));

            fila.Append(
                celdaIzquierda,
                celdaDerecha);

            tablaEncabezado.Append(fila);

            Header header =
                new Header();

            header.Append(tablaEncabezado);

            headerPart.Header = header;
            headerPart.Header.Save();

            // ==================================================
            // CONFIGURACIÓN DE SECCIÓN
            // Encabezado desde arriba = 1.25 cm
            // ==================================================

            string relacionHeader =
                partePrincipal.GetIdOfPart(headerPart);

            SectionProperties sectionProperties =
                cuerpo.Elements<SectionProperties>()
                      .LastOrDefault();

            if (sectionProperties == null)
            {
                sectionProperties =
                    new SectionProperties();

                cuerpo.Append(sectionProperties);
            }

            // Referencia al encabezado
            sectionProperties.PrependChild(
                new HeaderReference
                {
                    Type = HeaderFooterValues.Default,
                    Id = relacionHeader
                });

            // 1.25 cm ≈ 709 twips
            PageMargin? margen =
                sectionProperties.GetFirstChild<PageMargin>();

            if (margen == null)
            {
                margen =
                    new PageMargin
                    {
                        Top = 1440,
                        Right = 1440U,
                        Bottom = 1440,
                        Left = 1440U,
                        Header = 709U,
                        Footer = 709U,
                        Gutter = 0U
                    };

                sectionProperties.Append(margen);
            }
            else
            {
                margen.Header = 709U;
            }
        }


        private static Drawing CrearImagenEncabezado(
    string relacionImagen,
    string nombreImagen,
    uint idImagen,
    long ancho,
    long alto)
        {
            return new Drawing(
                new DW.Inline(
                    new DW.Extent
                    {
                        Cx = ancho,
                        Cy = alto
                    },

                    new DW.EffectExtent
                    {
                        LeftEdge = 0L,
                        TopEdge = 0L,
                        RightEdge = 0L,
                        BottomEdge = 0L
                    },

                    new DW.DocProperties
                    {
                        Id = idImagen,
                        Name = nombreImagen
                    },

                    new DW.NonVisualGraphicFrameDrawingProperties(
                        new A.GraphicFrameLocks
                        {
                            NoChangeAspect = true
                        }),

                    new A.Graphic(
                        new A.GraphicData(
                            new PIC.Picture(

                                new PIC.NonVisualPictureProperties(
                                    new PIC.NonVisualDrawingProperties
                                    {
                                        Id = idImagen,
                                        Name = nombreImagen
                                    },
                                    new PIC.NonVisualPictureDrawingProperties()),

                                new PIC.BlipFill(
                                    new A.Blip
                                    {
                                        Embed = relacionImagen,
                                        CompressionState =
                                            A.BlipCompressionValues.Print
                                    },
                                    new A.Stretch(
                                        new A.FillRectangle())),

                                new PIC.ShapeProperties(
                                    new A.Transform2D(
                                        new A.Offset
                                        {
                                            X = 0L,
                                            Y = 0L
                                        },
                                        new A.Extents
                                        {
                                            Cx = ancho,
                                            Cy = alto
                                        }),
                                    new A.PresetGeometry(
                                        new A.AdjustValueList())
                                    {
                                        Preset =
                                            A.ShapeTypeValues.Rectangle
                                    })))
                        {
                            Uri =
                                "http://schemas.openxmlformats.org/drawingml/2006/picture"
                        }))
                {
                    DistanceFromTop = 0U,
                    DistanceFromBottom = 0U,
                    DistanceFromLeft = 0U,
                    DistanceFromRight = 0U,
                    EditId = "50D07946"
                });
        }
    }
}
