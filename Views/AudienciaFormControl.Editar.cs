using PoderJudicial.Helpers;
using PoderJudicial.Models;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PoderJudicial.Views
{
    /// <summary>
    /// Modo edición: precarga un registro existente (Audiencia o Ejecución)
    /// reutilizando exactamente los mismos controles, validaciones y lógica
    /// de visibilidad por tipo de causa que ya usa "Nuevo Registro" — la
    /// única diferencia es que el formulario nace con datos y actualiza en
    /// vez de insertar (ver AudienciaFormControl.Guardar.cs).
    /// </summary>
    public partial class AudienciaFormControl : UserControl
    {
        private bool _esEdicion = false;
        private int _idEdicion;

        public bool EsEdicion => _esEdicion;

        // ── Audiencias (C, CP, JO) ─────────────────────────
        public void CargarParaEditar(Audiencia audiencia)
        {
            _esEdicion = true;
            _idEdicion = audiencia.Id;

            TxtId.Text = audiencia.Id.ToString();

            // Selecciona el tipo de causa: dispara CmbTipoCausa_SelectionChanged,
            // que ya sabe mostrar/ocultar exactamente los campos que
            // corresponden a C, CP o JO (misma lógica que Nuevo Registro).
            SeleccionarComboPorTexto(CmbTipoCausa, audiencia.TipoCausa);

            EstablecerTexto(TxtNoCausa, audiencia.NoCausa);
            EstablecerTexto(TxtNUC, audiencia.NUC);

            if (audiencia.FechaAudiencia.HasValue)
            {
                EstablecerTexto(TxtFechaAudiencia, audiencia.FechaAudiencia.Value.ToString("dd/MM/yyyy"));
                EstablecerTexto(TxtHoraAudiencia, audiencia.FechaAudiencia.Value.ToString("HH:mm"));
            }

            if (audiencia.FechaRecibo.HasValue)
            {
                TxtFechaRecibo.Text = audiencia.FechaRecibo.Value.ToString("dd/MM/yyyy");
                TxtHoraRecibo.Text = audiencia.FechaRecibo.Value.ToString("hh:mm tt");
            }

            SeleccionarComboConOtro(CmbJuzgado, TxtJuzgadoOtro, audiencia.Juzgado, "Otra...");
            CargarValoresMultiples(audiencia.Juez, TxtJuez, PanelJuecesExtra, AgregarCampoJuez);
            CargarValoresMultiples(audiencia.TipoAudiencia, TxtTipoAudiencia, PanelAudienciaExtra,
                () => CrearCampoDinamico(PanelAudienciaExtra, "Tipo de Audiencia", TxtTipoAudiencia_TextChanged));
            CargarValoresMultiples(audiencia.Delito, TxtDelito, PanelDelitoExtra,
                () => CrearCampoDinamico(PanelDelitoExtra, "Tipo de delito", TxtDelito_TextChanged));

            CargarImputados(audiencia.Imputado);
            EstablecerTexto(TxtAgraviado, audiencia.Agraviado);
            SeleccionarComboPorTexto(CmbSala, audiencia.Sala);
            EstablecerTexto(TxtNoCausaJuicio, audiencia.NoCausaJuicio);

            if (audiencia.HoraConclusion.HasValue)
                EstablecerTexto(TxtHoraConclusion, audiencia.HoraConclusion.Value.ToString("hh:mm tt"));

            if (audiencia.TotDiscos.HasValue)
                SeleccionarComboPorTexto(CmbTipoDisco,
                    $"{audiencia.TotDiscos} Archivo{(audiencia.TotDiscos == 1 ? "" : "s")}");

            SeleccionarComboConOtro(CmbTotDiscoAudiencia, TxtTotDiscoAudienciaOtro, audiencia.TotDiscoAudiencia, "Otro...");

            CmbVideoconferencia.SelectedIndex =
                ModalidadAudienciaHelper.EsVideoconferencia(audiencia.QuienRealiza) ? 1 : 0;

            ActivarModoEdicion();
        }

        // ── Ejecución (EXP) ────────────────────────────────
        // Ejecución no tiene formulario propio: reutiliza este mismo control
        // configurado como si el usuario hubiera elegido "EXP" a mano.
        public void CargarParaEditar(Ejecucion ejecucion)
        {
            _esEdicion = true;
            _idEdicion = ejecucion.Id;

            TxtId.Text = ejecucion.Id.ToString();

            // Fuerza tipo de causa EXP: dispara la misma visibilidad que si
            // el usuario lo hubiera seleccionado manualmente en Nuevo Registro
            // (oculta NUC, No. Causa Juicio y muestra el panel de Expediente).
            SeleccionarComboPorTexto(CmbTipoCausa, "EXP");

            EstablecerTexto(TxtNoCausa, ejecucion.Causa);           // "Causa" comparte control con "No. Causa"
            EstablecerTexto(TxtExpediente, ejecucion.ExpedienteNumero);

            if (ejecucion.FechaAudiencia.HasValue)
            {
                EstablecerTexto(TxtFechaAudiencia, ejecucion.FechaAudiencia.Value.ToString("dd/MM/yyyy"));
                EstablecerTexto(TxtHoraAudiencia, ejecucion.FechaAudiencia.Value.ToString("HH:mm"));
            }

            CargarValoresMultiples(ejecucion.Juez, TxtJuez, PanelJuecesExtra, AgregarCampoJuez);
            CargarValoresMultiples(ejecucion.TipoAudiencia, TxtTipoAudiencia, PanelAudienciaExtra,
                () => CrearCampoDinamico(PanelAudienciaExtra, "Tipo de Audiencia", TxtTipoAudiencia_TextChanged));
            CargarValoresMultiples(ejecucion.Delito, TxtDelito, PanelDelitoExtra,
                () => CrearCampoDinamico(PanelDelitoExtra, "Tipo de delito", TxtDelito_TextChanged));

            EstablecerTexto(TxtHoraConclusion, ejecucion.HoraTermino);
            CargarImputados(ejecucion.Imputado);
            EstablecerTexto(TxtAgraviado, ejecucion.Victima);       // "Víctima" comparte control con "Agraviado"
            SeleccionarComboPorTexto(CmbSala, ejecucion.Sala);

            SeleccionarComboConOtro(CmbTotDiscoAudiencia, TxtTotDiscoAudienciaOtro, ejecucion.TotalDiscos, "Otro...");

            CmbVideoconferencia.SelectedIndex =
                ModalidadAudienciaHelper.EsVideoconferencia(ejecucion.Observaciones) ? 1 : 0;

            ActivarModoEdicion();
        }

        private void ActivarModoEdicion()
        {
            MostrarBotonConcentrada(false);   // editar es siempre un registro puntual
            BtnGuardar.Content = "Guardar Cambios";
        }

        // ── Helpers de carga ───────────────────────────────
        private static void EstablecerTexto(TextBox txt, string valor)
        {
            txt.Text = valor ?? string.Empty;
            txt.Foreground = (Brush)Application.Current.Resources["InputTextBrush"];
        }

        private static void SeleccionarComboPorTexto(ComboBox combo, string valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return;

            foreach (ComboBoxItem item in combo.Items)
            {
                if (string.Equals(item.Content?.ToString(), valor, StringComparison.OrdinalIgnoreCase))
                {
                    combo.SelectedItem = item;
                    return;
                }
            }
        }

        private static void SeleccionarComboConOtro(ComboBox combo, TextBox txtOtro, string valor, string etiquetaOtro)
        {
            if (string.IsNullOrWhiteSpace(valor)) return;

            foreach (ComboBoxItem item in combo.Items)
            {
                if (string.Equals(item.Content?.ToString(), valor, StringComparison.OrdinalIgnoreCase))
                {
                    combo.SelectedItem = item;
                    return;
                }
            }

            // No coincide con ninguna opción fija: usar "Otro.../Otra..." + texto libre,
            // igual que hace el usuario a mano en Nuevo Registro.
            foreach (ComboBoxItem item in combo.Items)
            {
                if (item.Content?.ToString() == etiquetaOtro)
                {
                    combo.SelectedItem = item;
                    break;
                }
            }

            txtOtro.Visibility = Visibility.Visible;
            EstablecerTexto(txtOtro, valor);
        }

        /// <summary>
        /// Reparte un valor combinado (guardado con " / ", mismo separador que
        /// usa UIHelper.ObtenerTextosPanelDinamico al guardar) entre el campo
        /// principal y los campos dinámicos extra (Juez/Delito/Tipo Audiencia).
        /// </summary>
        private void CargarValoresMultiples(string valorCombinado, TextBox campoPrincipal,
            StackPanel panelExtra, Action agregarCampoVacio)
        {
            panelExtra.Children.Clear();

            if (string.IsNullOrWhiteSpace(valorCombinado)) return;

            string[] valores = valorCombinado.Split(
                new[] { " / " }, StringSplitOptions.RemoveEmptyEntries);

            if (valores.Length == 0) return;

            EstablecerTexto(campoPrincipal, valores[0].Trim());

            for (int i = 1; i < valores.Length; i++)
            {
                agregarCampoVacio();

                // El campo recién agregado es el último TextBox del último Grid del panel.
                var ultimoGrid = panelExtra.Children[panelExtra.Children.Count - 1] as Grid;
                var stack = ultimoGrid?.Children[0] as StackPanel;
                var txt = stack?.Children[0] as TextBox;

                if (txt != null) EstablecerTexto(txt, valores[i].Trim());
            }
        }

        private void CargarImputados(string valorCombinado)
        {
            PanelImputadosExtra.Children.Clear();
            List<string> valores = UIHelper.SepararImputados(valorCombinado);
            if (valores.Count == 0) return;

            EstablecerTexto(TxtImputado, valores[0]);
            for (int i = 1; i < valores.Count; i++)
            {
                CrearCampoDinamico(PanelImputadosExtra, "Nombre del imputado", null);
                var grid = PanelImputadosExtra.Children[PanelImputadosExtra.Children.Count - 1] as Grid;
                var stack = grid?.Children[0] as StackPanel;
                var txt = stack?.Children[0] as TextBox;
                if (txt != null) EstablecerTexto(txt, valores[i]);
            }
        }
    }
}
