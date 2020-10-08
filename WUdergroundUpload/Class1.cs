using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using api = API.API;
using System.ComponentModel.Composition;
using System.Collections.ObjectModel;
using System.Net;
using System.Timers;

namespace WUdergroundUpload
{
    [Export(typeof(api.IBackgroundPlugin))]
    public class WUnderground_Uploader : api.IBackgroundPlugin
    {
        String _manualUpdateText = "Upload Station Data";
        String _name = "WUnderground Uploader";
        private ObservableCollection<api.Config> _config;
        private ObservableCollection<api.Station> _stations;
        static int _uploadinterval = 120000;
        static int RapidFireInterval = 3000;
        enum configIndex { UploadEnabled = 0, StationID = 1, Password = 2, UseRapidFire = 3 }

        private Timer uploadtimer = new Timer(_uploadinterval);
        private Timer RapidFireTimer = new Timer(RapidFireInterval);

        public bool AllowsManualUpdate
        {
            get
            {
                return true;
            }
        }

        public void Close()
        {

        }

        public ObservableCollection<api.Config> Configuration
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

        public void Initilize(ObservableCollection<api.Station> Stations)
        {
            api.DebugMessanger.SendMessage(this.ToString(), "WUnderground Uploader Loading", "");
            _config = new ObservableCollection<api.Config>();
            _stations = Stations;
            _stations.CollectionChanged += _stations_CollectionChanged;
            uploadtimer.Elapsed += UploadTimer_Elapsed;
            uploadtimer.Enabled = true;
            RapidFireTimer.Elapsed += rapidFireUploadTimer_Elapsed;
            RapidFireTimer.Enabled = true;

            api.DebugMessanger.SendMessage(this.ToString(), "WUnderground Uploader Loaded", "");
        }

