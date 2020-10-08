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
using System.ComponentModel.Composition;
using api = API.API;
using System.Xml;
namespace Wunderground_Forecast
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    [Export(typeof(api.IView))]
    public partial class UserControl1 : UserControl , api.IView
    {
        private errorObject errorMsg = new errorObject();
        private string _name = "Wunderground Forecast";
        private System.Collections.ObjectModel.ObservableCollection<api.Config> _config;
        private api.ConfigManager.GlobalConfig _GBLSettings;

        private System.Collections.ObjectModel.ObservableCollection<api.Station> _stations;
        private int currentStationNum = 0;
        private System.Windows.Threading.DispatcherTimer _autoUpdateTimer = new System.Windows.Threading.DispatcherTimer(System.Windows.Threading.DispatcherPriority.Background);

        //public DependencyProperty ForecastsProperty = DependencyProperty.Register("Forecasts", typeof(ForecastObject), typeof(UserControl1), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender | FrameworkPropertyMetadataOptions.AffectsRender));
        private System.Collections.ObjectModel.ObservableCollection<ForecastObject> _forecasts;
        


        public UserControl1()
        {
            InitializeComponent();
            _autoUpdateTimer.Tick += _autoUpdateTimer_Elapsed;
            _forecasts = new System.Collections.ObjectModel.ObservableCollection<ForecastObject>();
            this.Mainlist.DataContext = _forecasts;
            this.Mainlist2.DataContext = _forecasts;
            errorMsg.ErrorType = "Data Has Not been Updated!";
            errorMsg.ErrorDescription = "Click Update Forecast to get the latest.";
            this.errorText.DataContext = errorMsg;
        }

        private void UpDateIntervalChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            checkUpdateinterval();
        }
        
        public System.Collections.ObjectModel.ObservableCollection<api.Config> Configuration
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

        public void Intilize(System.Collections.ObjectModel.ObservableCollection<api.Station> Stations, ref api.IViewHost StationManager, ref api.ConfigManager.GlobalConfig GlobalSettings)
        {
            _stations = Stations;
            intConfig();
            _GBLSettings = GlobalSettings;
            checkUpdateinterval();
            _config[0].ConfigSettings[3].PropertyChanged += UpDateIntervalChanged;
            _autoUpdateTimer.Start();
            //UpdateForecast();
        }
        private void checkUpdateinterval()
        {
            if (Convert.ToInt32(_config[0].ConfigSettings[3].CurrentValue) < 5)
            {
                _config[0].ConfigSettings[3].CurrentValue = "5";
            }
            _autoUpdateTimer.Interval = new TimeSpan(0, Convert.ToInt32(_config[0].ConfigSettings[3].CurrentValue), 0);
            UpdateForecast();
        }

        public void SelectedStationChanged(int SelectedStaitonIndex)
        {
            currentStationNum = SelectedStaitonIndex;
            //UpdateForecast();
        }

        public string ViewName
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }

        private void intConfig()
        {
            _config = new System.Collections.ObjectModel.ObservableCollection<api.Config>();
            _config.Add(new api.Config("General Settings"));
            _config[0].ConfigSettings.Add(new api.Setting("API Key", ""));
            _config[0].ConfigSettings.Add(new api.Setting("State", "VA"));
            _config[0].ConfigSettings.Add(new api.Setting("Location", "Callands"));
            _config[0].ConfigSettings.Add(new api.Setting("Auto Update Freqency (In Minutes)", "30"));
        }

        private void UpdateForecast()
        {
            List<ForecastObject> TempforecastList = new List<ForecastObject>();
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load("http://api.wunderground.com/api/" + _config[0].ConfigSettings[0].CurrentValue + "/forecast10day/q/" + _config[0].ConfigSettings[1].CurrentValue + "/" + _config[0].ConfigSettings[2].CurrentValue + ".xml");
            }
            catch (Exception)
            {
                return;
            }


            XmlElement root = doc.DocumentElement;
            XmlNodeList nodes = root.SelectNodes("/response/forecast/simpleforecast/forecastdays/forecastday");
            if (nodes.Count == 0)
            {
                errorMsg.IsError = true;
                errorMsg.ErrorType = root.SelectSingleNode("/response/error")["type"].InnerText;
                errorMsg.ErrorDescription = root.SelectSingleNode("/response/error")["description"].InnerText;
                return;
            }
            errorMsg.IsError = false;
            errorMsg.ErrorType = "";
            errorMsg.ErrorDescription = "";
            //_forecasts.Clear();
            int maxnumberofDays = 0;
            foreach (XmlNode node in nodes)
            {
                if (maxnumberofDays <= 8)
                {
                ForecastObject dayForecast = new ForecastObject();
                dayForecast.DayOfWeek = node.SelectSingleNode("date")["weekday"].InnerText;
                //XmlNode day = node.SelectSingleNode("/date/weekday_short");
                if (_GBLSettings.GlobalSettings[0].ConfigSettings[(int)api.ConfigManager.GlobalConfig.configIndexes.Temp].CurrentValue == api.DataUnitType.Temp_Fahrenheit.ToString())
                {
                    dayForecast.HighTemp = node.SelectSingleNode("high")["fahrenheit"].InnerText;
                    dayForecast.LowTemp = node.SelectSingleNode("low")["fahrenheit"].InnerText;
                }
                else
                {
                    dayForecast.HighTemp = node.SelectSingleNode("high")["celsius"].InnerText;
                    dayForecast.LowTemp = node.SelectSingleNode("low")["celsius"].InnerText;
                }

                if (_GBLSettings.GlobalSettings[0].ConfigSettings[(int)api.ConfigManager.GlobalConfig.configIndexes.Rain].CurrentValue == api.DataUnitType.Precip_Inch.ToString())
                {
                    dayForecast.Rain = node.SelectSingleNode("qpf_allday")["in"].InnerText;
                    dayForecast.Snow = node.SelectSingleNode("snow_allday")["in"].InnerText;
                }
                else
                {
                    dayForecast.Rain = node.SelectSingleNode("qpf_allday")["mm"].InnerText;
                    dayForecast.Snow = node.SelectSingleNode("snow_allday")["cm"].InnerText;
                }
                if (_GBLSettings.GlobalSettings[0].ConfigSettings[(int)api.ConfigManager.GlobalConfig.configIndexes.Wind].CurrentValue == api.DataUnitType.Wind_Mph.ToString())
                {
                    dayForecast.MaxWind = node.SelectSingleNode("maxwind")["mph"].InnerText;
                }
                else
                {
                    dayForecast.MaxWind = node.SelectSingleNode("maxwind")["kph"].InnerText;
                }
                dayForecast.MaxWindDir = node.SelectSingleNode("maxwind")["dir"].InnerText;
                dayForecast.ChanceOfPrecip = node["pop"].InnerText;
                dayForecast.IconURL = node["icon_url"].InnerText;
                    //if (_forecasts.Count < 9)
                    //{
                    //    _forecasts.Add(dayForecast);
                    //}
                    //else
                    //{
                    //    _forecasts[maxnumberofDays] = dayForecast;
                    //}
                TempforecastList.Add(dayForecast);
                    maxnumberofDays++;
                }
            }
            nodes = root.SelectNodes("/response/forecast/txt_forecast/forecastdays/forecastday");
            maxnumberofDays = 0;
            for (int i = 0; i <= 8; i++)
            {
                if (_GBLSettings.GlobalSettings[0].ConfigSettings[(int)api.ConfigManager.GlobalConfig.configIndexes.Temp].CurrentValue == api.DataUnitType.Temp_Fahrenheit.ToString())
                {
                    //_forecasts[i].DayText = nodes[maxnumberofDays]["fcttext"].InnerText;
                    //_forecasts[i].NightText = nodes[maxnumberofDays + 1]["fcttext"].InnerText;
                    TempforecastList[i].DayText = nodes[maxnumberofDays]["fcttext"].InnerText;
                    TempforecastList[i].NightText = nodes[maxnumberofDays + 1]["fcttext"].InnerText;
                }
                else
                {
                    //_forecasts[i].DayText = nodes[maxnumberofDays]["fcttext_metric"].InnerText;
                    //_forecasts[i].NightText = nodes[maxnumberofDays + 1]["fcttext_metric"].InnerText;
                    TempforecastList[i].DayText = nodes[maxnumberofDays]["fcttext_metric"].InnerText;
                    TempforecastList[i].NightText = nodes[maxnumberofDays + 1]["fcttext_metric"].InnerText;
                }
                    //_forecasts[i].DayIconURL = nodes[maxnumberofDays]["icon_url"].InnerText;
                    //_forecasts[i].NightIconURL = nodes[maxnumberofDays + 1]["icon_url"].InnerText;
                TempforecastList[i].DayIconURL = nodes[maxnumberofDays]["icon_url"].InnerText;
                TempforecastList[i].NightIconURL = nodes[maxnumberofDays + 1]["icon_url"].InnerText;
                    maxnumberofDays += 2;
            }
            //_forecasts.Clear();
            for (int i = 0, n = TempforecastList.Count; i < n; i++ )
            {
                if (_forecasts.Count < 9)
                {
                    _forecasts.Add(TempforecastList[i]);
                }
                else
                {
                    _forecasts[i] = TempforecastList[i];
                }
            }
            errorMsg.ErrorType = "Last Updated at: " + DateTime.Now.ToShortTimeString();
            errorMsg.ErrorDescription = "Next Update Scheduled for: " + DateTime.Now.AddMinutes(_autoUpdateTimer.Interval.Minutes).ToShortTimeString();
        }
        
        public class errorObject : System.ComponentModel.INotifyPropertyChanged
        {
            public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
            private void OnPropertyChanged(String info)
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(info));
                }
            }

            private Boolean _IsError = false;
            private String _errorType = "";
            private String _errordescription = "";

            public Boolean IsError
            {
                get { return _IsError; }
                set { _IsError = value; OnPropertyChanged("IsError"); }
            }
            public String ErrorType
            {
                get { return _errorType; }
                set { _errorType = value; OnPropertyChanged("ErrorType"); }
            }
            public String ErrorDescription
            {
                get { return _errordescription; }
                set { _errordescription = value; OnPropertyChanged("ErrorDescription"); }
            }
        }
        public class ForecastObject : System.ComponentModel.INotifyPropertyChanged
        {
            public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
            private void OnPropertyChanged(String info)
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(info));
                }
            }

            private String _chanceOfPrecip;
            private String _dayOfWeek;
            private String _highTemp;
            private String _lowtemp;
            private String _rain;
            private String _snow;
            private String _maxwind;
            private String _maxWindDir;
            private String _iconURL;
            private String _dayText;
            private String _dayIcon;
            private String _nightText;
            private String _nightIcon;

            public String DayOfWeek
            {
                get { return _dayOfWeek; }
                set { _dayOfWeek = value; OnPropertyChanged("DayOfWeek"); }
            }
            public String HighTemp 
            {
                get { return _highTemp; }
                set { _highTemp = value; OnPropertyChanged("HighTemp"); } 
            }
            public String LowTemp 
            {
                get { return _lowtemp; }
                set { _lowtemp = value; OnPropertyChanged("LowTemp"); }
            }
            public String Rain 
            {
                get { return _rain; }
                set { _rain = value; OnPropertyChanged("Rain"); }
            }
            public String Snow 
            {
                get { return _snow; }
                set { _snow = value; OnPropertyChanged("Snow"); } 
            }
            public String MaxWind 
            {
                get { return _maxwind; }
                set { _maxwind = value; OnPropertyChanged("MaxWind"); }
            }
            public String MaxWindDir 
            {
                get { return _maxWindDir; }
                set { _maxWindDir = value; OnPropertyChanged("MaxWindDir"); }
            }
            public String IconURL
            {
                get { return _iconURL; }
                set { _iconURL = value; OnPropertyChanged("IconURL"); }
            }
            public String ChanceOfPrecip
            {
                get { return _chanceOfPrecip; }
                set { _chanceOfPrecip = value; OnPropertyChanged("ChanceOfPrecip"); }
            }
            
            public String DayIconURL
            {
                get { return _dayIcon; }
                set { _dayIcon = value; OnPropertyChanged("DayIconURL"); }
            }
            public String NightIconURL
            {
                get { return _nightIcon; }
                set { _nightIcon = value; OnPropertyChanged("NightIconURL"); }
            }

            public String DayText
            {
                get { return _dayText; }
                set { _dayText = value; OnPropertyChanged("DayText"); }
            }
            public String NightText
            {
                get { return _nightText; }
                set { _nightText = value; OnPropertyChanged("NightText"); }
            }
        }

        private void openWundergroundPage()
        {
            //"http://www.wunderground.com/weather-forecast/VA/Callands.html?apiref=813555b9b890671e"
            System.Diagnostics.Process.Start("http://www.wunderground.com/weather-forecast/" + _config[0].ConfigSettings[1].CurrentValue + "/" + _config[0].ConfigSettings[2].CurrentValue.Replace(" ","_") + ".html?apiref=813555b9b890671e");
        }
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            openWundergroundPage();
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            openWundergroundPage();
        }

        private void _autoUpdateTimer_Elapsed(object sender, EventArgs e)
        {
            UpdateForecast();
        }

        private void Update_Forecast(object sender, RoutedEventArgs e)
        {
            UpdateForecast();
        }

    }
}
