using PoderJudicial.Helpers;
using PoderJudicial.Models;
using System;
using System.Windows;
using System.Windows.Controls;

namespace PoderJudicial.Views
{
    public partial class AudienciaFormControl : UserControl
    {
        // El clic solo avisa al host (NuevoRegistro); es el host quien
        // valida y guarda TODOS los formularios de la cadena de concentradas
        // (incluido este) en una sola operación. Así el comportamiento es
        // idéntico tanto si hay 1 formulario como si hay 7.
        // (el manejador real vive en AudienciaFormControl.xaml.cs)

        // ── Tipo de causa actual (expuesto para el host) ──
        public string TipoCausaActual => UIHelper.ObtenerValorCombo(CmbTipoCausa);

        // ── Validación (expuesta para el host) ─────────────
        // Devuelve false y el mensaje del primer problema encontrado,
        // sin mostrar ningún MessageBox — el host decide cómo presentarlo
        // (agregando "Formulario N:" cuando hay varias audiencias concentradas).
        public bool Validar(out string mensajeError)
        {
            mensajeError = null;
            string tipoCausa = TipoCausaActual;

            if (ValidationHelper.CampoVacio(TxtId))
                return Falla(out mensajeError, "El campo 'Id' es obligatorio.");

            // ── No. Causa / Causa ──────────────────────────
            // Para EXP el mismo control representa "Causa" y puede
            // quedar vacío, previa confirmación del usuario.
            string noCausa = UIHelper.ObtenerTexto(TxtNoCausa);
            bool causaOpcionalVacia = tipoCausa == "EXP" && string.IsNullOrWhiteSpace(noCausa);

            if (causaOpcionalVacia)
            {
                if (!ValidationHelper.ConfirmarCampoVacio("Causa"))
                    return Falla(out mensajeError, "Guardado cancelado: el campo 'Causa' quedó vacío.");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(noCausa))
                    return Falla(out mensajeError, "El campo 'No. Causa' es obligatorio.");

                if (!ValidationHelper.NumerosYDiagonalConExcepcion(noCausa, _permitirLetrasNoCausa))
                    return Falla(out mensajeError, "El campo 'No. Causa' solo permite números y '/'.");
            }

            // ── NUC (no aplica a EXP) ──────────────────────
            if (tipoCausa != "EXP")
            {
                string nuc = UIHelper.ObtenerTexto(TxtNUC);

                if (string.IsNullOrWhiteSpace(nuc))
                    return Falla(out mensajeError, "El campo NUC es obligatorio.");

                if (!ValidationHelper.NumerosYGuionConExcepcion(nuc, _permitirLetrasNUC))
                    return Falla(out mensajeError, "El campo 'NUC' solo permite números y '-'.");
            }

            // ── Expediente (solo EXP) ───────────────────────
            if (tipoCausa == "EXP")
            {
                string expediente = UIHelper.ObtenerTexto(TxtExpediente);

                if (string.IsNullOrWhiteSpace(expediente))
                    return Falla(out mensajeError, "El campo 'Expediente' es obligatorio.");

                if (!ValidationHelper.NumerosYDiagonalConExcepcion(expediente, _permitirLetrasExpediente))
                    return Falla(out mensajeError, "El campo 'Expediente' solo permite números y '/'.");
            }

            // ── Juzgado (no aplica a EXP: Ejecución no guarda juzgado) ─
            if (tipoCausa != "EXP")
            {
                var juzgadoItem = CmbJuzgado.SelectedItem as ComboBoxItem;
                if (juzgadoItem == null || juzgadoItem.Content.ToString() == "Seleccione juzgado")
                    return Falla(out mensajeError, "El campo 'Juzgado' es obligatorio.");
            }

            // ── Sala ────────────────────────────────────────
            var salaItem = CmbSala.SelectedItem as ComboBoxItem;
            if (salaItem == null || salaItem.Content.ToString() == "Seleccione sala")
                return Falla(out mensajeError, "El campo 'Sala' es obligatorio.");

            if (!ValidationHelper.FechaValida(UIHelper.ObtenerTexto(TxtFechaAudiencia)))
                return Falla(out mensajeError, "La fecha de audiencia no es válida.");

            if (!ValidationHelper.HoraValida(UIHelper.ObtenerTexto(TxtHoraAudiencia)))
                return Falla(out mensajeError, "La hora de audiencia no es válida.");

