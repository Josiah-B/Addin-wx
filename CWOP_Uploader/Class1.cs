using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using System.Net.Sockets;
using System.Collections.ObjectModel;
using System.Timers;
using System.Diagnostics;
using api = API.API;

namespace CWOP_Uploader
{
    [Export(typeof(api.IBackgroundPlugin))]
    public class CWOPUpload : api.IBackgroundPlugin
    {
        int _updateInterval = 5000;
        String _name = "CWOP Uploader";
        String _ManualUpdateText = "Upload Station Data";
        private ObservableCollection<api.Config> _settings;
        private ObservableCollection<api.Station> _stations;
        private String[] CWOP_Servers = new String[] { "cwop.aprs.net", "cwop.tssg.org" };
        private String[] CWOP_HAM_Servers = new String[] { "rotate.aprs.net", "rotate.aprs2.net" }; //Password Required HAM Servers
        private Timer UploadTimer = new Timer(420000);


        public void Close()
        {

        }

        public ObservableCollection<api.Config> Configuration
        {
            get
            {
                return _settings;
            }
            set
            {
                _settings = value;
            }
        }

        public String ManualUpdateText
        {
            get
            {
                return _ManualUpdateText;
            }
        }

        public void Initilize(ObservableCollection<api.Station> Stations)
        {
            _settings = new ObservableCollection<api.Config>();
            _stations = Stations;
            _stations.CollectionChanged += _stations_CollectionChanged;
            UploadTimer.Elapsed += UploadTimer_Elapsed;
            UploadTimer.Enabled = true;
            api.DebugMessanger.SendMessage(this.ToString(), "Upload Timer Enabled", "");
            //Debug.WriteLine("Upload Timer Enabled");
        }

