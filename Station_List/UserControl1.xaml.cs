using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.ComponentModel.Composition;
using api = API.API;
namespace Station_List
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    [Export(typeof(API.API.IView))]
    public partial class UserControl1 : UserControl , API.API.IView
    {
        private System.Collections.ObjectModel.ObservableCollection<api.Config> _config = new System.Collections.ObjectModel.ObservableCollection<api.Config>();
        private System.Collections.ObjectModel.ObservableCollection<api.Station> _stations;
        private String _viewName = "Station List";
        public UserControl1()
        {
            intilizeSettings();
            InitializeComponent();
        }
        //public static readonly DependencyProperty ConfigurationProperty = DependencyProperty.Register("Configuration", typeof(System.Collections.ObjectModel.ObservableCollection<api.Config>), typeof(UserControl1));

        public System.Collections.ObjectModel.ObservableCollection<API.API.Config> Configuration
        {
            get
            {
                return _config;
            }
            set
            {
                _config = value;
            }
        }

        public void Intilize(System.Collections.ObjectModel.ObservableCollection<API.API.Station> Stations, ref API.API.IViewHost StationManager, ref API.API.ConfigManager.GlobalConfig GlobalSettings)
        {
            _stations = Stations;
            this.DataContext = _stations;
        }
        
        public void intilizeSettings()
        {
            //_config = new System.Collections.ObjectModel.ObservableCollection<api.Config>();
            _config.Add(new api.Config("Displayed Columns"));
            _config[0].ConfigSettings.Add(new api.Setting("Station Name", "Yes"));
            _config[0].ConfigSettings.Add(new api.Setting("Temp-Out-Cur", "Yes"));
            _config[0].ConfigSettings.Add(new api.Setting("Temp-Out-High", "Yes"));
            _config[0].ConfigSettings.Add(new api.Setting("Temp-Out-Low", "Yes"));
            _config[0].ConfigSettings.Add(new api.Setting("Temp-Apparent", "Yes"));

            _config[0].ConfigSettings.Add(new api.Setting("Temp-In-Cur", "Yes"));
            _config[0].ConfigSettings.Add(new api.Setting("Temp-In-High", "Yes"));
            _config[0].ConfigSettings.Add(new api.Setting("Temp-In-Low", "Yes"));

            _config[0].ConfigSettings.Add(new api.Setting("Heat Index", "Yes"));
            _config[0].ConfigSettings.Add(new api.Setting("Wind Chill", "Yes"));
            _config[0].ConfigSettings.Add(new api.Setting("Solar Rad", "Yes"));
            _config[0].ConfigSettings.Add(new api.Setting("UV Index", "Yes"));

            _config[0].ConfigSettings.Add(new api.Setting("Wind-Speed", "Yes"));
            _config[0].ConfigSettings.Add(new api.Setting("Wind-Direction", "Yes"));
            _config[0].ConfigSettings.Add(new api.Setting("Wind-Bearing", "Yes"));
            _config[0].ConfigSettings.Add(new api.Setting("Wind-Peak Gust", "Yes"));
            _config[0].ConfigSettings.Add(new api.Setting("5 Min Wind Peak", "Yes"));
            _config[0].ConfigSettings.Add(new api.Setting("1 Min Wind Avg.", "Yes"));

            _config[0].ConfigSettings.Add(new api.Setting("Barometer-Cur", "Yes"));
            _config[0].ConfigSettings.Add(new api.Setting("Barometer-3hr", "Yes"));
            _config[0].ConfigSettings.Add(new api.Setting("Barometer-High", "Yes"));
            _config[0].ConfigSettings.Add(new api.Setting("Barometer-Low", "Yes"));
            _config[0].ConfigSettings.Add(new api.Setting("Active Weather", "Yes"));
            _config[0].ConfigSettings.Add(new api.Setting("Sky Conditions", "Yes"));

            _config[0].ConfigSettings.Add(new api.Setting("Rain-Rate-1Hr", "Yes"));
            _config[0].ConfigSettings.Add(new api.Setting("Rain-Year", "Yes"));
            _config[0].ConfigSettings.Add(new api.Setting("Rain-Today", "Yes"));

            _config[0].ConfigSettings.Add(new api.Setting("Humidity-Out", "Yes"));
            _config[0].ConfigSettings.Add(new api.Setting("Humidity-Out-High", "Yes"));
            _config[0].ConfigSettings.Add(new api.Setting("Humidity-Out-Low", "Yes"));
            _config[0].ConfigSettings.Add(new api.Setting("Humidity-In", "Yes"));

            //_config[0].ConfigSettings.Add(new api.Setting("Humidity-In-High", "Yes"));
            //_config[0].ConfigSettings.Add(new api.Setting("Humidity-In-Low", "Yes"));

            _config[0].ConfigSettings.Add(new api.Setting("Dew Point", "Yes"));
            _config[0].ConfigSettings.Add(new api.Setting("Cloud Base", "Yes"));
            _config[0].ConfigSettings.Add(new api.Setting("Density Altitude-NOAA", "Yes"));

            for (int i = 0, n = _config[0].ConfigSettings.Count; i < n; i++)
            {
                _config[0].ConfigSettings[i].AllowedParameters = new List<string> { "Yes", "No" };
            }
        }
        public void SelectedStationChanged(int SelectedStaitonIndex)
        {
            
        }

        public string ViewName
        {
            get
            {
                return _viewName;
            }
            set
            {
                _viewName = value;
            }
        }
    }

   
    [ValueConversion(typeof(String), typeof(Boolean))]
    public class yesnoToBoolConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if ((String)value == "Yes")
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if ((Boolean)value == true)
            {
                return "Yes";
            }
            else
            {
                return "No";
            }
        }
    }

    /// <summary>
    /// This converter targets a column header,
    /// in order to take its width to zero when
    /// it should be hidden
    /// </summary>
    public class ColumnWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,object parameter, System.Globalization.CultureInfo culture)
        {
            String val = (String)value;
            Boolean isVisible = false;
            if (val == "Yes")
            {
                isVisible = true;
            }
            else
            {
                isVisible = false;
            }

            if (isVisible == true)
            {
                return parameter;
            }
            else
            {
                return 0.0;
            }
        }


        public object ConvertBack(object value,Type targetType,object parameter,System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }


}