            if (!ValidationHelper.HoraValida(UIHelper.ObtenerTexto(TxtHoraConclusion)))
                return Falla(out mensajeError, "La hora de conclusión no es válida.");

            string juez = UIHelper.ObtenerTextosPanelDinamico(PanelJuecesExtra, TxtJuez);
            if (string.IsNullOrWhiteSpace(juez))
                return Falla(out mensajeError, "Debe capturar al menos un juez.");

            string audiencia = UIHelper.ObtenerTextosPanelDinamico(PanelAudienciaExtra, TxtTipoAudiencia);
            if (string.IsNullOrWhiteSpace(audiencia))
                return Falla(out mensajeError, "El tipo de audiencia es obligatorio.");

            if (tipoCausa != "CP")
            {
                if (string.IsNullOrWhiteSpace(UIHelper.ObtenerImputados(PanelImputadosExtra, TxtImputado)))
                    return Falla(out mensajeError, "El imputado es obligatorio.");

                string delito = UIHelper.ObtenerTextosPanelDinamico(PanelDelitoExtra, TxtDelito);
                if (string.IsNullOrWhiteSpace(delito))
                    return Falla(out mensajeError, "El delito es obligatorio.");

                if (string.IsNullOrWhiteSpace(UIHelper.ObtenerTexto(TxtAgraviado)))
                    return Falla(out mensajeError, "El agraviado es obligatorio.");
            }

            if (tipoCausa == "JO")
            {
                string noCausaJuicio = UIHelper.ObtenerTexto(TxtNoCausaJuicio);
                if (string.IsNullOrWhiteSpace(noCausaJuicio))
                    return Falla(out mensajeError, "El No. Causa Juicio es obligatorio para JO.");

                if (!ValidationHelper.NumerosYDiagonal(noCausaJuicio))
                    return Falla(out mensajeError, "No. Causa Juicio solo permite números y '/'.");
            }

            return true;
        }

        private static bool Falla(out string mensajeError, string mensaje)
        {
            mensajeError = mensaje;
            return false;
        }

        // ── Construcción del modelo (expuesta para el host) ─
        // esConcentrada: true cuando este formulario forma parte de una
        // cadena de audiencias concentradas (2 o más formularios en la
        // misma operación de guardado); afecta únicamente el texto que se
        // guarda en "Quien Realiza"/"Observaciones" (ver ModalidadAudienciaHelper).
        public object ConstruirModelo(int id, bool esConcentrada)
        {
            // En modo edición el Id ya existe (el del registro original) y
            // nunca se marca como "concentrada": es una edición puntual, no
            // parte de una captura en lote.
            if (_esEdicion)
            {
                id = _idEdicion;
                esConcentrada = false;
            }

            string tipoCausa = TipoCausaActual;

            if (tipoCausa == "EXP")
            {
                return VM.ConstruirEjecucion(
                    id: id,
                    fechaAudiencia: ParsearFechaHora(TxtFechaAudiencia, TxtHoraAudiencia),
                    totalDiscos: UIHelper.ObtenerValorComboOtro(CmbTotDiscoAudiencia, TxtTotDiscoAudienciaOtro),
                    juez: UIHelper.ObtenerTextosPanelDinamico(PanelJuecesExtra, TxtJuez),
                    expedienteNumero: UIHelper.ObtenerTexto(TxtExpediente),
                    causa: UIHelper.ObtenerTexto(TxtNoCausa),
                    tipoAudiencia: UIHelper.ObtenerTextosPanelDinamico(PanelAudienciaExtra, TxtTipoAudiencia),
                    horaTermino: UIHelper.ObtenerTexto(TxtHoraConclusion),
                    imputado: UIHelper.ObtenerImputados(PanelImputadosExtra, TxtImputado),
                    delito: UIHelper.ObtenerTextosPanelDinamico(PanelDelitoExtra, TxtDelito),
                    victima: UIHelper.ObtenerTexto(TxtAgraviado),
                    sala: UIHelper.ObtenerValorCombo(CmbSala),
                    esVideoconferencia: EsVideoconferencia(),
                    esConcentrada: esConcentrada
                );
            }

            string tipoDiscoTexto = UIHelper.ObtenerValorCombo(CmbTipoDisco);
            int? totalDiscosAudiencia = int.TryParse(tipoDiscoTexto.Split(' ')[0], out int d) ? d : null;

