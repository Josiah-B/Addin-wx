using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using api = API.API;
using System.IO;
using System.ComponentModel.Composition;
using System.Timers;

namespace SteelSeriesGauges
{
    [Export(typeof(api.IBackgroundPlugin))]
    public class Class1 : api.IBackgroundPlugin
    {
        String _pluginName = "Steel Series Gauges";
        int _updateInterval = 30000;
        String _manualUpdateText = "Update Gauges";
        Boolean _allowsManualUpdate = true;
        System.Collections.ObjectModel.ObservableCollection<api.Config> _config;
        System.Collections.ObjectModel.ObservableCollection<api.Station> _Stations;

        String FileName = "realtimegauges.txt";
        private Timer UploadTimer = new Timer(30000);

        public string Name
        {
            get
            {
                return _pluginName;
            }
            set
            {
                _pluginName = value;
            }
        }

        public int UpdateInterval
        {
            get
            {
                return _updateInterval;
            }
            set
            {
                _updateInterval = value;
            }
        }

        public string ManualUpdateText
        {
            get
            {
                return _manualUpdateText;
            }
        }

        public bool AllowsManualUpdate
        {
            get
            {
                return _allowsManualUpdate;
            }
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

        public void Initilize(System.Collections.ObjectModel.ObservableCollection<api.Station> Stations)
        {
            _Stations = Stations;
            intConfig();
            _Stations.CollectionChanged += _stations_CollectionChanged;
            UploadTimer.Elapsed += UploadTimer_Elapsed;
            UploadTimer.Enabled = true;
        }
        
        void UploadTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Update();
        }

