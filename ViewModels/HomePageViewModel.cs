using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PoderJudicial.Models;
using PoderJudicial.Data;

namespace PoderJudicial.ViewModels
{

    public class HomePageViewModel : BaseViewModel
    {
        private int _totalAudienciasMes;
        public int TotalAudienciasMes
        {
            get => _totalAudienciasMes;
            set
            {
                _totalAudienciasMes = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<ActividadReciente> _actividades;

        public ObservableCollection<ActividadReciente> Actividades
        {
            get => _actividades;
            set
            {
                _actividades = value;
                OnPropertyChanged();
            }
        }



        private int _totalEjecucionesMes;
        public int TotalEjecucionesMes
        {
            get => _totalEjecucionesMes;
            set
            {
                _totalEjecucionesMes = value;
                OnPropertyChanged();
            }
        }


        private int _totalCopiasMes;
        public int TotalCopiasMes
        {
            get => _totalCopiasMes;
            set
            {
                _totalCopiasMes = value;
                OnPropertyChanged();
            }
        }


        private int _audienciasHoy;
        public int AudienciasHoy
        {
            get => _audienciasHoy;
            set
            {
                _audienciasHoy = value;
                OnPropertyChanged();
            }
        }


        private string _versionSistema;
        public string VersionSistema
        {
            get => _versionSistema;
            set
            {
                _versionSistema = value;
                OnPropertyChanged();
            }
        }

        private string _nombreBaseDatos;
        public string NombreBaseDatos
        {
            get => _nombreBaseDatos;
            set
            {
                _nombreBaseDatos = value;
                OnPropertyChanged();
            }
        }

        private string _ultimaCopiaSeguridad;
        public string UltimaCopiaSeguridad
        {
            get => _ultimaCopiaSeguridad;
            set
            {
                _ultimaCopiaSeguridad = value;
                OnPropertyChanged();
            }
        }

        private string _estadoSistema;
        public string EstadoSistema
        {
            get => _estadoSistema;
            set
            {
                _estadoSistema = value;
                OnPropertyChanged();
            }
        }


    }


}