            return VM.ConstruirAudiencia(
                id: id,
                noCausa: UIHelper.ObtenerTexto(TxtNoCausa),
                nuc: UIHelper.ObtenerTexto(TxtNUC),
                fechaAudiencia: ParsearFechaHora(TxtFechaAudiencia, TxtHoraAudiencia),
                fechaRecibo: ParsearFechaHora(TxtFechaRecibo, TxtHoraRecibo, esRecibo: true),
                tipoAudiencia: UIHelper.ObtenerTextosPanelDinamico(PanelAudienciaExtra, TxtTipoAudiencia),
                tipoCausa: tipoCausa,
                juzgado: UIHelper.ObtenerValorComboOtro(CmbJuzgado, TxtJuzgadoOtro),
                juez: UIHelper.ObtenerTextosPanelDinamico(PanelJuecesExtra, TxtJuez),
                imputado: UIHelper.ObtenerImputados(PanelImputadosExtra, TxtImputado),
                delito: UIHelper.ObtenerTextosPanelDinamico(PanelDelitoExtra, TxtDelito),
                agraviado: UIHelper.ObtenerTexto(TxtAgraviado),
                sala: UIHelper.ObtenerValorCombo(CmbSala),
                horaConclusion: ParsearHora(TxtHoraConclusion),
                noCausaJuicio: UIHelper.ObtenerTexto(TxtNoCausaJuicio),
                totDiscos: totalDiscosAudiencia,
                totDiscoAudiencia: UIHelper.ObtenerValorComboOtro(CmbTotDiscoAudiencia, TxtTotDiscoAudienciaOtro),
                esVideoconferencia: EsVideoconferencia(),
                esConcentrada: esConcentrada
            );
        }

        // ── Persistencia de un modelo ya construido ────────
        public void PersistirModelo(object modelo)
        {
            if (modelo is Ejecucion ejecucion)
            {
                if (_esEdicion) VM.ActualizarEjecucion(ejecucion);
                else VM.GuardarEjecucion(ejecucion);
            }
            else if (modelo is Audiencia audiencia)
            {
                if (_esEdicion) VM.ActualizarAudiencia(audiencia);
                else VM.GuardarAudiencia(audiencia);
            }
        }

        // ── Modalidad de audiencia (presencial / videoconferencia) ─
        private bool EsVideoconferencia()
            => UIHelper.ObtenerValorCombo(CmbVideoconferencia) == "Sí";

        // ── Parsers de fecha/hora ─────────────────────────
        private DateTime? ParsearFechaHora(TextBox txtFecha, TextBox txtHora, bool esRecibo = false)
        {
            string texto = esRecibo
                ? $"{txtFecha.Text} {txtHora.Text}"
                : $"{UIHelper.ObtenerTexto(txtFecha)} {UIHelper.ObtenerTexto(txtHora)}";
            return DateTime.TryParse(texto, out DateTime dt) ? dt : null;
        }

        private DateTime? ParsearHora(TextBox txtHora)
            => DateTime.TryParse(UIHelper.ObtenerTexto(txtHora), out DateTime h) ? h : null;

        // ── Limpiar formulario (expuesto para el host tras guardar) ─
        public void LimpiarFormulario()
        {
            TxtId.Text = TxtNoCausa.Text = TxtNUC.Text = TxtFechaAudiencia.Text =
            TxtTipoAudiencia.Text = TxtImputado.Text = TxtDelito.Text =
            TxtAgraviado.Text = TxtHoraConclusion.Text = TxtNoCausaJuicio.Text =
            TxtJuez.Text = TxtHoraAudiencia.Text = TxtExpediente.Text =
            TxtObservaciones.Text = string.Empty;

            CmbJuzgado.SelectedIndex = 1;
            CmbTotDiscoAudiencia.SelectedIndex = 1;
            CmbSala.SelectedIndex = 1;
            CmbTipoCausa.SelectedIndex = 1;
            CmbTipoDisco.SelectedIndex = 0;
            CmbVideoconferencia.SelectedIndex = 0;

            _permitirLetrasNoCausa = false;
            _permitirLetrasNUC = false;
            _permitirLetrasExpediente = false;

            PanelJuecesExtra.Children.Clear();
            PanelDelitoExtra.Children.Clear();
            PanelAudienciaExtra.Children.Clear();
            PanelImputadosExtra.Children.Clear();

            AplicarPlaceholders();
            _ = CargarIdVisualAsync();
        }
    }
}