        void _stations_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    {
                        foreach (api.Station tempstation in e.NewItems)
                        {
                            _config[0].ConfigSettings[0].AllowedParameters.Add(tempstation.Name);
                        }
                        break;
                    }
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    {
                        foreach (api.Station tempStation in e.OldItems)
                        {
                            foreach (String StationName in _config[0].ConfigSettings[0].AllowedParameters)
                            {
                                if (StationName == tempStation.Name)
                                {
                                    _config[0].ConfigSettings[0].AllowedParameters.Remove(StationName);
                                    break;
                                }
                            }
                        }
                        break;
                    }
            }
        }
        private void intConfig()
        {
            _config = new System.Collections.ObjectModel.ObservableCollection<api.Config>();
            _config.Add(new api.Config("General"));
            _config[0].ConfigSettings.Add(new api.Setting("Station Data to Use", ""));
            _config[0].ConfigSettings.Add(new api.Setting("Output Folder Path", ""));
        }
        public void Update()
        {
            if (_Stations.Count() == 0)
            {
                return;
            }
            int currentStation = 0;
            if (_config[0].ConfigSettings[0].CurrentValue != "")
            {
                for (int i = 0, n = _Stations.Count() - 1; i < n; i++)
                {
                    if (_config[0].ConfigSettings[0].CurrentValue == _Stations[i].Name)
                    {
                        currentStation = i;
                        break;
                    }
                }
            }
            //_config[0].ConfigSettings[1].CurrentValue == Directory Path to the File
            String completeFilePath = Path.Combine(_config[0].ConfigSettings[1].CurrentValue, FileName);
            if (System.IO.File.Exists(completeFilePath))
            {
                System.IO.File.Delete(completeFilePath);
            }
            using (FileStream writefile = System.IO.File.OpenWrite(completeFilePath))
            {
                //(char)34 == "
                //(char)13 == CR or Carriage return
                //(char)10 == LF or new line
                char endChar1 = (char)13;
                char endchar2 = (char)10;
                
                AddText(writefile, "{");
                AddText(writefile, (char)34 + "date" + (char)34 + ":" + (char)34 + DateTime.UtcNow.Hour + ":" + DateTime.UtcNow.Minute + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "temp" + (char)34 + ":" + (char)34 + _Stations[currentStation].WXStation.WXData.TempOutCur.AsEnglish + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "tempTL" + (char)34 + ":" + (char)34 + _Stations[currentStation].WXStation.WXData.TempOutLow.AsEnglish + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "tempTH" + (char)34 + ":" + (char)34 + _Stations[currentStation].WXStation.WXData.TempOutHigh.AsEnglish + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "intemp" + (char)34 + ":" + (char)34 + _Stations[currentStation].WXStation.WXData.TempInCur.AsEnglish + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "dew" + (char)34 + ":" + (char)34 + _Stations[currentStation].WXStation.WXData.DewPoint.AsEnglish + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "dewpointTL" + (char)34 + ":" + (char)34 + "0" + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "dewpointTH" + (char)34 + ":" + (char)34 + "0" + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "apptemp" + (char)34 + ":" + (char)34 + _Stations[currentStation].WXStation.WXData.TempApparent.AsEnglish + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "apptempTL" + (char)34 + ":" + (char)34 + "0" + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "apptempTH" + (char)34 + ":" + (char)34 + "0" + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "wchill" + (char)34 + ":" + (char)34 + _Stations[currentStation].WXStation.WXData.WindChill.AsEnglish + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "wchillTL" + (char)34 + ":" + (char)34 + "0" + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "heatindex" + (char)34 + ":" + (char)34 + _Stations[currentStation].WXStation.WXData.HeatIndex.AsEnglish + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "heatindexTH" + (char)34 + ":" + (char)34 + "0" + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "humidex" + (char)34 + ":" + (char)34 + "0" + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "wlatest" + (char)34 + ":" + (char)34 + _Stations[currentStation].WXStation.WXData.WindSpeed.AsEnglish + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "wspeed" + (char)34 + ":" + (char)34 + "0" + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "wgust" + (char)34 + ":" + (char)34 + "0" + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "wgustTM" + (char)34 + ":" + (char)34 + "0" + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "bearing" + (char)34 + ":" + (char)34 + _Stations[currentStation].WXStation.WXData.WindBearing.AsEnglish + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "avgbearing" + (char)34 + ":" + (char)34 + _Stations[currentStation].WXStation.WXData.WindBearing.AsEnglish + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "press" + (char)34 + ":" + (char)34 + _Stations[currentStation].WXStation.WXData.BarometerCur.AsEnglish + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "pressTL" + (char)34 + ":" + (char)34 + _Stations[currentStation].WXStation.WXData.BarometerLow.AsEnglish + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "pressTH" + (char)34 + ":" + (char)34 + _Stations[currentStation].WXStation.WXData.BarometerHigh.AsEnglish + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "pressL" + (char)34 + ":" + (char)34 + _Stations[currentStation].WXStation.WXData.BarometerLow.AsEnglish + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "pressH" + (char)34 + ":" + (char)34 + _Stations[currentStation].WXStation.WXData.BarometerHigh.AsEnglish + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "rfall" + (char)34 + ":" + (char)34 + _Stations[currentStation].WXStation.WXData.RainTodayTotal.AsEnglish + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "rrate" + (char)34 + ":" + (char)34 + _Stations[currentStation].WXStation.WXData.RainRateAnHr.AsEnglish + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "rrateTM" + (char)34 + ":" + (char)34 + "0" + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "hum" + (char)34 + ":" + (char)34 + _Stations[currentStation].WXStation.WXData.HumidityOutCur.AsEnglish + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "humTL" + (char)34 + ":" + (char)34 + _Stations[currentStation].WXStation.WXData.HumidityOutLow.AsEnglish + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "humTH" + (char)34 + ":" + (char)34 + _Stations[currentStation].WXStation.WXData.HumidityOutHigh.AsEnglish + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "inhum" + (char)34 + ":" + (char)34 + _Stations[currentStation].WXStation.WXData.HumidityIn.AsEnglish + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "SensorContactLost" + (char)34 + ":" + (char)34 + "0" + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "forecast" + (char)34 + ":" + (char)34 + "The last update was on " + DateTime.Now.ToString("dddd, MMMM dd") + " at " + DateTime.Now.ToShortTimeString() + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "tempunit" + (char)34 + ":" + (char)34 + "F" + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "windunit" + (char)34 + ":" + (char)34 + "mph" + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "pressunit" + (char)34 + ":" + (char)34 + "in" + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "rainunit" + (char)34 + ":" + (char)34 + "in" + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "temptrend" + (char)34 + ":" + (char)34 + "0" + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "TtempTL" + (char)34 + ":" + (char)34 + _Stations[currentStation].WXStation.WXData.TempOutLow.TimeOfValue.ToString("HH:mm") + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "TtempTH" + (char)34 + ":" + (char)34 + _Stations[currentStation].WXStation.WXData.TempOutHigh.TimeOfValue.ToString("HH:mm") + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "TdewpointTL" + (char)34 + ":" + (char)34 + "00:00" + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "TdewpointTH" + (char)34 + ":" + (char)34 + "00:00" + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "TapptempTL" + (char)34 + ":" + (char)34 + "00:00" + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "TapptempTH" + (char)34 + ":" + (char)34 + "00:00" + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "TwchillTL" + (char)34 + ":" + (char)34 + "00:00" + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "TheatindexTH" + (char)34 + ":" + (char)34 + "00:00" + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "TrrateTM" + (char)34 + ":" + (char)34 + "00:00" + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "ThourlyrainTH" + (char)34 + ":" + (char)34 + "00:00" + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "LastRainTipISO" + (char)34 + ":" + (char)34 + "2015-01-13 10:30" + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "hourlyrainTH" + (char)34 + ":" + (char)34 + _Stations[currentStation].WXStation.WXData.RainRateAnHr.AsEnglish + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "ThumTL" + (char)34 + ":" + (char)34 + _Stations[currentStation].WXStation.WXData.HumidityOutLow.TimeOfValue.ToString("HH:mm") + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "ThumTH" + (char)34 + ":" + (char)34 + _Stations[currentStation].WXStation.WXData.HumidityOutHigh.TimeOfValue.ToString("HH:mm") + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "TpressTL" + (char)34 + ":" + (char)34 + _Stations[currentStation].WXStation.WXData.BarometerLow.TimeOfValue.ToString("HH:mm") + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "TpressTH" + (char)34 + ":" + (char)34 + _Stations[currentStation].WXStation.WXData.BarometerHigh.TimeOfValue.ToString("HH:mm") + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "presstrendval" + (char)34 + ":" + (char)34 + _Stations[currentStation].WXStation.WXData.Barometer3HrChange.AsEnglish + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "Tbeaufort" + (char)34 + ":" + (char)34 + "0" + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "TwgustTM" + (char)34 + ":" + (char)34 + "00:00" + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "windTM" + (char)34 + ":" + (char)34 + _Stations[currentStation].WXStation.WXData.WindPeakGust.AsEnglish + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "bearingTM" + (char)34 + ":" + (char)34 + "0" + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "timeUTC" + (char)34 + ":" + (char)34 + DateTime.UtcNow.Year + "," + DateTime.UtcNow.Month + "," + DateTime.UtcNow.Day + "," + DateTime.UtcNow.Hour + "," + DateTime.UtcNow.Minute + "," + DateTime.UtcNow.Second + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "BearingRangeFrom10" + (char)34 + ":" + (char)34 + "0" + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "BearingRangeTo10" + (char)34 + ":" + (char)34 + "360" + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "UV" + (char)34 + ":" + (char)34 + _Stations[currentStation].WXStation.WXData.UVIndex.AsEnglish + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "UVTH" + (char)34 + ":" + (char)34 + _Stations[currentStation].WXStation.WXData.UVIndex.AsEnglish + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "SolarRad" + (char)34 + ":" + (char)34 + _Stations[currentStation].WXStation.WXData.SolarRaidiation.AsEnglish + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "SolarTM" + (char)34 + ":" + (char)34 + _Stations[currentStation].WXStation.WXData.SolarRaidiation.AsEnglish + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "CurrentSolarMax" + (char)34 + ":" + (char)34 + "0" + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "domwinddir" + (char)34 + ":" + (char)34 + _Stations[currentStation].WXStation.WXData.WindDirection.AsEnglish + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "WindRoseData" + (char)34 + ":" + "[0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0,0,0,0,0]" + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "windrun" + (char)34 + ":" + (char)34 + "0" + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "version" + (char)34 + ":" + (char)34 + "1.0.0.2" + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "build" + (char)34 + ":" + (char)34 + "1" + (char)34 + "," + endChar1 + endchar2);
                AddText(writefile, (char)34 + "ver" + (char)34 + ":" + (char)34 + "11" + (char)34 + "}");
            }

        }
        private static double getdataval(double dataVal)
        {
            if (dataVal == api.DataErrorVal)
            {
                return 0.0;
            }
            return dataVal;
        }
        private static void AddText(FileStream fs, string value)
        {
            byte[] info = new UTF8Encoding(true).GetBytes(value);
            fs.Write(info, 0, info.Length);
        }

        public void Close()
        {
            
        }
    }
}