        public void Update()
        {
            UploadStationData();
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
                            foreach (api.Config tempConfig in _settings)
                            {
                                if (tempConfig.ConfigGroupName == tempStation.Name)
                                {
                                    _settings.Remove(tempConfig);
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
            _settings.Add(new api.Config(StationName));
            _settings[_settings.Count - 1].ConfigSettings.Add(new api.Setting("Enable Upload", "No"));
            _settings[_settings.Count - 1].ConfigSettings[0].AllowedParameters.AddRange(new String[] { "Yes", "No" });
            _settings[_settings.Count - 1].ConfigSettings.Add(new api.Setting("Use HAM Servers", "No"));
            _settings[_settings.Count - 1].ConfigSettings[1].AllowedParameters.AddRange(new String[] { "Yes", "No" });
            _settings[_settings.Count - 1].ConfigSettings.Add(new api.Setting("Station ID", "EW1007"));
            _settings[_settings.Count - 1].ConfigSettings.Add(new api.Setting("Password", "-1"));
            _settings[_settings.Count - 1].ConfigSettings.Add(new api.Setting("Latitude", "36.7745"));
            _settings[_settings.Count - 1].ConfigSettings.Add(new api.Setting("Longitude", " -79.6398"));
        }

        void UploadTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            api.DebugMessanger.SendMessage(this.ToString(), "CWOP Upload Timer Fired", "");
            //Debug.WriteLine("CWOP Upload Timer Fired");
            UploadStationData();
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

        //Plugin Specific stuff
        enum SettingsIndex { EnableUpload = 0, UseHAMServers = 1, StationID = 2, Password = 3, Latitude = 4, Longitude = 5};

        private void UploadStationData()
        {
            for (int i = 0, n = _stations.Count - 1; i <= n; i++)
            {
                api.DebugMessanger.SendMessage(this.ToString(), String.Format("{0} -- {1}", _settings[i].ConfigGroupName, _settings[i].ConfigSettings[(int)SettingsIndex.EnableUpload].CurrentValue), "");
                //Debug.WriteLine("{0} -- {1}", _settings[i].ConfigGroupName, _settings[i].ConfigSettings[(int)SettingsIndex.EnableUpload].CurrentValue);
                if (_settings[i].ConfigSettings[(int)SettingsIndex.EnableUpload].CurrentValue == "Yes")
                {
                    api.DebugMessanger.SendMessage(this.ToString(),"Forming Data String","");
                    String DataString = formDataString(i);
                    //Debug.WriteLine("[{0} {1}] - {2}", DateTime.Now.ToShortDateString(), DateTime.Now.ToShortTimeString(), DataString);
                    if (DataString == "")
                    {
                        break;
                    }
                    api.DebugMessanger.SendMessage(this.ToString(), "Data String Formed", "");
                    if (_settings[i].ConfigSettings[(int)SettingsIndex.UseHAMServers].CurrentValue == "No")
                    { // Upload Using Regular servers
                        api.DebugMessanger.SendMessage(this.ToString(), "Uploading Via Regular Servers", "");
                        for (int x = 0, v = CWOP_Servers.Count() - 1; x < v; x++)
                        {
                            int UploadStatus = uploadUsingServer(CWOP_Servers[x], 14580, DataString, _settings[i].ConfigSettings[(int)SettingsIndex.StationID].CurrentValue, _settings[i].ConfigSettings[(int)SettingsIndex.Password].CurrentValue);
                            if (UploadStatus == 0)
                            {
                                break;
                            }
                        }
                    }
                    else // Uplaod using HAM Servers
                    {
                        api.DebugMessanger.SendMessage(this.ToString(), "Uploading Via HAM Servers", "");
                        for (int x = 0, v = CWOP_HAM_Servers.Count() - 1; x < v; x++)
                        {
                            int UploadStatus = uploadUsingServer(CWOP_HAM_Servers[x], 14580, DataString, _settings[i].ConfigSettings[(int)SettingsIndex.StationID].CurrentValue, _settings[i].ConfigSettings[(int)SettingsIndex.Password].CurrentValue);
                            if (UploadStatus == 0)
                            {
                                break;
                            }
                        }
                    }

                }
            }
        }
        private int uploadUsingServer(String ServerAddress, int ServerPortNum, String UploadString, String CWOP_ID, String CWOP_Password)
        {
            api.DebugMessanger.SendMessage(this.ToString(), "Connecting to CWOP Server.", "Server Address=" + ServerAddress);
            try
            {
                using (TcpClient CWOP_Client = new TcpClient(ServerAddress, ServerPortNum))
                {
                    if (CWOP_Client.Connected)
                    {
                        api.DebugMessanger.SendMessage(this.ToString(), "Connection Opened to CWOP Server.", String.Concat("Server Address=", ServerAddress));
                        using (NetworkStream CWOP_Client_DataStream = CWOP_Client.GetStream())
                        {
                            System.Threading.Thread.Sleep(3000);
                            Byte[] Data = System.Text.Encoding.ASCII.GetBytes(String.Format("user {0} pass {1} vers Add-InWx_Plugin_CWOP {2}\r\n", CWOP_ID, CWOP_Password, "1.0"));
                            api.DebugMessanger.SendMessage(this.ToString(), "Sending Connection Request", String.Concat("Data String=", String.Format("user {0} pass {1} vers Add-InWx_Plugin_CWOP {2}\r\n", CWOP_ID, CWOP_Password, "1.0")));
                            CWOP_Client_DataStream.Write(Data, 0, Data.Length);
                            Byte[] databytes = new byte[1024];
                            int msgArray = CWOP_Client_DataStream.Read(databytes, 0, databytes.Length);
                            String msg = System.Text.Encoding.ASCII.GetString(databytes, 0, msgArray);
                            if (msg != "")
                            {
                                //connected and starting data upload
                                //form data string
                                System.Threading.Thread.Sleep(3000);
                                api.DebugMessanger.SendMessage(this.ToString(), "Uploading Data.", String.Concat("Data String=" , UploadString));
                                Data = System.Text.Encoding.ASCII.GetBytes(String.Concat(UploadString, "\r\n"));
                                CWOP_Client_DataStream.Write(Data, 0, Data.Length); //send data
                                System.Threading.Thread.Sleep(3000); // Delay 3 Seconds before Disconnecting
                                api.DebugMessanger.SendMessage(this.ToString(), "Data Upload Completed.", String.Concat("Server Address=" , ServerAddress));
                                //Data Uploaded
                                return 0; //upload success
                            }
                            else
                            {
                                return -2; // login failed
                            }
                        }
                    }
                    else
                    {
                        api.DebugMessanger.SendMessage(this.ToString(), "Failed to Connect to CWOP Server.", String.Concat("Server Address=", ServerAddress));
                        return -1; // failed to connect to server
                    }
                }
            }
            catch
            {
                api.DebugMessanger.SendMessage(this.ToString(), "Failed to Connect to CWOP Server.", String.Concat("Server Address=", ServerAddress));
                return -1;
            }
            //api.DebugMessanger.SendMessage(this.ToString(), "Failed to Connect to CWOP Server.", "Server Address=" + ServerAddress);
            //return -1;
        }
        private String formDataString(int cStation)
        {
           
            String CWOPStationID = "", LAT_LORAN = "", LONG_LORAN = "", WindBearing = "", WindAverage = "", OutDoorTemp = "", WindPeak = "";
            
            //Does not except ommision. use '...' instead if needed
            CWOPStationID = _settings[cStation].ConfigSettings[(int)SettingsIndex.StationID].CurrentValue;
            LAT_LORAN = api.Conversion.DecimalDegrees_To_LORAN_DecimalDegreeMinutes(Convert.ToDouble(_settings[cStation].ConfigSettings[(int)SettingsIndex.Latitude].CurrentValue), true);
            LONG_LORAN = api.Conversion.DecimalDegrees_To_LORAN_DecimalDegreeMinutes(Convert.ToDouble(_settings[cStation].ConfigSettings[(int)SettingsIndex.Longitude].CurrentValue), false);
            WindBearing = GetFromatedDataVal(Convert.ToDouble(_stations[cStation].WXStation.WXData.WindBearing.Value), "000", 0, true, "_", false);
            WindAverage = GetFromatedDataVal(_stations[cStation].WXStation.WXData.WindSpeed.AsEnglish, "000", 0, true, "/", false);
            OutDoorTemp = GetFromatedDataVal(_stations[cStation].WXStation.WXData.TempOutCur.AsEnglish, "000", 0, true, "t", false);
            WindPeak = GetFromatedDataVal(_stations[cStation].WXStation.WXData.Wind5MinPeak.AsEnglish, "000", 0, true, "g", false);
            

            String RainLastHour = "", RainSinceMidnight = "", Humidity = "", Barometer = "";
            
            //Does except ommision
            RainLastHour = GetFromatedDataVal(_stations[cStation].WXStation.WXData.RainRateAnHr.AsEnglish, "000", 100, false, "r", true);
            RainSinceMidnight = GetFromatedDataVal(_stations[cStation].WXStation.WXData.RainTodayTotal.AsEnglish, "000", 100, false, "P", true);

            Humidity = GetHumidity(_stations[cStation].WXStation.WXData.HumidityOutCur.Value);

            Barometer = GetFromatedDataVal(_stations[cStation].WXStation.WXData.BarometerCur.AsMetric, "00000", 10, false, "b", true);
            
            String ProgramName_Comments = "Add-InWx";

            //String DataString = String.Format("{0}>APRS,TCPIP*:!{1}/{2}{3}{4}{5}{6}{7}{8}{9}{10}e{11}", CWOPStationID, LAT_LORAN, LONG_LORAN, WindBearing, WindAverage, WindPeak, OutDoorTemp, RainLastHour, RainSinceMidnight, Humidity, Barometer, ProgramName_Comments);
            String DataString = CWOPStationID + ">APRS,TCPIP*:!" + LAT_LORAN + "/" + LONG_LORAN + WindBearing + WindAverage + WindPeak;
            DataString = DataString + OutDoorTemp + RainLastHour + RainSinceMidnight + Humidity + Barometer + "e" + ProgramName_Comments;
            
            return DataString;
        }
        private String GetHumidity(String Humidity)
        {
            if (Convert.ToDouble(Humidity) != api.DataErrorVal)
            {
                if (Humidity == "100")
                {
                    return "h00";
                }
                else
                {
                    Decimal hum = Convert.ToDecimal(Humidity);
                    Math.Round(hum);
                    return "h" + String.Format(hum.ToString(), "00");
                }
            }
            else
            {
                return ""; //Return the ommision String
            }
        }
        private String GetFromatedDataVal(Double DataVal, String FormatString, Double Multiplier, Boolean RoundVal, String prefixChar, Boolean isNullable)
        {
            try
            {
            if (DataVal == api.DataErrorVal)
            {
                if (isNullable == true)
                {
                    return "";
                }
                else
                {
                    return String.Concat(prefixChar, "...");
                }
            }
            else
            {
                Double val = DataVal;
                if (Multiplier != 0)
                {
                    val = val * Multiplier;
                }

                if (RoundVal == true)
                {
                    return String.Concat(prefixChar, Decimal.Round(Convert.ToDecimal(val),0).ToString(FormatString));
                }
                else
                {
                    return String.Concat(prefixChar, val.ToString(FormatString));
                }
            }

            }
            catch (Exception ex)
            {
                api.DebugMessanger.SendMessage(this.ToString(), "AT: GetFormatedDataVal....Prefix==" + prefixChar + "...Val==" + DataVal, "Exception==" + ex.Message);
                if (isNullable == true)
                {
                    return "";
                }
                else
                {
                    return String.Concat(prefixChar, "...");
                }
            }
        }

        public bool AllowsManualUpdate
        {
            get
            {
                return true;
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
    }
}
