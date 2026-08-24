using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using PoderJudicial.Models;

namespace PoderJudicial.Helpers
{
    public static class PdfExporter
    {
        /// Genera un archivo .html con formato de tabla lista para imprimir como PDF
        /// desde el navegador (Ctrl+P → Guardar como PDF).
        /// </summary>
        /// <summary>
        /// Genera un archivo .html con formato de tabla lista para imprimir como PDF
        /// desde el navegador (Ctrl+P → Guardar como PDF).
        /// </summary>
        public static void Exportar(
            List<Audiencia> datos,
            string nombreArchivo)
        {
            if (datos == null || datos.Count == 0)
            {
                MessageBox.Show(
                    "No hay datos para exportar.",
                    "Info",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Guardar reporte PDF",
                Filter = "Archivo HTML (*.html)|*.html",
                FileName = nombreArchivo
            };

            if (dlg.ShowDialog() != true)
                return;

            try
            {
                var html = GenerarHtml(datos);

                File.WriteAllText(
                    dlg.FileName,
                    html,
                    Encoding.UTF8);

                System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = dlg.FileName,
                        UseShellExecute = true
                    });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al exportar PDF:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // ─────────────────────────────────────────────────────────────
        private static string GenerarHtml(List<Audiencia> datos)
        {
            var sb = new StringBuilder();

            sb.Append(@"<!DOCTYPE html>
<html lang='es'>
<head>
<meta charset='UTF-8'/>
<title>Reporte de Audiencias</title>
<style>
  * { box-sizing: border-box; margin: 0; padding: 0; }
  body {
    font-family: Arial, sans-serif;
    font-size: 10px;
    color: #111827;
    padding: 20px;
    background: #fff;
  }
  .header {
    display: flex;
    justify-content: space-between;
    align-items: flex-end;
    margin-bottom: 16px;
    border-bottom: 2px solid #1F7A5C;
    padding-bottom: 10px;
  }
  .header h1 { font-size: 18px; color: #111827; }
  .header p  { font-size: 10px; color: #6B7280; margin-top: 3px; }
  .meta      { font-size: 10px; color: #6B7280; text-align: right; }
  .resumen {
    display: flex;
    gap: 24px;
    margin-bottom: 14px;
  }
  .chip {
    background: #F3F4F6;
    border-radius: 8px;
    padding: 6px 14px;
    font-size: 10px;
    color: #374151;
  }
  .chip strong { font-size: 14px; color: #1F7A5C; display: block; }
  table {
    border-collapse: collapse;
    width: 100%;
  }
  th {
    background: #1F7A5C;
    color: #fff;
    font-weight: 600;
    font-size: 9px;
    padding: 6px 5px;
    text-align: left;
    border: 1px solid #17634A;
    white-space: nowrap;
  }
  td {
    border: 1px solid #E5E7EB;
    padding: 4px 5px;
    font-size: 9px;
    vertical-align: top;
  }
  tr:nth-child(even) td { background: #F9FAFB; }
  tr:hover td { background: #ECFDF5; }
  .total-row td {
    background: #F0FDF4 !important;
    font-weight: 700;
    border-top: 2px solid #1F7A5C;
  }
  @media print {
    @page { size: landscape; margin: 0.8cm; }
    body  { padding: 0; }
    tr    { page-break-inside: avoid; }
  }
</style>
</head>
<body>
");
            // Encabezado
            sb.Append($@"
<div class='header'>
  <div>
    <h1>Reporte de Audiencias</h1>
    <p>Sistema Poder Judicial — Registros filtrados</p>
  </div>
  <div class='meta'>
    Generado: {DateTime.Now:dd/MM/yyyy HH:mm}<br/>
    Total registros: <strong>{datos.Count}</strong>
  </div>
</div>");

            // Chips resumen
            int totalDiscos = 0;
            foreach (var a in datos) totalDiscos += a.TotDiscos ?? 0;

            sb.Append($@"
<div class='resumen'>
  <div class='chip'><strong>{datos.Count}</strong>Registros encontrados</div>
  <div class='chip'><strong>{totalDiscos}</strong>Total de discos</div>
</div>");

            // Tabla
            bool esExp = datos.Count > 0 && datos.All(a =>
                string.Equals(a.TipoCausa, "EXP", StringComparison.OrdinalIgnoreCase));

            var cols = new List<string> {
                "Fecha Audiencia","Tot. Discos", "Juzgado", "Juez", "No. Causa", "NUC",
       "Tipo Causa","Tipo Audiencia","Hora Conclusión","Imputado", "Delito", "Agraviado",
       "Sala","No. Causa Juicio"

            };

            if (esExp)
                cols.Add("Expediente");

            sb.Append("<table><thead><tr>");
            foreach (var col in cols)
                sb.Append($"<th>{Encode(col)}</th>");
            sb.Append("</tr></thead><tbody>");

            foreach (var a in datos)
            {
                sb.Append("<tr>");
                var celdas = new List<string?>
                {
                    a.FechaAudiencia?.ToString("dd/MM/yyyy"),
                    (a.TotDiscos ?? 0).ToString(),
                    a.Juzgado, a.Juez, a.NoCausa, a.NUC,
                    a.TipoCausa, a.TipoAudiencia,
                    a.HoraConclusion?.ToString("HH:mm"),
                    a.Imputado, a.Delito,  a.Agraviado, a.Sala,
                     a.NoCausaJuicio
                };

                if (esExp)
                    celdas.Add(a.Expediente);

                foreach (var cel in celdas)
                    sb.Append($"<td>{Encode(cel ?? "")}</td>");
                sb.Append("</tr>");
            }

            // Fila de totales
            if (esExp)
            {
                sb.Append($@"
<tr class='total-row'>
  <td colspan='14' style='text-align:right;padding-right:8px;'>TOTAL DISCOS:</td>
  <td>{totalDiscos}</td>
</tr>");
            }
            else
            {
                sb.Append($@"
<tr class='total-row'>
  <td colspan='14' style='text-align:right;padding-right:8px;'>TOTAL DISCOS:</td>
  <td>{totalDiscos}</td>
  <td colspan='{cols.Count - 15}'></td>
</tr>");
            }

            sb.Append("</tbody></table></body></html>");
            return sb.ToString();
        }

        // Escapa caracteres HTML sin depender de System.Web
        private static string Encode(string text)
        {
            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;");
        }
    }
}