        void _stations_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            //int changedIndex = e.NewStartingIndex;
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    {
                        foreach (api.Station tempstation in e.NewItems)
                        {
                            addSettings(tempstation.Name);
                        }
                        break;
                    }
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    {
                        foreach (api.Station tempStation in e.OldItems)
                        {
                            foreach (api.Config tempConfig in _config)
                            {
                                if (tempConfig.ConfigGroupName == tempStation.Name)
                                {
                                    _config.Remove(tempConfig);
                                    break;
                                }
                            }
                        }
                        break;
                    }
            }
        }

        private void addSettings(String StationName)
        {
            _config.Add(new api.Config(StationName));
            _config[_config.Count - 1].ConfigSettings.Add(new api.Setting("Enable Upload", "No"));
            _config[_config.Count - 1].ConfigSettings[0].AllowedParameters.AddRange(new String[] { "Yes", "No" });
            _config[_config.Count - 1].ConfigSettings.Add(new api.Setting("Station ID", ""));
            _config[_config.Count - 1].ConfigSettings.Add(new api.Setting("Password", ""));
            _config[_config.Count - 1].ConfigSettings.Add(new api.Setting("Use Rapid Fire", "No"));
            _config[_config.Count - 1].ConfigSettings[3].AllowedParameters.AddRange(new String[] { "Yes", "No" });
        }

        public string ManualUpdateText
        {
            get
            {
                return _manualUpdateText;
            }
        }

        public string Name
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

        void UploadTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            uploadStationData(false);
        }
        void rapidFireUploadTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            uploadStationData(true);
        }
        private void uploadStationData(Boolean IsRapidFire)
        {
            for (int i = 0, n = _stations.Count - 1; i <= n; i++)
            {
                if (_config[i].ConfigSettings[(int)configIndex.UploadEnabled].CurrentValue == "Yes")
                {
                    if (_config[i].ConfigSettings[(int)configIndex.UseRapidFire].CurrentValue == "No" && !(IsRapidFire))
                    {
                        api.DebugMessanger.SendMessage(this.ToString(), "Forming Data String", "Uploading Data from Station.. " + _stations[i].Name);
                        uploadDataUsingServer("http://weatherstation.wunderground.com", i, "");
                    }

                    if (_config[i].ConfigSettings[(int)configIndex.UseRapidFire].CurrentValue == "Yes" && IsRapidFire)
                    {
                        api.DebugMessanger.SendMessage(this.ToString(), "Forming Data String", "Uploading Data from Station.. " + _stations[i].Name);
                        uploadDataUsingServer("http://rtupdate.wunderground.com", i, "&realtime=1&rtfreq=" + RapidFireInterval / 1000);
                    }
                }
            }
        }
        public void Update()
        {
            uploadStationData(false);
        }
        private void uploadDataUsingServer(String serverAddress, int StationIndex, String AppendToEnd)
        {
            serverAddress += GetUploadString(StationIndex);
            api.DebugMessanger.SendMessage(this.ToString(), "Data String=" + serverAddress, "");

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(serverAddress);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            System.IO.Stream responseStream = response.GetResponseStream();

            System.IO.StreamReader responseStreamReader = new System.IO.StreamReader(responseStream);
            String Data = responseStreamReader.ReadToEnd();
            responseStreamReader.Close();

            if (Data.StartsWith("success"))
            {
                api.DebugMessanger.SendMessage(this.ToString(), "Upload Success", "");
            }
            else if (Data.StartsWith("INVALIDPASSWORDID"))
            {
                api.DebugMessanger.SendMessage(this.ToString(), "Upload Failed", "Invalid ID or Password");
            }
            else
            {
                api.DebugMessanger.SendMessage(this.ToString(), "Upload Failed", "Unkown");
            }

        }
        private String GetUploadString(int _stationIndex)
        {
            String UploadString = "/weatherstation/updateweatherstation.php?ID=";
            UploadString = String.Concat(UploadString, _config[_stationIndex].ConfigSettings[(int)configIndex.StationID].CurrentValue);
            UploadString = String.Concat(UploadString, "&PASSWORD=", _config[_stationIndex].ConfigSettings[(int)configIndex.Password].CurrentValue);
            UploadString = String.Concat(UploadString, "&dateutc=", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss").Replace(" ", "+").Replace(":", "%3A"));
            if (Convert.ToDouble(_stations[_stationIndex].WXStation.WXData.WindBearing.AsEnglish) != api.DataErrorVal)
            {
                UploadString = String.Concat(UploadString, "&winddir=", _stations[_stationIndex].WXStation.WXData.WindBearing.AsEnglish);
            }

            if (_stations[_stationIndex].WXStation.WXData.WindSpeed.AsEnglish != api.DataErrorVal)
            {
                UploadString = String.Concat(UploadString, "&windspeedmph=", Convert.ToString(_stations[_stationIndex].WXStation.WXData.WindSpeed.AsEnglish));
            }

            if (Convert.ToDouble(_stations[_stationIndex].WXStation.WXData.HumidityOutCur.AsEnglish) != api.DataErrorVal)
            {
                UploadString = String.Concat(UploadString, "&humidity=", _stations[_stationIndex].WXStation.WXData.HumidityOutCur.AsEnglish);
            }

            if (_stations[_stationIndex].WXStation.WXData.DewPoint.AsEnglish != api.DataErrorVal)
            {
                UploadString = String.Concat(UploadString, "&dewptf=", Convert.ToString(_stations[_stationIndex].WXStation.WXData.DewPoint.AsEnglish));
            }

            if (_stations[_stationIndex].WXStation.WXData.TempOutCur.AsEnglish != api.DataErrorVal)
            {
                UploadString = String.Concat(UploadString, "&tempf=", Convert.ToString(_stations[_stationIndex].WXStation.WXData.TempOutCur.AsEnglish));
            }

            if (_stations[_stationIndex].WXStation.WXData.RainTodayTotal.AsEnglish != api.DataErrorVal)
            {
                UploadString = String.Concat(UploadString, "&dailyrainin=", Convert.ToString(_stations[_stationIndex].WXStation.WXData.RainTodayTotal.AsEnglish));
            }

            if (_stations[_stationIndex].WXStation.WXData.BarometerCur.AsEnglish != api.DataErrorVal)
            {
                UploadString = String.Concat(UploadString, "&baromin=", Convert.ToString(_stations[_stationIndex].WXStation.WXData.BarometerCur.AsEnglish));
            }

            UploadString = String.Concat(UploadString, "&softwaretype=AddinWx", "&action=updateraw");
            return UploadString;
        }


        public int UpdateInterval
        {
            get
            {
                return _uploadinterval;
            }
            set
            {
                _uploadinterval = value;
            }
        }
    }
}
