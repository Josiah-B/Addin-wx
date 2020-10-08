// VBConversions Note: VB project level imports
using System.Collections.Generic;
using System;
using System.Diagnostics;
using Microsoft.VisualBasic;
using System.Xml.Linq;
using System.Collections;
using System.Linq;
// End of VB project level imports

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Management;
using System.Windows.Threading;
using System.Collections.Specialized;
using System.Threading;

//Imports CustomObserableCollection

namespace API
{
    public class API
    {
        public static double DataErrorVal = -9999;
        public static DebugMessenger DebugMessanger = new DebugMessenger();
        public class Station
        {
            private string WXName;
            private IWXStation DataStation;
            public string WXStationType { get; set; }
            public BackgroundWorker BackgroundUpdater;
            private bool FirstDataUpdate = true;
            private bool UpdatePrecipTimerData = true;
            private List<Double> PrecipRateVals = new List<Double>();
            private List<Double> Bar3HrChange = new List<Double>();
            private System.Timers.Timer PrecipRateTimer = new System.Timers.Timer(60000);
            private System.Timers.Timer UpdateStationDataTimer = new System.Timers.Timer(5000);
            public bool IsBusy { get { return BackgroundUpdater.IsBusy; } }


            public LatLong Location { get; set; }

            public string Name
            {
                get
                {
                    return WXName;
                }
                private set
                {
                    WXName = value;
                }
            }

            public IWXStation WXStation
            {
                get
                {
                    return DataStation;
                }
                set
                {
                    DataStation = value;
                }
            }

            public Station(string StationName, IWXStation Station, string StationType)
            {
                DataStation = Station;
                WXName = StationName;
                WXStationType = StationType;
                WXStation.ApplySettings(new ObservableCollection<Config>());
                intilizeTimers();
            }

            public void StartUpdating()
            {
                UpdateStationDataTimer.Enabled = true;
            }

            public void Reconnect()
            {
                CloseStation();
                StartUpdating();
            }

            public void CloseStation()
            {
                UpdateStationDataTimer.Enabled = false;
                WXStation.Disconnect();
            }

            private void BackgroundUpdater_DoWork(object sender, DoWorkEventArgs e)
            {
                try
                {
                    WXStation.Update();
                    FillOtherData();

                }
                catch (Exception ex)
                {
                    Debug.WriteLine("@" + WXName + " An Error as occured: " + ex.Message);
                }

                if (BackgroundUpdater.CancellationPending == true)
                {
                    Debug.WriteLine("Thread to Update " + WXName + " was Canceled");
                    DebugMessanger.SendMessage(WXName, "Exiting Update Loop and Disconnecting", "");
                    WXStation.Disconnect();
                    e.Cancel = true;
                    //break;
                }
            }

            private void intilizeTimers()
            {
                PrecipRateTimer.Elapsed += PrecipRateTimer_Elapsed;
                PrecipRateTimer.Enabled = true;

                UpdateStationDataTimer.Interval = DataStation.UpdateInterval_Milliseconds;
                UpdateStationDataTimer.Elapsed += UpdateStationDataTimer_Elapsed;
                UpdateStationDataTimer.Enabled = true;
            }

            private void UpdateStationDataTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
            {
                if (BackgroundUpdater == null || !BackgroundUpdater.IsBusy)
                {
                    BackgroundUpdater = new BackgroundWorker();
                    BackgroundUpdater.DoWork += BackgroundUpdater_DoWork;
                    BackgroundUpdater.RunWorkerAsync();
                }
            }

            void PrecipRateTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
            {
                PrecipTimerFired();
            }

            private void PrecipTimerFired()
            {
                UpdatePrecipTimerData = true;
            }

            private void Precip_3HrBarTimerFired()
            {

                if (WXStation.WXData.RainRateAnHr.autoCalculate && (WXStation.WXData.RainTodayTotal.Value != DataErrorVal))
                {
                    PrecipRateVals.Add(WXStation.WXData.RainTodayTotal.Value);
                    if (PrecipRateVals.Count > 60)
                    {
                        PrecipRateVals.RemoveAt(0);
                    }
                    WXStation.WXData.RainRateAnHr.Value = Convert.ToDouble(Decimal.Round(Convert.ToDecimal(WXStation.WXData.RainTodayTotal.Value - PrecipRateVals[0]), 2));
                    WXStation.WXData.RainRateAnHr.TimeOfValue = DateTime.Now;
                }
                //3Hr Barometer Change
                if (WXStation.WXData.Barometer3HrChange.autoCalculate && (WXStation.WXData.BarometerCur.Value != DataErrorVal))
                {
                    Bar3HrChange.Add(WXStation.WXData.BarometerCur.Value);
                    //Dim BarAverage As Decimal = 0
                    //For i = 0 To Bar3HrChange.Count - 1
                    //    BarAverage += CDec(Bar3HrChange(i).Value)
                    //Next
                    //If Bar3HrChange.Count > 0 Then
                    //    BarAverage /= Bar3HrChange.Count
                    //End If
                    WXStation.WXData.Barometer3HrChange.Value = Convert.ToDouble(Decimal.Round(Convert.ToDecimal(WXStation.WXData.BarometerCur.Value - Bar3HrChange[0]), 2));
                    WXStation.WXData.Barometer3HrChange.TimeOfValue = DateTime.Now;
                    if (Bar3HrChange.Count > 180) // This sets the max number of elements in the list or how far back we want to store
                    {
                        Bar3HrChange.RemoveAt(0);
                    }
                }
            }

            private void FillOtherData()
            {

                //Fire off the PrecipRate/BarChange Timer so the Sky Conditions and Active weather Has something to work with
                if (FirstDataUpdate || UpdatePrecipTimerData)
                {
                    FirstDataUpdate = false;
                    UpdatePrecipTimerData = false;
                    Precip_3HrBarTimerFired();

                }

                if (WXStation.WXData.WindDirection.autoCalculate && (Convert.ToDouble(WXStation.WXData.WindBearing.Value) != DataErrorVal))
                {
                    WXStation.WXData.WindDirection.Value = Conversion.GetWindDirection(Convert.ToDouble(WXStation.WXData.WindBearing.Value));
                    WXStation.WXData.WindDirection.TimeOfValue = DateTime.Now;

                }
                //Apparent Temp
                if (WXStation.WXData.TempApparent.autoCalculate)
                {
                    WXStation.WXData.TempApparent.Value = Conversion.ApperentTemperature_feelsLike(WXStation.WXData.TempOutCur.AsEnglish, Convert.ToDouble(WXStation.WXData.HumidityOutCur.Value), WXStation.WXData.WindSpeed.AsEnglish);
                    WXStation.WXData.TempApparent.TimeOfValue = DateTime.Now;
                }

                //OutDoor Temperature High and Low
                if (WXStation.WXData.TempOutHigh.autoCalculate)
                {
                    WXStation.WXData.TempOutHigh = UpdateHighTemp(WXStation.WXData.TempOutCur, WXStation.WXData.TempOutHigh);
                }
                if (WXStation.WXData.TempOutLow.autoCalculate)
                {
                    WXStation.WXData.TempOutLow = UpdateLowTemp(WXStation.WXData.TempOutCur, WXStation.WXData.TempOutLow);
                }

                //OutDoor Humidity High and Low
                if ((WXStation.WXData.HumidityOutHigh.autoCalculate | WXStation.WXData.HumidityOutLow.autoCalculate) && (Convert.ToDouble(WXStation.WXData.HumidityOutCur.Value) != DataErrorVal))
                {
                    if (WXStation.WXData.HumidityOutHigh.Value == DataErrorVal.ToString())
                    {
                        WXStation.WXData.HumidityOutHigh.Value = WXStation.WXData.HumidityOutCur.Value;
                        WXStation.WXData.HumidityOutHigh.TimeOfValue = WXStation.WXData.HumidityOutCur.TimeOfValue;
                    }
                    else if (Convert.ToDouble(WXStation.WXData.HumidityOutCur.Value) >= Convert.ToDouble(WXStation.WXData.HumidityOutHigh.Value))
                    {
                        WXStation.WXData.HumidityOutHigh.Value = WXStation.WXData.HumidityOutCur.Value;
                        WXStation.WXData.HumidityOutHigh.TimeOfValue = WXStation.WXData.HumidityOutCur.TimeOfValue;
                    }
                    if (Convert.ToDouble(WXStation.WXData.HumidityOutLow.Value) == DataErrorVal)
                    {
                        WXStation.WXData.HumidityOutLow.Value = WXStation.WXData.HumidityOutCur.Value;
                        WXStation.WXData.HumidityOutLow.TimeOfValue = WXStation.WXData.HumidityOutCur.TimeOfValue;
                    }
                    else if (Convert.ToDouble(WXStation.WXData.HumidityOutCur.Value) <= Convert.ToDouble(WXStation.WXData.HumidityOutLow.Value))
                    {
                        WXStation.WXData.HumidityOutLow.Value = WXStation.WXData.HumidityOutCur.Value;
                        WXStation.WXData.HumidityOutLow.TimeOfValue = WXStation.WXData.HumidityOutCur.TimeOfValue;
                    }
                }

                //Indoor Temperature High and Low
                if (WXStation.WXData.TempInHigh.autoCalculate)
                {
                    WXStation.WXData.TempInHigh = UpdateHighTemp(WXStation.WXData.TempInCur, WXStation.WXData.TempInHigh);
                }
                if (WXStation.WXData.TempInLow.autoCalculate)
                {
                    WXStation.WXData.TempInLow = UpdateHighTemp(WXStation.WXData.TempInCur, WXStation.WXData.TempInLow);
                }

                //Barometer High and Low
                if (WXStation.WXData.BarometerHigh.autoCalculate)
                {
                    WXStation.WXData.BarometerHigh = UpdateHighBarometer(WXStation.WXData.BarometerCur, WXStation.WXData.BarometerHigh);
                }
                if (WXStation.WXData.BarometerLow.autoCalculate)
                {
                    WXStation.WXData.BarometerLow = UpdateLowBarometer(WXStation.WXData.BarometerCur, WXStation.WXData.BarometerLow);
                }

                //Peak Wind Speed
                if (WXStation.WXData.WindPeakGust.autoCalculate && (WXStation.WXData.WindSpeed.Value != DataErrorVal))
                {
                    if (WXStation.WXData.WindPeakGust.Value == DataErrorVal || WXStation.WXData.WindPeakGust.TimeOfValue.Date != WXStation.WXData.WindSpeed.TimeOfValue.Date)
                    {
                        WXStation.WXData.WindPeakGust.Value = WXStation.WXData.WindSpeed.Value;
                        WXStation.WXData.WindPeakGust.TimeOfValue = WXStation.WXData.WindSpeed.TimeOfValue;
                    }
                    else if (Convert.ToDouble(WXStation.WXData.WindSpeed.Value) >= Convert.ToDouble(WXStation.WXData.WindPeakGust.Value))
                    {
                        WXStation.WXData.WindPeakGust.Value = WXStation.WXData.WindSpeed.Value;
                        WXStation.WXData.WindPeakGust.TimeOfValue = WXStation.WXData.WindSpeed.TimeOfValue;
                    }
                }
                //WindChill
                if (WXStation.WXData.WindChill.autoCalculate && (WXStation.WXData.WindSpeed.Value != DataErrorVal && WXStation.WXData.TempOutCur.AsEnglish != DataErrorVal))
                {
                    if (WXStation.WXData.TempOutCur.AsEnglish <= 50)
                    {
                        WXStation.WXData.WindChill.Value = Conversion.WindChill_Fahenheight(WXStation.WXData.TempOutCur.AsEnglish, WXStation.WXData.WindSpeed.Value);
                    }
                    else
                    {
                        WXStation.WXData.WindChill.Value = WXStation.WXData.TempOutCur.Value;
                    }
                    WXStation.WXData.WindChill.TimeOfValue = WXStation.WXData.WindSpeed.TimeOfValue;
                }

                //Heat Index, DewPoint and CloudBase
                if (Convert.ToDouble(WXStation.WXData.HumidityOutCur.Value) != DataErrorVal & WXStation.WXData.TempOutCur.Value != DataErrorVal)
                {
                    if (WXStation.WXData.HeatIndex.autoCalculate)
                    {
                        if (WXStation.WXData.TempOutCur.AsEnglish >= 80)
                        {
                            WXStation.WXData.HeatIndex.Value = Conversion.HeatIndex(DataStation.WXData.TempOutCur.AsEnglish, Convert.ToDouble(DataStation.WXData.HumidityOutCur.Value));
                        }
                        else
                        {
                            WXStation.WXData.HeatIndex.Value = WXStation.WXData.TempOutCur.Value;
                        }
                        WXStation.WXData.HeatIndex.TimeOfValue = WXStation.WXData.TempOutCur.TimeOfValue;
                    }

                    if (WXStation.WXData.DewPoint.autoCalculate)
                    {
                        WXStation.WXData.DewPoint.Value = Conversion.DewPoint_Fahenheight(WXStation.WXData.TempOutCur.AsMetric, Convert.ToDouble(WXStation.WXData.HumidityOutCur.Value));
                    }

                    if (WXStation.WXData.CloudBase.autoCalculate)
                    {
                        WXStation.WXData.CloudBase.Value = Conversion.CloudBase(WXStation.WXData.TempOutCur.Value, WXStation.WXData.DewPoint.Value);
                    }
                }
                if (WXStation.WXData.DensityAltitude.autoCalculate && (WXStation.WXData.TempOutCur.Value != DataErrorVal & WXStation.WXData.BarometerCur.Value != DataErrorVal))
                {
                    WXStation.WXData.DensityAltitude.Value = Conversion.Noaa_DensityAltitude_Feet(WXStation.WXData.TempOutCur.AsMetric, WXStation.WXData.BarometerCur.AsEnglish, WXStation.WXData.DewPoint.AsMetric);
                }

                if (WXStation.WXData.ActiveWeather.autoCalculate && (WXStation.WXData.WindSpeed.Value != DataErrorVal & WXStation.WXData.RainRateAnHr.Value != DataErrorVal))
                {
                    WXStation.WXData.ActiveWeather.Value = Conversion.Active_Weather(WXStation.WXData.WindSpeed.AsEnglish, WXStation.WXData.RainRateAnHr.AsEnglish);

                }
                if (WXStation.WXData.SkyConditions.autoCalculate && (WXStation.WXData.TempOutCur.Value != DataErrorVal & WXStation.WXData.BarometerCur.Value != DataErrorVal & Convert.ToDouble(WXStation.WXData.HumidityOutCur.Value) != DataErrorVal & WXStation.WXData.RainRateAnHr.Value != DataErrorVal & WXStation.WXData.DewPoint.Value != DataErrorVal & WXStation.WXData.CloudBase.Value != DataErrorVal & WXStation.WXData.DensityAltitude.Value != DataErrorVal))
                {
                    WXStation.WXData.SkyConditions.Value = Conversion.Sky_Conditions(WXStation.WXData.TempOutCur.AsEnglish, WXStation.WXData.BarometerCur.AsEnglish, Convert.ToDouble(WXStation.WXData.HumidityOutCur.Value), WXStation.WXData.RainRateAnHr.Value, WXStation.WXData.DewPoint.AsEnglish, WXStation.WXData.CloudBase.AsEnglish, WXStation.WXData.DensityAltitude.AsEnglish);
                }

            }

            private Temperature UpdateHighTemp(Temperature curVal, Temperature HighVal)
            {
                Temperature tempval = HighVal;
                if (curVal.Value != DataErrorVal)
                {

                    if (HighVal.Value == DataErrorVal | HighVal.TimeOfValue.Date != curVal.TimeOfValue.Date)
                    {
                        tempval.Value = curVal.Value;
                        tempval.TimeOfValue = curVal.TimeOfValue;
                    }
                    else if (curVal.Value >= HighVal.Value)
                    {
                        tempval.Value = curVal.Value;
                        tempval.TimeOfValue = curVal.TimeOfValue;
                    }
                }
                return tempval;
            }
            private Temperature UpdateLowTemp(Temperature curVal, Temperature LowVal)
            {
                Temperature tempval = LowVal;
                if (curVal.Value != DataErrorVal)
                {
                    if (LowVal.Value == DataErrorVal | LowVal.TimeOfValue.Date != curVal.TimeOfValue.Date)
                    {
                        tempval.Value = curVal.Value;
                        tempval.TimeOfValue = curVal.TimeOfValue;
                    }
                    else if (curVal.Value <= LowVal.Value)
                    {
                        tempval.Value = curVal.Value;
                        tempval.TimeOfValue = curVal.TimeOfValue;
                    }
                }
                return tempval;
            }
            private BarometricPressure UpdateHighBarometer(BarometricPressure curVal, BarometricPressure HighVal)
            {
                BarometricPressure TempBar = HighVal;
                if (curVal.Value != DataErrorVal)
                {
                    if (HighVal.Value == DataErrorVal | HighVal.TimeOfValue.Date != curVal.TimeOfValue.Date)
                    {
                        TempBar.Value = curVal.Value;
                        TempBar.TimeOfValue = curVal.TimeOfValue;
                    }
                    else if (curVal.Value >= HighVal.Value)
                    {
                        TempBar.Value = curVal.Value;
                        TempBar.TimeOfValue = curVal.TimeOfValue;
                    }
                }
                return TempBar;
            }
            private BarometricPressure UpdateLowBarometer(BarometricPressure curVal, BarometricPressure LowVal)
            {
                BarometricPressure TempBar = LowVal;
                if (curVal.Value != DataErrorVal)
                {
                    if (LowVal.Value == DataErrorVal | LowVal.TimeOfValue.Date != curVal.TimeOfValue.Date)
                    {
                        TempBar.Value = curVal.Value;
                        TempBar.TimeOfValue = curVal.TimeOfValue;
                    }
                    else if (curVal.Value <= LowVal.Value)
                    {
                        TempBar.Value = curVal.Value;
                        TempBar.TimeOfValue = curVal.TimeOfValue;
                    }
                }
                return TempBar;
            }
        }

        #region Debug Messaging System
        interface IMessengerHost
        {
            void SendMessage(string Sender, string Message, string Reason);
        }
        public class DebugMessenger : IMessengerHost
        {
            public class DebugMessage
            {
                public DateTime TimeStamp { get; set; }
                public string Sender { get; set; }
                public string Message { get; set; }
                public string Reason { get; set; }
                public DebugMessage(DateTime _timeStamp, string _sender, string _message, string _reason)
                {
                    TimeStamp = _timeStamp;
                    Sender = _sender;
                    Message = _message;
                    Reason = _reason;
                }
            }
            public static CustomObserableCollection.DispatcherNotifiedObservableCollection<DebugMessage> _Messages = new CustomObserableCollection.DispatcherNotifiedObservableCollection<DebugMessage>();
            public void SendMessage(string Sender, string Message, string Reason)
            {
                int MSGCount = System.Convert.ToInt32(_Messages.Count - 1);
                if (MSGCount > 50)
                {
                    _Messages.RemoveAt(MSGCount - 1);
                }
                //_Messages.Insert(0, Message)
                _Messages.Insert(0, new DebugMessage(DateTime.Now, Sender, Message, Reason));
            }
            public CustomObserableCollection.DispatcherNotifiedObservableCollection<DebugMessage> Messages
            {
                get
                {
                    return _Messages;
                }
                set
                {
                    _Messages = value;
                }
            }

        }
        #endregion
        #region View Plugins
        public interface IView
        {
            ObservableCollection<Config> Configuration { get; set; }
            void SelectedStationChanged(int SelectedStaitonIndex);
            void Intilize(ObservableCollection<Station> Stations, ref IViewHost StationManager, ref ConfigManager.GlobalConfig GlobalSettings);
            string ViewName { get; set; }
        }
        public interface IViewHost
        {
            List<string> AvailableStationTypes();
            int AddStation(string Station_Type, string StationName, bool IsNew);
            int RemoveStation(string StationName);
        }
        #endregion
        #region Background Plugins
        public interface IBackgroundPlugin
        {
            string Name { get; set; }
            int UpdateInterval { get; set; }
            string ManualUpdateText { get; }
            bool AllowsManualUpdate { get; }
            ObservableCollection<Config> Configuration { get; set; }
            void Initilize(ObservableCollection<Station> Stations);
            void Update();
            void Close();
        }
        #endregion
        #region Settings Managment
        public class ConfigManager
        {
            public class GlobalConfig
            {
                public ObservableCollection<Config> GlobalSettings;

                public enum configIndexes
                {
                    Temp,
                    Wind,
                    Bar,
                    Rain,
                    Length
                }
            }
            public static GlobalConfig _globalConfig = new GlobalConfig();
            public static Config _current_configObj;
            public static void intilizeGlobalConfig()
            {
                _globalConfig.GlobalSettings = new ObservableCollection<Config>();
                _globalConfig.GlobalSettings.Add(new Config("Units"));
                _globalConfig.GlobalSettings[0].ConfigSettings.Add(new Setting("Temperature", System.Convert.ToString(DataUnitType.Temp_Fahrenheit.ToString())));
                _globalConfig.GlobalSettings[0].ConfigSettings[System.Convert.ToInt32(GlobalConfig.configIndexes.Temp)].AllowedParameters = new List<string> { DataUnitType.Temp_Fahrenheit.ToString(), DataUnitType.Temp_Celsius.ToString() };
                _globalConfig.GlobalSettings[0].ConfigSettings.Add(new Setting("Wind Speed", System.Convert.ToString(DataUnitType.Wind_Mph.ToString())));
                _globalConfig.GlobalSettings[0].ConfigSettings[System.Convert.ToInt32(GlobalConfig.configIndexes.Wind)].AllowedParameters = new List<string> { DataUnitType.Wind_Mph.ToString(), DataUnitType.Wind_Kph.ToString() };
                _globalConfig.GlobalSettings[0].ConfigSettings.Add(new Setting("Barometer", System.Convert.ToString(DataUnitType.Bar_InHg.ToString())));
                _globalConfig.GlobalSettings[0].ConfigSettings[System.Convert.ToInt32(GlobalConfig.configIndexes.Bar)].AllowedParameters = new List<string> { DataUnitType.Bar_InHg.ToString(), DataUnitType.Bar_mbar.ToString(), DataUnitType.Bar_hpa.ToString() };
                _globalConfig.GlobalSettings[0].ConfigSettings.Add(new Setting("Rain", System.Convert.ToString(DataUnitType.Precip_Inch.ToString())));
                _globalConfig.GlobalSettings[0].ConfigSettings[System.Convert.ToInt32(GlobalConfig.configIndexes.Rain)].AllowedParameters = new List<string> { DataUnitType.Precip_Inch.ToString(), DataUnitType.Precip_Millimeter.ToString() };
                _globalConfig.GlobalSettings[0].ConfigSettings.Add(new Setting("Length", System.Convert.ToString(DataUnitType.Other_Feet.ToString())));
                _globalConfig.GlobalSettings[0].ConfigSettings[System.Convert.ToInt32(GlobalConfig.configIndexes.Length)].AllowedParameters = new List<string> { DataUnitType.Other_Feet.ToString(), DataUnitType.Other_Meter.ToString() };
            }
            public static void SaveSettings(string FilePath, ObservableCollection<Config> Config_Object)
            {
                if (Config_Object != null)
                {
                    FileSystem.FileOpen(1, FilePath, OpenMode.Output, OpenAccess.Write, OpenShare.LockReadWrite, -1);

                    //Write The Settings
                    foreach (Config ConfigObj in Config_Object)
                    {

                        FileSystem.WriteLine(1, "Settings Group", ConfigObj.ConfigGroupName);
                        for (var i = 0; i <= ConfigObj.ConfigSettings.Count - 1; i++)
                        {
                            FileSystem.WriteLine(1, ConfigObj.ConfigSettings[i].Key, ConfigObj.ConfigSettings[i].CurrentValue);
                        }
                        FileSystem.WriteLine(1, "End Settings Group", ConfigObj.ConfigGroupName);
                    }
                    FileSystem.FileClose(1);
                }
            }

            public static string LoadSettings(string FilePath, ref ObservableCollection<Config> Config_Object)
            {
                if ((new Microsoft.VisualBasic.Devices.ServerComputer()).FileSystem.FileExists(FilePath))
                {
                    int CurrentConfigGroupNum = 0;
                    FileSystem.FileOpen(1, FilePath, OpenMode.Input, OpenAccess.Read, OpenShare.LockReadWrite, -1);
                    string LineTitle = "";
                    string LineData = "";

                    do
                    {
                        if (!(FileSystem.EOF(1)))
                        {
                            FileSystem.Input(1, ref LineTitle);
                            FileSystem.Input(1, ref LineData);
                            switch (LineTitle)
                            {
                                case "Station Name":
                                    break;
                                case "Station Type":
                                    break;
                                case "Settings Group":
                                    _current_configObj = Config_Object[CurrentConfigGroupNum];
                                    ReadSettingsGroup();
                                    CurrentConfigGroupNum++;
                                    break;
                                default:
                                    break;

                            }
                        }
                    } while (!(FileSystem.EOF(1)));

                    FileSystem.FileClose(1);
                    return string.Format("Settings Loaded.");
                }
                else
                {
                    return string.Format("File Does Not Exist -- {0}", FilePath);
                }
            }
            private static void SetSetting(string SettingKey, string SettingVal, Config Config_Object)
            {
                for (var i = 0; i <= Config_Object.ConfigSettings.Count - 1; i++)
                {
                    if (Config_Object.ConfigSettings[i].Key == SettingKey)
                    {
                        Config_Object.ConfigSettings[i].CurrentValue = SettingVal;
                        break;
                    }
                }
            }

            private static void ReadSettingsGroup()
            {
                bool FoundEndOfSettingGroup = false;
                string LineTitle = "";
                string LineData = "";
                do
                {
                    FileSystem.Input(1, ref LineTitle);
                    FileSystem.Input(1, ref LineData);
                    if (LineTitle != "End Settings Group")
                    {
                        SetSetting(LineTitle, LineData, _current_configObj);
                    }
                    else
                    {
                        FoundEndOfSettingGroup = true;
                    }
                } while (!FileSystem.EOF(1) && !FoundEndOfSettingGroup);
            }
        }

        public class Config
        {
            public string ConfigGroupName { get; set; }
            public List<Setting> ConfigSettings { get; set; }
            public Config(string GroupName)
            {
                // VBConversions Note: Non-static class variable initialization is below.  Class variables cannot be initially assigned non-static values in C#.
                ConfigSettings = new List<Setting>();

                ConfigGroupName = GroupName;
                ConfigSettings = new List<Setting>();
            }
        }
        public class Setting : INotifyPropertyChanged
        {
            private string _cValue;
            public string Key { get; set; }
            public string DefaultVal { get; set; }
            public string CurrentValue
            {
                get
                {
                    return _cValue;
                }
                set
                {
                    _cValue = value;
                    OnPropertyChanged("CurrentValue");
                }
            }
            public List<string> AllowedParameters { get; set; }
            public Setting(string Keyval, string DefaultValue)
            {
                // VBConversions Note: Non-static class variable initialization is below.  Class variables cannot be initially assigned non-static values in C#.
                AllowedParameters = new List<string>();

                Key = Keyval;
                DefaultVal = DefaultValue;
                CurrentValue = DefaultValue;
            }

            public event PropertyChangedEventHandler PropertyChanged;
            public void OnPropertyChanged(string propertyName)
            {
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion
        public class Lat_Long : INotifyPropertyChanged
        {
            private string _latitudie;
            private string _longitude;
            public string Latitude
            {
                get
                {
                    return _latitudie;
                }
                set
                {
                    _latitudie = value;
                    OnPropertyChanged("Latitude");
                }
            }
            public string Longitude
            {
                get
                {
                    return _longitude;
                }
                set
                {
                    _longitude = value;
                    OnPropertyChanged("Longitude");
                }
            }
            public Lat_Long(string _lat, string _long)
            {
                _latitudie = _lat;
                _longitude = _long;
            }
            #region PropertyChanged Members
            public event PropertyChangedEventHandler PropertyChanged;
            public void OnPropertyChanged(string propertyName)
            {
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
            #endregion
        }
        public class LatLong
        {
            //Implements INotifyPropertyChanged
            private Lat_Long _location;
            public LatLong(string Latitude, string Longitude)
            {
                _location = new Lat_Long(Latitude, Longitude);
            }
            public Lat_Long DecimalDegrees
            {
                get
                {
                    return _location;
                }
                set
                {
                    _location = value;
                    //OnPropertyChanged("Location")
                }
            }
            public Lat_Long LORAN
            {
                get
                {
                    return new Lat_Long(Conversion.DecimalDegrees_To_LORAN_DecimalDegreeMinutes((double)(decimal.Parse(_location.Latitude)), true), Conversion.DecimalDegrees_To_LORAN_DecimalDegreeMinutes((double)(decimal.Parse(_location.Latitude)), false));
                }
            }
            public Lat_Long DegreesMinutesSeconds
            {
                get
                {
                    return new Lat_Long(string.Join(" ", Conversion.DecimalDegrees_To_DegreesMinutesSeconds(_location.Latitude)), string.Join(" ", Conversion.DecimalDegrees_To_DegreesMinutesSeconds(_location.Longitude)));
                }
                set
                {
                    _location = new Lat_Long(Conversion.DegreesMinutesSeconds_To_DecimalDegrees(value.Latitude.Split(' ')), Conversion.DegreesMinutesSeconds_To_DecimalDegrees(value.Longitude.Split(' ')));
                    //OnPropertyChanged("DegreesMinutesSeconds")
                }
            }
            //#Region "PropertyChanged Members"
            //        Public Event PropertyChanged(sender As Object, e As PropertyChangedEventArgs) Implements INotifyPropertyChanged.PropertyChanged
            //        Public Sub OnPropertyChanged(ByVal propertyName As String)
            //            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
            //        End Sub
            //#End Region
        }

        public class Temperature
        {
            private DataUnitType _englishUnitType = DataUnitType.Temp_Fahrenheit;
            private DataUnitType _metricUnitType = DataUnitType.Temp_Celsius;
            private double _value;
            private DateTime _timeOfVal;
            private string _title;
            private Boolean _AutoCalulate;
            private DataUnitType _curUnitType; // VBConversions Note: Initial value cannot be assigned here since it is non-static.  Assignment has been moved to the class constructors.
            private double _asOther;

            public double ToEnglish()
            {
                if (_value != DataErrorVal)
                {
                    if (UnitType == (_metricUnitType).ToString())
                    {
                        return Conversion.Celsius_To_Fahrenheight(_value);
                    }
                    else
                    {
                        return _value;
                    }
                }
                else
                {
                    return _value;
                }
            }

            public double ToMetric()
            {
                if (_value != DataErrorVal)
                {
                    if (UnitType == (_englishUnitType).ToString())
                    {
                        return Conversion.Fahrenheight_To_Celsius(_value);
                    }
                    else
                    {
                        return _value;
                    }
                }
                else
                {
                    return _value;
                }
            }

            public double Value //Implements IValue.Value
            {
                get
                {
                    return _value;
                }
                set
                {
                    _value = value;
                    AsEnglish = value;
                    AsMetric = value;
                    //OnPropertyChanged("Value")
                }
            }

            public string UnitType //Implements IValue.UnitType
            {
                get
                {
                    return (_curUnitType).ToString();
                }
                set
                {
                    _curUnitType = (DataUnitType)(int.Parse(value));
                    //OnPropertyChanged("UnitType")
                }
            }

            public DataUnitType EnglishUnitType //Implements IValue.EnglishUnitType
            {
                get
                {
                    return _englishUnitType;
                }
            }

            public DataUnitType MetricUnitType //Implements IValue.MetricUnitType
            {
                get
                {
                    return _metricUnitType;
                }
            }
            public string Title //Implements IValue.Title
            {
                get
                {
                    return _title;
                }
                set
                {
                    _title = value;
                    //OnPropertyChanged("Title")
                }
            }
            public Boolean autoCalculate
            {
                get
                {
                    return _AutoCalulate;
                }
                set
                {
                    _AutoCalulate = value;
                }
            }

            public Temperature(string DataTitle, DataUnitType UnitType, Boolean AutoCalculate = true)
            {
                // VBConversions Note: Non-static class variable initialization is below.  Class variables cannot be initially assigned non-static values in C#.
                _curUnitType = _englishUnitType;
                Title = DataTitle;
                _curUnitType = UnitType;
                Value = DataErrorVal;
                autoCalculate = AutoCalculate;
            }

            public DateTime TimeOfValue // Implements IValue.TimeOfValue
            {
                get
                {
                    return _timeOfVal;
                }
                set
                {
                    _timeOfVal = value;
                    //OnPropertyChanged("TimeOfValue")
                }
            }

            public double AsEnglish //Implements IValue.AsEnglish
            {
                get
                {
                    if (_curUnitType == DataUnitType.Temp_Fahrenheit)
                    {
                        return _value;
                    }
                    else
                    {
                        return _asOther;
                    }
                }
                set
                {
                    if (value != DataErrorVal)
                    {
                        if (_curUnitType == _englishUnitType)
                        {
                            _asOther = Conversion.Celsius_To_Fahrenheight(value);
                        }
                        else if (_curUnitType == _metricUnitType)
                        {
                            _asOther = Conversion.Fahrenheight_To_Celsius(value);
                        }
                    }
                    else
                    {
                        _asOther = value;
                    }
                    //OnPropertyChanged("AsEnglish")
                }
            }

            public double AsMetric //Implements IValue.AsMetric
            {
                get
                {
                    if (_curUnitType == DataUnitType.Temp_Celsius)
                    {
                        return _value;
                    }
                    else
                    {
                        return _asOther;
                    }
                }
                set
                {
                    if (value != DataErrorVal)
                    {
                        if (_curUnitType == _metricUnitType)
                        {
                            _asOther = Conversion.Celsius_To_Fahrenheight(value);
                        }
                        else if (_curUnitType == _englishUnitType)
                        {
                            _asOther = Conversion.Fahrenheight_To_Celsius(value);
                        }
                    }
                    else
                    {
                        _asOther = value;
                    }
                    //OnPropertyChanged("AsMetric")
                }
            }
        }
        public class Wind
        {
            private DataUnitType _englishUnitType = DataUnitType.Wind_Mph;
            private DataUnitType _metricUnitType = DataUnitType.Wind_Kph;
            private double _value;
            private DateTime _timeOfVal;
            private string _title;
            private Boolean _AutoCalulate;

            private DataUnitType _curUnitType; // VBConversions Note: Initial value cannot be assigned here since it is non-static.  Assignment has been moved to the class constructors.
            private double _asOther;
            public double ToEnglish()
            {
                if (_value != DataErrorVal)
                {
                    if (UnitType == (_metricUnitType).ToString())
                    {
                        return Conversion.Kph_To_Mph(_value);
                    }
                    else
                    {
                        return _value;
                    }
                }
                else
                {
                    return _value;
                }
            }

            public double ToMetric()
            {
                if (_value != DataErrorVal)
                {
                    if (UnitType == (_englishUnitType).ToString())
                    {
                        return Conversion.Mph_To_Kph(_value);
                    }
                    else
                    {
                        return _value;
                    }
                }
                else
                {
                    return _value;
                }
            }

            public double Value //Implements IValue.Value
            {
                get
                {
                    return _value;
                }
                set
                {
                    _value = value;
                    AsEnglish = value;
                    AsMetric = value;
                    //OnPropertyChanged("Value")
                }
            }

            public string UnitType //Implements IValue.UnitType
            {
                get
                {
                    return (_curUnitType).ToString();
                }
                set
                {
                    _curUnitType = (DataUnitType)(int.Parse(value));
                    //OnPropertyChanged("UnitType")
                }
            }

            public DataUnitType EnglishUnitType //Implements IValue.EnglishUnitType
            {
                get
                {
                    return _englishUnitType;
                }
            }

            public DataUnitType MetricUnitType //Implements IValue.MetricUnitType
            {
                get
                {
                    return _metricUnitType;
                }
            }
            public string Title //Implements IValue.Title
            {
                get
                {
                    return _title;
                }
                set
                {
                    _title = value;
                    //OnPropertyChanged("Title")
                }
            }
            public Boolean autoCalculate
            {
                get
                {
                    return _AutoCalulate;
                }
                set
                {
                    _AutoCalulate = value;
                }
            }
            public Wind(string DataTitle, DataUnitType UnitType, Boolean AutoCalculate = true)
            {
                // VBConversions Note: Non-static class variable initialization is below.  Class variables cannot be initially assigned non-static values in C#.
                _curUnitType = _englishUnitType;

                Title = DataTitle;
                _curUnitType = UnitType;
                Value = DataErrorVal;
                autoCalculate = AutoCalculate;
            }

            public DateTime TimeOfValue //Implements IValue.TimeOfValue
            {
                get
                {
                    return _timeOfVal;
                }
                set
                {
                    _timeOfVal = value;
                    //OnPropertyChanged("TimeOfValue")
                }
            }
            public double AsEnglish //Implements IValue.AsEnglish
            {
                get
                {
                    if (_curUnitType == DataUnitType.Wind_Mph)
                    {
                        return _value;
                    }
                    else
                    {
                        return _asOther;
                    }
                }
                set
                {
                    if (value != DataErrorVal)
                    {
                        if (_curUnitType == _englishUnitType)
                        {
                            _asOther = Conversion.Mph_To_Kph(value);
                        }
                        else if (_curUnitType == _metricUnitType)
                        {
                            _asOther = Conversion.Kph_To_Mph(value);
                        }
                    }
                    else
                    {
                        _asOther = value;
                    }
                    //OnPropertyChanged("AsEnglish")
                }
            }

            public double AsMetric //Implements IValue.AsMetric
            {
                get
                {
                    if (_curUnitType == _metricUnitType)
                    {
                        return _value;
                    }
                    else
                    {
                        return _asOther;
                    }
                }
                set
                {
                    if (value != DataErrorVal)
                    {
                        if (_curUnitType == _metricUnitType)
                        {
                            _asOther = Conversion.Kph_To_Mph(value);
                        }
                        else if (_curUnitType == _englishUnitType)
                        {
                            _asOther = Conversion.Mph_To_Kph(value);
                        }
                    }
                    else
                    {
                        _asOther = value;
                    }
                    //OnPropertyChanged("AsMetric")
                }
            }
        }
        public class BarometricPressure
        {
            private DataUnitType _englishUnitType = DataUnitType.Bar_InHg;
            private DataUnitType _metricUnitType = DataUnitType.Bar_mbar;
            private double _value;
            private DateTime _timeOfVal;
            private string _title;
            private Boolean _AutoCalulate;

            private DataUnitType _curUnitType; // VBConversions Note: Initial value cannot be assigned here since it is non-static.  Assignment has been moved to the class constructors.
            private double _asOther;
            public double ToEnglish()
            {
                if (_value != DataErrorVal)
                {
                    if (UnitType == (_metricUnitType).ToString())
                    {
                        return Conversion.mBar_to_inHg(_value);
                    }
                    else
                    {
                        return _value;
                    }
                }
                else
                {
                    return _value;
                }
            }

            public double ToMetric()
            {
                if (_value != DataErrorVal)
                {
                    if (UnitType == (_englishUnitType).ToString())
                    {
                        return Conversion.inHg_to_mBar(_value);
                    }
                    else
                    {
                        return _value;
                    }
                }
                else
                {
                    return _value;
                }
            }

            public double Value //Implements IValue.Value
            {
                get
                {
                    return _value;
                }
                set
                {
                    _value = value;
                    AsEnglish = value;
                    AsMetric = value;
                    //OnPropertyChanged("Value")
                }
            }

            public string UnitType //Implements IValue.UnitType
            {
                get
                {
                    return (_curUnitType).ToString();
                }
                set
                {
                    _curUnitType = (DataUnitType)(int.Parse(value));
                    //OnPropertyChanged("UnitType")
                }
            }

            public DataUnitType EnglishUnitType //Implements IValue.EnglishUnitType
            {
                get
                {
                    return _englishUnitType;
                }
            }

            public DataUnitType MetricUnitType //Implements IValue.MetricUnitType
            {
                get
                {
                    return _metricUnitType;
                }
            }
            public string Title //Implements IValue.Title
            {
                get
                {
                    return _title;
                }
                set
                {
                    _title = value;
                    //OnPropertyChanged("Title")
                }
            }
            public Boolean autoCalculate
            {
                get
                {
                    return _AutoCalulate;
                }
                set
                {
                    _AutoCalulate = value;
                }
            }
            public BarometricPressure(string DataTitle, DataUnitType UnitType, Boolean AutoCalculate = true)
            {
                // VBConversions Note: Non-static class variable initialization is below.  Class variables cannot be initially assigned non-static values in C#.
                _curUnitType = _englishUnitType;

                Title = DataTitle;
                _curUnitType = UnitType;
                Value = DataErrorVal;
                autoCalculate = AutoCalculate;
            }

            public DateTime TimeOfValue //Implements IValue.TimeOfValue
            {
                get
                {
                    return _timeOfVal;
                }
                set
                {
                    _timeOfVal = value;
                    //OnPropertyChanged("TimeOfValue")
                }
            }

            public double AsEnglish //Implements IValue.AsEnglish
            {
                get
                {
                    if (_curUnitType == _englishUnitType)
                    {
                        return _value;
                    }
                    else
                    {
                        return _asOther;
                    }
                }
                set
                {
                    if (value != DataErrorVal)
                    {
                        if (_curUnitType == _englishUnitType)
                        {
                            _asOther = Conversion.inHg_to_mBar(value);
                        }
                        else if (_curUnitType == _metricUnitType)
                        {
                            _asOther = Conversion.mBar_to_inHg(value);
                        }
                    }
                    else
                    {
                        _asOther = value;
                    }
                    //OnPropertyChanged("AsEnglish")
                }
            }

            public double AsMetric //Implements IValue.AsMetric
            {
                get
                {
                    if (_curUnitType == _metricUnitType)
                    {
                        return _value;
                    }
                    else
                    {
                        return _asOther;
                    }
                }
                set
                {
                    if (value != DataErrorVal)
                    {
                        if (_curUnitType == _metricUnitType)
                        {
                            _asOther = Conversion.mBar_to_inHg(value);
                        }
                        else if (_curUnitType == _englishUnitType)
                        {
                            _asOther = Conversion.inHg_to_mBar(value);
                        }
                    }
                    else
                    {
                        _asOther = value;
                    }
                    //OnPropertyChanged("AsMetric")
                }
            }
        }
        public class Precipitation
        {
            private DataUnitType _englishUnitType = DataUnitType.Precip_Inch;
            private DataUnitType _metricUnitType = DataUnitType.Precip_Millimeter;
            private double _value;
            private DateTime _timeOfVal;
            private string _title;
            private Boolean _AutoCalulate;

            private DataUnitType _curUnitType; // VBConversions Note: Initial value cannot be assigned here since it is non-static.  Assignment has been moved to the class constructors.
            private double _asOther;
            public double ToEnglish()
            {
                if (_value != DataErrorVal)
                {
                    if (UnitType == (_metricUnitType).ToString())
                    {
                        return Conversion.mm_to_in(_value);
                    }
                    else
                    {
                        return _value;
                    }
                }
                else
                {
                    return _value;
                }
            }

            public double ToMetric()
            {
                if (_value != DataErrorVal)
                {
                    if (UnitType == (_englishUnitType).ToString())
                    {
                        return Conversion.in_To_mm(_value);
                    }
                    else
                    {
                        return _value;
                    }
                }
                else
                {
                    return _value;
                }
            }

            public double Value //Implements IValue.Value
            {
                get
                {
                    return _value;
                }
                set
                {
                    _value = value;
                    AsEnglish = value;
                    AsMetric = value;
                    //OnPropertyChanged("Value")
                }
            }

            public string UnitType //Implements IValue.UnitType
            {
                get
                {
                    return (_curUnitType).ToString();
                }
                set
                {
                    _curUnitType = (DataUnitType)(int.Parse(value));
                    //OnPropertyChanged("UnitType")
                }
            }

            public DataUnitType EnglishUnitType //Implements IValue.EnglishUnitType
            {
                get
                {
                    return _englishUnitType;
                }
            }

            public DataUnitType MetricUnitType //Implements IValue.MetricUnitType
            {
                get
                {
                    return _metricUnitType;
                }
            }
            public string Title //Implements IValue.Title
            {
                get
                {
                    return _title;
                }
                set
                {
                    _title = value;
                    //OnPropertyChanged("Title")
                }
            }
            public Boolean autoCalculate
            {
                get
                {
                    return _AutoCalulate;
                }
                set
                {
                    _AutoCalulate = value;
                }
            }
            public Precipitation(string DataTitle, DataUnitType UnitType, Boolean AutoCalculate = true)
            {
                // VBConversions Note: Non-static class variable initialization is below.  Class variables cannot be initially assigned non-static values in C#.
                _curUnitType = _englishUnitType;

                Title = DataTitle;
                _curUnitType = UnitType;
                Value = DataErrorVal;
                autoCalculate = AutoCalculate;
            }

            public DateTime TimeOfValue //Implements IValue.TimeOfValue
            {
                get
                {
                    return _timeOfVal;
                }
                set
                {
                    _timeOfVal = value;
                    //OnPropertyChanged("TimeOfValue")
                }
            }

            public double AsEnglish //Implements IValue.AsEnglish
            {
                get
                {
                    if (_curUnitType == _englishUnitType)
                    {
                        return _value;
                    }
                    else
                    {
                        return _asOther;
                    }
                }
                set
                {
                    if (value != DataErrorVal)
                    {
                        if (_curUnitType == _englishUnitType)
                        {
                            _asOther = Conversion.in_To_mm(value);
                        }
                        else if (_curUnitType == _metricUnitType)
                        {
                            _asOther = Conversion.mm_to_in(value);
                        }
                    }
                    else
                    {
                        _asOther = value;
                    }
                    //OnPropertyChanged("AsEnglish")
                }
            }

            public double AsMetric //Implements IValue.AsMetric
            {
                get
                {
                    if (_curUnitType == _metricUnitType)
                    {
                        return _value;
                    }
                    else
                    {
                        return _asOther;
                    }
                }
                set
                {
                    if (value != DataErrorVal)
                    {
                        if (_curUnitType == _englishUnitType)
                        {
                            _asOther = Conversion.in_To_mm(value);
                        }
                        else if (_curUnitType == _metricUnitType)
                        {
                            _asOther = Conversion.mm_to_in(value);
                        }
                    }
                    else
                    {
                        _asOther = value;
                    }
                    //OnPropertyChanged("AsMetric")
                }
            }
        }
        public class Length
        {
            private DataUnitType _englishUnitType = DataUnitType.Other_Feet;
            private DataUnitType _metricUnitType = DataUnitType.Other_Meter;
            private double _value;
            private DateTime _timeOfVal;
            private string _title;
            private Boolean _AutoCalulate;

            private DataUnitType _curUnitType; // VBConversions Note: Initial value cannot be assigned here since it is non-static.  Assignment has been moved to the class constructors.
            private double _asOther;
            public double ToEnglish()
            {
                if (_value != DataErrorVal)
                {
                    if (UnitType == (_metricUnitType).ToString())
                    {
                        return Conversion.Meters_To_Feet(_value);
                    }
                    else
                    {
                        return _value;
                    }
                }
                else
                {
                    return _value;
                }
            }

            public double ToMetric()
            {
                if (_value != DataErrorVal)
                {
                    if (UnitType == (_englishUnitType).ToString())
                    {
                        return Conversion.Feet_To_Meters(_value);
                    }
                    else
                    {
                        return _value;
                    }
                }
                else
                {
                    return _value;
                }
            }

            public double Value //Implements IValue.Value
            {
                get
                {
                    return _value;
                }
                set
                {
                    _value = value;
                    AsEnglish = value;
                    AsMetric = value;
                    //OnPropertyChanged("Value")
                }
            }

            public string UnitType //Implements IValue.UnitType
            {
                get
                {
                    return (_curUnitType).ToString();
                }
                set
                {
                    _curUnitType = (DataUnitType)(int.Parse(value));
                    //OnPropertyChanged("UnitType")
                }
            }

            public DataUnitType EnglishUnitType //Implements IValue.EnglishUnitType
            {
                get
                {
                    return _englishUnitType;
                }
            }

            public DataUnitType MetricUnitType //Implements IValue.MetricUnitType
            {
                get
                {
                    return _metricUnitType;
                }
            }
            public string Title //Implements IValue.Title
            {
                get
                {
                    return _title;
                }
                set
                {
                    _title = value;
                    //OnPropertyChanged("Title")
                }
            }
            public Boolean autoCalculate
            {
                get
                {
                    return _AutoCalulate;
                }
                set
                {
                    _AutoCalulate = value;
                }
            }

            public Length(string DataTitle, DataUnitType UnitType, Boolean AutoCalculate = true)
            {
                // VBConversions Note: Non-static class variable initialization is below.  Class variables cannot be initially assigned non-static values in C#.
                _curUnitType = _englishUnitType;

                Title = DataTitle;
                _curUnitType = UnitType;
                Value = DataErrorVal;
                autoCalculate = AutoCalculate;
            }

            public DateTime TimeOfValue //Implements IValue.TimeOfValue
            {
                get
                {
                    return _timeOfVal;
                }
                set
                {
                    _timeOfVal = value;
                    //OnPropertyChanged("TimeOfValue")
                }
            }

            public double AsEnglish //Implements IValue.AsEnglish
            {
                get
                {
                    if (_curUnitType == _englishUnitType)
                    {
                        return _value;
                    }
                    else
                    {
                        return _asOther;
                    }
                }
                set
                {
                    if (value != DataErrorVal)
                    {
                        if (_curUnitType == _englishUnitType)
                        {
                            _asOther = Conversion.Feet_To_Meters(value);
                        }
                        else if (_curUnitType == _metricUnitType)
                        {
                            _asOther = Conversion.Meters_To_Feet(value);
                        }
                    }
                    else
                    {
                        _asOther = value;
                    }
                    //OnPropertyChanged("AsEnglish")
                }
            }

            public double AsMetric //Implements IValue.AsMetric
            {
                get
                {
                    if (_curUnitType == _metricUnitType)
                    {
                        return _value;
                    }
                    else
                    {
                        return _asOther;
                    }
                }
                set
                {
                    if (value != DataErrorVal)
                    {
                        if (_curUnitType == _englishUnitType)
                        {
                            _asOther = Conversion.Feet_To_Meters(value);
                        }
                        else if (_curUnitType == _metricUnitType)
                        {
                            _asOther = Conversion.Meters_To_Feet(value);
                        }
                    }
                    else
                    {
                        _asOther = value;
                    }
                    //OnPropertyChanged("AsMetric")
                }
            }
        }
        public class Other_NoConversion
        {
            private DataUnitType _englishUnitType = DataUnitType.NOConversions;
            private DataUnitType _metricUnitType = DataUnitType.NOConversions;
            private string _value;
            private DateTime _timeOfVal;
            private string _title;
            private Boolean _AutoCalulate;

            private DataUnitType _curUnitType;
            private string _asOther;
            public string ToEnglish()
            {
                return _value;
            }

            public string ToMetric()
            {
                return _value;
            }

            public string Value //Implements IValue.Value
            {
                get
                {
                    return _value;
                }
                set
                {
                    _value = value;
                    AsEnglish = value;
                    AsMetric = value;
                    //OnPropertyChanged("Value")
                }
            }

            public string UnitType //Implements IValue.UnitType
            {
                get
                {
                    return (_curUnitType).ToString();
                }
                set
                {
                    _curUnitType = (DataUnitType)Enum.Parse(typeof(DataUnitType), value);
                    //OnPropertyChanged("UnitType")
                }
            }

            public DataUnitType EnglishUnitType //Implements IValue.EnglishUnitType
            {
                get
                {
                    return _englishUnitType;
                }
            }

            public DataUnitType MetricUnitType //Implements IValue.MetricUnitType
            {
                get
                {
                    return _metricUnitType;
                }
            }
            public string Title //Implements IValue.Title
            {
                get
                {
                    return _title;
                }
                set
                {
                    _title = value;
                    //OnPropertyChanged("Title")
                }
            }
            public Boolean autoCalculate
            {
                get
                {
                    return _AutoCalulate;
                }
                set
                {
                    _AutoCalulate = value;
                }
            }
            public Other_NoConversion(string DataTitle, DataUnitType _UnitType, Boolean AutoCalculate = true)
            {
                Title = DataTitle;
                UnitType = _UnitType.ToString();
                Value = (DataErrorVal).ToString();
                autoCalculate = AutoCalculate;
            }

            public DateTime TimeOfValue //Implements IValue.TimeOfValue
            {
                get
                {
                    return _timeOfVal;
                }
                set
                {
                    _timeOfVal = value;
                    //OnPropertyChanged("TimeOfValue")
                }
            }
            public string AsEnglish //Implements IValue.AsEnglish
            {
                get
                {
                    return _value;
                }
                set
                {
                    _asOther = _value;
                    //OnPropertyChanged("AsEnglish")
                }
            }

            public string AsMetric //Implements IValue.AsMetric
            {
                get
                {
                    return _value;
                }
                set
                {
                    _asOther = _value;
                    //OnPropertyChanged("AsMetric")
                }
            }
        }

        /// <summary>
        /// Defines the unit Type that the Station Data Value is Stored in. eg the "UnitType"
        /// </summary>
        /// <remarks>This creates a standard to make it easier for other Plugin Creators.</remarks>
        public enum DataUnitType
        {
            NOConversions = 0,
            Bar_InHg = 1,
            Bar_mbar = 2,
            Bar_hpa = 3,
            Temp_Fahrenheit = 4,
            Temp_Celsius = 5,
            Wind_Mph = 6,
            Wind_Kph = 7,
            Precip_Inch = 8,
            Precip_Millimeter = 9,
            Other_Feet = 10,
            Other_Meter = 11
        }
        public class StationData : INotifyPropertyChanged
        {
            private DateTime _lastUpdated;
            private Temperature _TempOutCur = new Temperature("Temperature", DataUnitType.Temp_Fahrenheit);
            public Temperature TempOutCur
            {
                get
                {
                    return _TempOutCur;
                }
                set
                {
                    _TempOutCur = value;
                }
            }
            private Temperature _TempOutHigh = new Temperature("High Temp", DataUnitType.Temp_Fahrenheit);
            public Temperature TempOutHigh
            {
                get
                {
                    return _TempOutHigh;
                }
                set
                {
                    _TempOutHigh = value;
                }
            }
            private Temperature _TempOutLow = new Temperature("Low Temp", DataUnitType.Temp_Fahrenheit);
            public Temperature TempOutLow
            {
                get
                {
                    return _TempOutLow;
                }
                set
                {
                    _TempOutLow = value;
                }
            }
            private Temperature _TempApparent = new Temperature("Temp Apparent", DataUnitType.Temp_Fahrenheit);
            public Temperature TempApparent
            {
                get
                {
                    return _TempApparent;
                }
                set
                {
                    _TempApparent = value;
                }
            }

            private Temperature _TempInCur = new Temperature("Indoor Temperature", DataUnitType.Temp_Fahrenheit);
            public Temperature TempInCur
            {
                get
                {
                    return _TempInCur;
                }
                set
                {
                    _TempInCur = value;
                }
            }
            private Temperature _TempInHigh = new Temperature("High Indoor Temp", DataUnitType.Temp_Fahrenheit);
            public Temperature TempInHigh
            {
                get
                {
                    return _TempInHigh;
                }
                set
                {
                    _TempInHigh = value;
                }
            }
            private Temperature _TempInLow = new Temperature("Low Indoor Temp", DataUnitType.Temp_Fahrenheit);
            public Temperature TempInLow
            {
                get
                {
                    return _TempInLow;
                }
                set
                {
                    _TempInLow = value;
                }
            }

            private Temperature _HeatIndex = new Temperature("Heat Index", DataUnitType.Temp_Fahrenheit);
            public Temperature HeatIndex
            {
                get
                {
                    return _HeatIndex;
                }
                set
                {
                    _HeatIndex = value;
                }
            }
            private Temperature _WindChill = new Temperature("Wind Chill", DataUnitType.Temp_Fahrenheit);
            public Temperature WindChill
            {
                get
                {
                    return _WindChill;
                }
                set
                {
                    _WindChill = value;
                }
            }
            private Other_NoConversion _SolarRaidiation = new Other_NoConversion("Solar Raidiation", DataUnitType.NOConversions);
            public Other_NoConversion SolarRaidiation
            {
                get
                {
                    return _SolarRaidiation;
                }
                set
                {
                    _SolarRaidiation = value;
                }
            }
            private Other_NoConversion _UVIndex = new Other_NoConversion("UV Index", DataUnitType.NOConversions);
            public Other_NoConversion UVIndex
            {
                get
                {
                    return _UVIndex;
                }
                set
                {
                    _UVIndex = value;
                }
            }

            private Wind _WindSpeed = new Wind("Wind Speed", DataUnitType.Wind_Mph);
            public Wind WindSpeed
            {
                get
                {
                    return _WindSpeed;
                }
                set
                {
                    _WindSpeed = value;
                }
            }
            private Other_NoConversion _WindDirection = new Other_NoConversion("Wind Direction", DataUnitType.NOConversions);
            public Other_NoConversion WindDirection
            {
                get
                {
                    return _WindDirection;
                }
                set
                {
                    _WindDirection = value;
                }
            }
            private Other_NoConversion _WindBearing = new Other_NoConversion("Wind Bearing", DataUnitType.NOConversions);
            public Other_NoConversion WindBearing
            {
                get
                {
                    return _WindBearing;
                }
                set
                {
                    _WindBearing = value;
                }
            }
            private Wind _WindPeakGust = new Wind("Peak Wind Gust", DataUnitType.Wind_Mph);
            public Wind WindPeakGust
            {
                get
                {
                    return _WindPeakGust;
                }
                set
                {
                    _WindPeakGust = value;
                }
            }
            private Wind _Wind5MinPeak = new Wind("5 Min Wind Peak", DataUnitType.Wind_Mph);
            public Wind Wind5MinPeak
            {
                get
                {
                    return _Wind5MinPeak;
                }
                set
                {
                    _Wind5MinPeak = value;
                }
            }
            private Wind _Wind1MinAvg = new Wind("1 Min Wind Avg", DataUnitType.Wind_Mph);
            public Wind Wind1MinAvg
            {
                get
                {
                    return _Wind1MinAvg;
                }
                set
                {
                    _Wind1MinAvg = value;
                }
            }

            private BarometricPressure _BarometerCur = new BarometricPressure("Barometer", DataUnitType.Bar_InHg);
            public BarometricPressure BarometerCur
            {
                get
                {
                    return _BarometerCur;
                }
                set
                {
                    _BarometerCur = value;
                }
            }
            private BarometricPressure _BarometerHigh = new BarometricPressure("High Barometer", DataUnitType.Bar_InHg);
            public BarometricPressure BarometerHigh
            {
                get
                {
                    return _BarometerHigh;
                }
                set
                {
                    _BarometerHigh = value;
                }
            }
            private BarometricPressure _BarometerLow = new BarometricPressure("Low Barometer", DataUnitType.Bar_InHg);
            public BarometricPressure BarometerLow
            {
                get
                {
                    return _BarometerLow;
                }
                set
                {
                    _BarometerLow = value;
                }
            }
            private Other_NoConversion _ActiveWeather = new Other_NoConversion("Active Weather", DataUnitType.NOConversions);
            public Other_NoConversion ActiveWeather
            {
                get
                {
                    return _ActiveWeather;
                }
                set
                {
                    _ActiveWeather = value;
                }
            }
            private Other_NoConversion _SkyConditions = new Other_NoConversion("Sky Conditions", DataUnitType.NOConversions);
            public Other_NoConversion SkyConditions
            {
                get
                {
                    return _SkyConditions;
                }
                set
                {
                    _SkyConditions = value;
                }
            }
            private BarometricPressure _Barometer3HrChange = new BarometricPressure("3 Hr Pressure Change", DataUnitType.Bar_InHg);
            public BarometricPressure Barometer3HrChange
            {
                get
                {
                    return _Barometer3HrChange;
                }
                set
                {
                    _Barometer3HrChange = value;
                }
            }

            private Precipitation _RainLongTermTotal = new Precipitation("Rainfall This Year", DataUnitType.Precip_Inch);
            public Precipitation RainLongTermTotal
            {
                get
                {
                    return _RainLongTermTotal;
                }
                set
                {
                    _RainLongTermTotal = value;
                }
            }
            private Precipitation _RainTodayTotal = new Precipitation("Today's Rainfall", DataUnitType.Precip_Inch);
            public Precipitation RainTodayTotal
            {
                get
                {
                    return _RainTodayTotal;
                }
                set
                {
                    _RainTodayTotal = value;
                }
            }
            private Other_NoConversion _HumidityOutCur = new Other_NoConversion("Outdoor Humidity", DataUnitType.NOConversions);
            public Other_NoConversion HumidityOutCur
            {
                get
                {
                    return _HumidityOutCur;
                }
                set
                {
                    _HumidityOutCur = value;
                }
            }
            private Other_NoConversion _HumidityOutHigh = new Other_NoConversion("High Humidity", DataUnitType.NOConversions);
            public Other_NoConversion HumidityOutHigh
            {
                get
                {
                    return _HumidityOutHigh;
                }
                set
                {
                    _HumidityOutHigh = value;
                }
            }
            private Other_NoConversion _HumidityOutLow = new Other_NoConversion("Low Humidity", DataUnitType.NOConversions);
            public Other_NoConversion HumidityOutLow
            {
                get
                {
                    return _HumidityOutLow;
                }
                set
                {
                    _HumidityOutLow = value;
                }
            }
            private Other_NoConversion _HumidityIn = new Other_NoConversion("Indoor Humidity", DataUnitType.NOConversions);
            public Other_NoConversion HumidityIn
            {
                get
                {
                    return _HumidityIn;
                }
                set
                {
                    _HumidityIn = value;
                }
            }
            private Precipitation _RainRateAnHr = new Precipitation("Precip Last Hour", DataUnitType.Precip_Inch);
            public Precipitation RainRateAnHr
            {
                get
                {
                    return _RainRateAnHr;
                }
                set
                {
                    _RainRateAnHr = value;
                }
            }
            private Temperature _DewPoint = new Temperature("Dew Point", DataUnitType.Temp_Fahrenheit);
            public Temperature DewPoint
            {
                get
                {
                    return _DewPoint;
                }
                set
                {
                    _DewPoint = value;
                }
            }
            private Length _CloudBase = new Length("Cloud Base", DataUnitType.Other_Feet);
            public Length CloudBase
            {
                get
                {
                    return _CloudBase;
                }
                set
                {
                    _CloudBase = value;
                }
            }
            private Length _DensityAltitude = new Length("Density Altitude", DataUnitType.Other_Feet);
            public Length DensityAltitude
            {
                get
                {
                    return _DensityAltitude;
                }
                set
                {
                    _DensityAltitude = value;
                }
            }

            /// <summary>
            /// Declares the last time the Station Data was Updated
            /// </summary>
            /// <value></value>
            /// <returns></returns>
            /// <remarks>Update this whenever data is Sucessfully retrived from the Station. This is also used to tell if the Plugin has stopped working, if its caught in a loop etc.</remarks>
            public DateTime LastUpdated
            {
                get
                {
                    return _lastUpdated;
                }
                set
                {
                    _lastUpdated = value;
                    OnPropertyChanged("LastUpdated");
                }
            }

            //Public UserDefinableData As New List(Of IValue)
            #region PropertyChanged Members
            public event PropertyChangedEventHandler PropertyChanged;

            private void OnPropertyChanged(string propertyName)
            {
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
            #endregion
        }

        public interface IWXStation
        {
            StationData WXData { get; set; }
            ObservableCollection<Config> Settings { get; set; }
            string StationErrorMessage { get; set; }
            string Status { get; set; }
            int UpdateInterval_Milliseconds { get; set; }
            /// <summary>
            /// Apply New Settings to the Station
            /// </summary>
            /// <remarks></remarks>
            void ApplySettings(ObservableCollection<Config> NewConfig);
            /// <summary>
            /// (Not Implemented)
            /// </summary>
            /// <remarks></remarks>
            void Update();
            /// <summary>
            /// This is called on program shutdown.
            /// </summary>
            /// <remarks>Use this to close TCP/IP, Serial Ports etc.</remarks>
            void Disconnect();
        }
        public interface IStationMetaData
        {
            string Type { get; }
            string Name { get; }
            string Version { get; }
        }

        public class Conversion
        {
            public static double HeatIndex(double TempOutFahenheight, double HumidityOut)
            {
                if (TempOutFahenheight > 80)
                {
                    return Math.Round(-42.379 + (2.04901523 * TempOutFahenheight) + (10.14333127 * HumidityOut) + (-0.22475541 * TempOutFahenheight * HumidityOut) + ((-6.83783 * (Math.Pow(10, -3))) * (Math.Pow(TempOutFahenheight, 2))) + ((-5.481717 * (Math.Pow(10, -2))) * (Math.Pow(HumidityOut, 2))) + ((1.22874 * (Math.Pow(10, -3))) * (Math.Pow(TempOutFahenheight, 2)) * (HumidityOut)) + ((8.5282 * (Math.Pow(10, -4))) * (TempOutFahenheight) * (Math.Pow(HumidityOut, 2))) + ((-1.99 * (Math.Pow(10, -6))) * (Math.Pow(TempOutFahenheight, 2)) * (Math.Pow(HumidityOut, 2))));
                }
                else
                {
                    return TempOutFahenheight;
                }
            }
            public static double WindChill_Fahenheight(double TempOut_Fahenheight, double WindSpeed_Mph)
            {
                if (WindSpeed_Mph > 3)
                {
                    return (double)(Math.Round((decimal)(35.74 + (0.6215 * TempOut_Fahenheight) - (35.75 * (Math.Pow(WindSpeed_Mph, 0.16))) + (0.4275 * (TempOut_Fahenheight * (Math.Pow(WindSpeed_Mph, 0.16)))))));
                }
                else
                {
                    return TempOut_Fahenheight;
                }
            }
            public static double DewPoint_Fahenheight(double TempOut_Celsius, double HumidityOut)
            {
                double returnValue = 0;
                returnValue = Conversion.Celsius_To_Fahrenheight(TempOut_Celsius - (14.55 + 0.114 * TempOut_Celsius) * (1 - (0.01 * HumidityOut)) - Math.Pow(((2.5 + 0.007 * TempOut_Celsius) * (1 - (0.01 * HumidityOut))), 3) - (15.9 + 0.117 * TempOut_Celsius) * Math.Pow((1 - (0.01 * HumidityOut)), 14));
                return returnValue;
            }
            public static double CloudBase(double TempOut_Fahenheight, double DewPoint_Fahenheight)
            {
                double returnValue = 0;
                returnValue = Math.Round(((TempOut_Fahenheight - DewPoint_Fahenheight) / 4.4) * 1000);
                return returnValue;
            }
            public static string Active_Weather(double WindSpeed_MPH, double PrecipRate_Inch)
            {
                string returnValue = "";
                returnValue = "N/A";
                if ((PrecipRate_Inch > 0.2) && (WindSpeed_MPH > 5))
                {
                    returnValue = "Thunderstorm";
                }
                else if (PrecipRate_Inch > 0.15)
                {
                    returnValue = "Heavy Rain";
                }
                else if (PrecipRate_Inch > 0.1)
                {
                    returnValue = "Rain";
                }
                else if (PrecipRate_Inch > 0.05)
                {
                    returnValue = "Light Rain";
                }
                else if (PrecipRate_Inch > 0.02)
                {
                    returnValue = "Drizzle";
                }
                else
                {
                    returnValue = "None Detected";
                }
                return returnValue;
            }
            public static string Sky_Conditions(double TempOut_Fahenheight, double Barometer_InHg, double HumidityOut, double PrecipRate_Inch, double DewPoint_Fahenheight, double CloudBase_Feet, double DensityAltitude_Feet)
            {
                string returnValue = "";
                returnValue = "N/A";
                if ((HumidityOut > 96) && (CloudBase_Feet < 100))
                {
                    returnValue = "Fog";
                }
                else if ((PrecipRate_Inch > 0.02) && (TempOut_Fahenheight < 35))
                {
                    returnValue = "Winter Mix";
                }
                else if (PrecipRate_Inch > 0.0)
                {
                    returnValue = "Cloudy";
                }
                else if ((CloudBase_Feet < 1450) && (Barometer_InHg < 29.8) && (HumidityOut > 86))
                {
                    returnValue = "Cloudy";
                }
                else if ((DewPoint_Fahenheight > 62) && (DensityAltitude_Feet > 3500))
                {
                    returnValue = "Cloudy";
                }
                else if ((HumidityOut > 69) && (Barometer_InHg < 29.75) && (DensityAltitude_Feet > 1000))
                {
                    returnValue = "Cloudy";
                }
                else if ((CloudBase_Feet < 1500) && (Barometer_InHg < 29.75) && (HumidityOut > 75))
                {
                    returnValue = "Cloudy";
                }
                else if ((DensityAltitude_Feet < -200) && (Barometer_InHg < 29.75) && (HumidityOut > 70) && (CloudBase_Feet < 1000))
                {
                    returnValue = "Cloudy";
                }
                else if ((DensityAltitude_Feet < 0) && (HumidityOut > 82) && (CloudBase_Feet < 500))
                {
                    returnValue = "Cloudy";
                }
                else if ((DensityAltitude_Feet < -500) && (Barometer_InHg < 29.79))
                {
                    returnValue = "Cloudy";
                }
                else if ((DensityAltitude_Feet < 500) && (HumidityOut > 90))
                {
                    returnValue = "Cloudy";
                }
                else if ((DensityAltitude_Feet < 200) && (Barometer_InHg < 29.8) && (HumidityOut) > 50 && (CloudBase_Feet < 2000))
                {
                    returnValue = "Cloudy";
                }
                else if ((DensityAltitude_Feet < 1800) && (Barometer_InHg < 29.75) && (HumidityOut > 60))
                {
                    returnValue = "Cloudy";
                }
                else if ((CloudBase_Feet < 2000) && (HumidityOut > 75) && ((TempOut_Fahenheight - DewPoint_Fahenheight) < 2))
                {
                    returnValue = "Cloudy";
                }
                else if ((DewPoint_Fahenheight > 62) && (Barometer_InHg < 29.8) && (TempOut_Fahenheight > 72))
                {
                    returnValue = "Partly Cloudy";
                }
                else if ((DewPoint_Fahenheight > 62) && (DensityAltitude_Feet > 2550))
                {
                    returnValue = "Partly Cloudy";
                }
                else if ((DewPoint_Fahenheight > 62) && (CloudBase_Feet < 1600) && (TempOut_Fahenheight > 72))
                {
                    returnValue = "Partly Cloudy";
                }
                else if ((DewPoint_Fahenheight > 70) && (CloudBase_Feet < 4000))
                {
                    returnValue = "Partly Cloudy";
                }
                else if ((DensityAltitude_Feet < -200) && (Barometer_InHg < 29.79))
                {
                    returnValue = "Partly Cloudy";
                }
                else if ((DensityAltitude_Feet < -1000) && (HumidityOut > 62) && (HumidityOut < 70))
                {
                    returnValue = "Partly Cloudy";
                }
                else if ((DensityAltitude_Feet < 1000) && (HumidityOut > 75) && (Barometer_InHg < 29.75))
                {
                    returnValue = "Partly Cloudy";
                }
                else if ((CloudBase_Feet < 3000) && (HumidityOut > 60) && (Barometer_InHg < 29.75))
                {
                    returnValue = "Partly Cloudy";
                }
                else if ((DewPoint_Fahenheight > 55) && (HumidityOut > 50) && (DensityAltitude_Feet < 2000) && (TempOut_Fahenheight > 75))
                {
                    returnValue = "Partly Cloudy";
                }
                else if (CloudBase_Feet > 2000)
                {
                    returnValue = "Clear";
                }
                else
                {
                    returnValue = "Clear";
                }
                return returnValue;
            }
            public static double Noaa_DensityAltitude_Feet(double TempOut_C, double BarometerCur_InHg, double DewPoint_C)
            {
                double TempK = Conversion.CtoK(TempOut_C);
                var PressureAltitude = 6.11 * 10 * ((7.5 * DewPoint_C) / (237.7 + DewPoint_C));
                var TempVirtual = Conversion.Kelvin_To_Rankine(TempK / (1 - (PressureAltitude / Conversion.inHg_to_mBar(BarometerCur_InHg)) * (1 - 0.622)));

                return Math.Round(145366 * (1 - Math.Pow(((17.326 * BarometerCur_InHg) / TempVirtual), 0.235)));
            }
            public static double Aircraft_DensityAltitude_Feet(double StationElevation_Feet, double TempOut_F, double BarometerCur_InHg)
            {
                double returnValue = 0;
                double PressureAltitude = (29.92 - BarometerCur_InHg) * (1000 + StationElevation_Feet);
                returnValue = PressureAltitude + ((288.15 - 0.0019812 * PressureAltitude) / 0.0019812) * (1 - Math.Pow(((288.15 - 0.0019812 * PressureAltitude) / (273.15 + Conversion.CtoK(Conversion.Fahrenheight_To_Celsius(TempOut_F)))), 0.234969));
                return returnValue;
            }
            public static double ApperentTemperature_feelsLike(double OutTemp, double OutHumidity, double WindSpeed)
            {
                if (OutTemp == 0)
                {
                    OutTemp = 0.01;
                }
                if (OutHumidity == 0)
                {
                    OutHumidity = 0.01;
                }
                if (WindSpeed == 0)
                {
                    WindSpeed = 0.01;
                }
                double EXXP = 17.27 * OutTemp / (237.7 + OutTemp);
                double POWR = Math.Pow(2.7182818284590451, EXXP);
                double EEE = OutHumidity / 100 * 6.105 * POWR;
                double ATEMP = OutTemp + 0.33 * EEE - 0.7 - (WindSpeed * 1000 / 3600) - 4;
                ATEMP = Math.Round(ATEMP, 0);
                return ATEMP;
            }
            public static double Celsius_To_Fahrenheight(double Celsius_Val)
            {
                double returnValue = 0;
                returnValue = Math.Round((Celsius_Val * 1.8) + 32, 1);
                return returnValue;
            }
            public static double Fahrenheight_To_Celsius(double Fahrenheight_Val)
            {
                double returnValue = 0;
                returnValue = Math.Round((Fahrenheight_Val - 32) / 1.8, 1);
                return returnValue;
            }
            public static double Kph_To_Mph(double Kph_Val)
            {
                double returnValue = 0;
                returnValue = Math.Round(Kph_Val / 1.6, 1);
                return returnValue;
            }
            public static double Mph_To_Kph(double Mph_Val)
            {
                double returnValue = 0;
                returnValue = Math.Round(Mph_Val * 1.6, 1);
                return returnValue;
            }
            public static double mBar_to_inHg(double mBar_Val)
            {
                double returnValue = 0;
                returnValue = Math.Round(mBar_Val / 33.86388158, 2);
                return returnValue;
            }
            public static double inHg_to_mBar(double inHg_Val) // mbar is the same as hpa
            {
                double returnValue = 0;
                returnValue = Math.Round(inHg_Val * 33.86388158, 1);
                return returnValue;
            }
            public static double mm_to_in(double mm_Val)
            {
                double returnValue = 0;
                returnValue = Math.Round(mm_Val * 0.0393700787401575, 2);
                return returnValue;
            }
            public static double in_To_mm(double In_Val)
            {
                double returnValue = 0;
                returnValue = Math.Round(In_Val * 25.4, 1);
                return returnValue;
            }
            public static double Feet_To_Meters(double Feet_Val)
            {
                double returnValue = 0;
                returnValue = Math.Round(Feet_Val * 0.3048);
                return returnValue;
            }
            public static double Meters_To_Feet(double Meters_Val)
            {
                double returnValue = 0;
                returnValue = Math.Round(Meters_Val / 0.3048);
                return returnValue;
            }
            public static double CtoK(double value)
            {
                return 273.15 + value;
            }
            public static double Kelvin_To_Rankine(double Temp_Kelvin)
            {
                return (((double)9 / 5) * (Temp_Kelvin - 273.15) + 32) + 459.69;
            }
            public static double KtoC(double value)
            {
                return value - 273.15;
            }

            public static double FToR(double value)
            {
                return value + 459.67;
            }

            public static double RToF(double value)
            {
                return value - 459.67;
            }
            public static string GetWindDirection(double WindBearing)
            {
                string returnValue = "";
                //WindBearing = (Double.Round(WindBearing * 1.4078431372549021))
                if (WindBearing >= 11.25 & WindBearing <= 33.75)
                {
                    returnValue = "NNE";
                }
                else if (WindBearing >= 33.75 & WindBearing <= 56.25)
                {
                    returnValue = "NE";
                }
                else if (WindBearing >= 56.25 & WindBearing <= 78.75)
                {
                    returnValue = "ENE";
                }
                else if (WindBearing >= 78.75 & WindBearing <= 101.25)
                {
                    returnValue = "E";
                }
                else if (WindBearing >= 101.25 & WindBearing <= 123.75)
                {
                    returnValue = "ESE";
                }
                else if (WindBearing > 123.75 & WindBearing <= 146.25)
                {
                    returnValue = "SE";
                }
                else if (WindBearing >= 146.25 & WindBearing <= 168.75)
                {
                    returnValue = "SSE";
                }
                else if (WindBearing >= 168.75 & WindBearing <= 191.25)
                {
                    returnValue = "S";
                }
                else if (WindBearing >= 191.25 & WindBearing <= 213.75)
                {
                    returnValue = "SSW";
                }
                else if (WindBearing >= 213.75 & WindBearing <= 236.25)
                {
                    returnValue = "SW";
                }
                else if (WindBearing >= 236.25 & WindBearing <= 258.75)
                {
                    returnValue = "WSW";
                }
                else if (WindBearing >= 258.75 & WindBearing <= 281.25)
                {
                    returnValue = "W";
                }
                else if (WindBearing >= 281.25 & WindBearing <= 303.75)
                {
                    returnValue = "WNW";
                }
                else if (WindBearing >= 303.75 & WindBearing <= 326.25)
                {
                    returnValue = "NW";
                }
                else if (WindBearing >= 326.25 & WindBearing <= 348.75)
                {
                    returnValue = "NNW";
                }
                else
                {
                    returnValue = "N";
                }
                return returnValue;
            }

            #region Misc Constants
            public static double gravity = 9.80665; // g at sea level at latitude 45.5 degrees in m/sec^2
            public static double uGC = 8.31432; // universal gas constant in J/mole-K
            public static double moleAir = 0.0289644; // mean molecular mass of air in kg/mole
            public static double moleWater = 0.01801528; // molecular weight of water in kg/mole
            public static double gasConstantAir = 8.31432 / 0.0289644; // (287.053) gas constant for air in J/kgK
            public static double standardSLP = 1013.25; // standard sea level pressure in hPa
            public static double standardSlpInHg = 29.921; // standard sea level pressure in inHg
            public static double standardTempK = 288.15; // standard sea level temperature in Kelvin
            public static double earthRadius45 = 6356.766; // radius of the earth at latitude 45.5 degrees in km

            public static double standardLapseRate = 0.0065; // standard lapse rate (6.5C/1000m i.e. 6.5K/1000m)

            public static double standardLapseRateFt = 0.0065 * 0.3048; // (0.0019812) standard lapse rate per foot (1.98C/1000ft)
            public static double vpLapseRateUS = 0.00275; // lapse rate used by Davis VantagePro (2.75F/1000ft)
            public static double manBarLapseRate = 0.0117; // lapse rate from Manual of Barometry (11.7F/1000m, which = 6.5C/1000m)

            const string DefaultSLPAlgorithm = "paManBar";
            const string DefaultAltimeterAlgorithm = "aaMADIS";
            const string DefaultVapAlgorithm = "vaBolton";
            #endregion
            public static string DegreesMinutesSeconds_To_DecimalDegrees(string[] DMS_Val)
            {
                double TempLat = 1;
                if (double.Parse(DMS_Val[0]) < 0)
                {
                    TempLat = -1;
                }
                double Val_To_Return = (((double.Parse(DMS_Val[0])) * TempLat) + ((double)(decimal.Parse(DMS_Val[1])) * ((double)1 / 60)) + ((double.Parse(DMS_Val[2])) * ((double)1 / 60) * ((double)1 / 60))) * TempLat;
                return (decimal.Round((decimal)Val_To_Return, 6)).ToString();
            }

            public static string DecimalDegrees_To_LORAN_DecimalDegreeMinutes(double DD_Val, bool IsLatitude) //Coordinate Conversion For CWOP Uploads
            {
                double DD = 1;
                if (DD_Val < 0)
                {
                    DD = -1;
                }

                double Degrees = Math.Truncate(DD_Val);
                double DegreeRemainder = (DD_Val - Degrees) * DD;

                double Minutes = DegreeRemainder * 60;
                if (DD_Val < 0)
                {
                    if (IsLatitude) //N+/S-
                    {
                        return (decimal.Parse((Degrees).ToString().Replace("-", "") + Minutes)).ToString("0000.00") + "S";
                    }
                    else //E+/W- Longitude
                    {
                        return (decimal.Parse((Degrees).ToString().Replace("-", "") + Minutes)).ToString("00000.00") + "W";
                    }
                }
                else //If CDec(DD_Val) > 0 Then
                {
                    if (IsLatitude) //N+/S-
                    {
                        return (decimal.Parse((Degrees).ToString() + System.Convert.ToString(Minutes))).ToString("0000.00") + "N";
                    }
                    else //E+/W- Longitude
                    {
                        return (decimal.Parse((Degrees).ToString() + System.Convert.ToString(Minutes))).ToString("00000.00") + "E";
                    }
                }

            }
            public static string[] DecimalDegrees_To_DegreesMinutesSeconds(string DD_Val)
            {
                double DD = 1.0D;
                if (double.Parse(DD_Val) < 0)
                {
                    DD = -1.0D;
                }

                double Degrees = Math.Truncate(double.Parse(DD_Val));
                double DegreeRemainder = (double.Parse(DD_Val)) - Degrees;

                double Minutes = Math.Truncate(DegreeRemainder * 60.0D) * DD;
                double MinuteRemainder = ((DegreeRemainder * 60.0D) * DD) - Minutes;

                double Seconds = (double)(decimal.Round((decimal)(MinuteRemainder * 60.0D), 2));
                return (Degrees + " " + System.Convert.ToString(Minutes) + " " + System.Convert.ToString(Seconds)).Split(' ');
            }

            public static string Get_SunRise_SunSet(DateTime input_date, int set_rise, double LocalTimeOffset, double longitude, double latitude)
            {
                double D2R = Math.PI / 180.0D;
                double R2D = 180.0D / Math.PI;
                //Convert longitude into hour value
                double long_hour = longitude / 15.0D;
                double t = 0;

                //sunset = 0, sunrise = 1
                //calculate approximate time
                if (set_rise == 1)
                {
                    t = input_date.DayOfYear + ((6.0D - long_hour) / 24.0D);
                }
                else if (set_rise == 0)
                {
                    t = input_date.DayOfYear + ((18.0D - long_hour) / 24.0D);
                }

                //Calculate Sun's mean anomaly time
                double mean = (0.9856 * t) - 3.289;

                //Calculate Sun's true longitude
                double sun_true_long = mean + (1.916 * Math.Sin(mean * D2R)) + (0.02 * Math.Sin(2.0D * mean * D2R)) + 282.634;
                if (sun_true_long > 360.0D)
                {
                    sun_true_long = sun_true_long - 360.0D;
                }
                else if (sun_true_long < 0.0D)
                {
                    sun_true_long = sun_true_long + 360.0D;
                }

                //Calculate Sun's right ascension
                double right_ascension = R2D * Math.Atan(0.91764 * Math.Tan(sun_true_long * D2R));
                if (right_ascension > 360.0D)
                {
                    right_ascension = right_ascension - 360.0D;
                }
                else if (right_ascension < 0.0D)
                {
                    right_ascension = right_ascension + 360.0D;
                }

                //Adjust right ascension value to be in the same quadrant as Sun's true longitude
                double Lquadrant = (Math.Floor(sun_true_long / 90.0D)) * 90.0D;
                double RAquadrant = (Math.Floor(right_ascension / 90.0D)) * 90.0D;
                right_ascension = right_ascension + (Lquadrant - RAquadrant);

                //Convert right ascension value into hours
                right_ascension = right_ascension / 15.0D;

                //Calculate Sun's declination
                double sinDec = 0.39782 * Math.Sin(sun_true_long * D2R);
                double cosDec = Math.Cos(Math.Asin(sinDec));

                //Setting Sun's zenith value
                double zenith = 90.0D + (50.0D / 60.0D);

                //Calculate Sun's local hour angle
                double cosH = (Math.Cos(zenith * D2R) - (sinDec * Math.Sin(latitude * D2R))) / (cosDec * Math.Cos(latitude * D2R));

                if (cosH > 1.0D)
                {
                    return ("Sun never rises on this day. " + System.Convert.ToString(input_date.Year) + "/" + System.Convert.ToString(input_date.Month) + "/" + System.Convert.ToString(input_date.Day));
                }
                else if (cosH < -1.0D)
                {
                    return ("Sun never sets on this day. " + System.Convert.ToString(input_date.Year) + "/" + System.Convert.ToString(input_date.Month) + "/" + System.Convert.ToString(input_date.Day));
                }

                //Calculate and convert into hour of sunset or sunrise
                double hour = 0.0D;
                if (set_rise == 1.0D)
                {
                    hour = 360.0D - R2D * Math.Acos(cosH);
                }
                else if (set_rise == 0.0D)
                {
                    hour = R2D * Math.Acos(cosH);
                }

                hour = hour / 15.0D;

                //Calculate local mean time of rising or setting
                double local_mean_time = hour + right_ascension - (0.06571 * t) - 6.622;

                //Adjust time to UTC
                double utc = local_mean_time - long_hour;

                //Convert time from UTC to local time zone
                double local_time = utc + LocalTimeOffset;
                if (local_time > 24)
                {
                    local_time = local_time - 24;
                }
                else if (local_time < 0)
                {
                    local_time = local_time + 24;
                }

                //Convert the local_time into time format
                int s_hour = (int)(Math.Floor(local_time));
                int s_minute = (int)((local_time - s_hour) * 60.0D);
                string ReturnString = "";

                if (set_rise == 1)
                {
                    ReturnString = (s_hour).ToString();
                }
                else
                {
                    ReturnString = (s_hour - 12).ToString();
                }
                if (s_minute < 10)
                {
                    ReturnString += ":0" + System.Convert.ToString(s_minute);
                }
                else
                {
                    ReturnString += ":" + System.Convert.ToString(s_minute);
                }
                if (set_rise == 1)
                {
                    ReturnString += " AM";
                }
                else
                {
                    ReturnString += " PM";
                }
                return ReturnString;
            }

            #region Barometric Pressure Conversions
            public static double StationToSensorPressure(double pressureHPa, double sensorElevationM, double stationElevationM, double currentTempC)
            {
                // from ASOS formula specified in US units
                return inHg_to_mBar(mBar_to_inHg(pressureHPa) / Power10(0.00813 * Meters_To_Feet(sensorElevationM - stationElevationM) / FToR(Celsius_To_Fahrenheight(currentTempC))));
            }

            public static double StationPressureToAltimeter(double PressureHPa, double elevationM, string algorithm = "aaMADIS")
            {

                double geopEl = 0;
                double k1 = 0;
                double k2 = 0;

                switch (algorithm)
                {
                    case "aaASOS":
                        // see ASOS training at http://www.nwstc.noaa.gov
                        // see also http://wahiduddin.net/calc/density_altitude.htm
                        return inHg_to_mBar(Power(System.Convert.ToDouble(Power(mBar_to_inHg(PressureHPa), 0.1903) + (0.00001313 * Meters_To_Feet(elevationM))), 5.255));

                    case "aaASOS2":
                        geopEl = GeopotentialAltitude(elevationM);
                        k1 = standardLapseRate * gasConstantAir / gravity; //  approx. 0.190263
                        k2 = 0.0000841728638; // (standardLapseRate / standardTempK) * (Power(standardSLP,  k1)
                        return Power(Power(PressureHPa, k1) + (k2 * geopEl), 1 / k1);

                    case "aaMADIS":
                        // from MADIS API by NOAA Forecast Systems Lab, see http://madis.noaa.gov/madis_api.html
                        k1 = 0.190284; // discrepency with calculated k1 probably because Smithsonian used less precise gas constant and gravity values
                        k2 = 0.000084184960528; // (standardLapseRate / standardTempK) * (Power(standardSLP, k1)
                        return Power(Power(PressureHPa - 0.3, k1) + (k2 * elevationM), 1 / k1);

                    case "aaNOAA":
                        // see http://www.srh.noaa.gov/elp/wxclc/formulas/altimeterSetting.html
                        k1 = 0.190284; // discrepency with k1 probably because Smithsonian used less precise gas constant and gravity values
                        k2 = 0.0000842288069; // (standardLapseRate / 288) * (Power(standardSLP, k1SMT)
                        return (PressureHPa - 0.3) * Power(1 + (k2 * (elevationM / Power(PressureHPa - 0.3, k1))), 1 / k1);

                    case "aaWOB":
                        // see http://www.wxqa.com/archive/obsman.pdf
                        k1 = standardLapseRate * gasConstantAir / gravity; //  approx. 0.190263
                        k2 = 0.00001312603; // (standardLapseRateFt / standardTempK) * Power(standardSlpInHg, k1)
                        return inHg_to_mBar(Power(System.Convert.ToDouble(Power(mBar_to_inHg(PressureHPa), k1) + (k2 * Meters_To_Feet(elevationM))), 1 / k1));

                    case "aaSMT":
                        // see WMO Instruments and Observing Methods Report No.19 at http://www.wmo.int/pages/prog/www/IMOP/publications/IOM-19-Synoptic-AWS.pdf
                        k1 = 0.190284; // discrepency with calculated value probably because Smithsonian used less precise gas constant and gravity values
                        k2 = 0.0000430899; // (standardLapseRate / 288) * (Power(standardSlpInHg, k1SMT))
                        geopEl = GeopotentialAltitude(elevationM);
                        return inHg_to_mBar((mBar_to_inHg(PressureHPa) - 0.01) * Power(1 + (k2 * (geopEl / Power(mBar_to_inHg(PressureHPa) - 0.01, k1))), 1 / k1));

                    default:
                        return PressureHPa; // unknown algorithm
                }
            }

            public static double StationToSeaLevelPressure(double pressureHPa, double elevationM, double currentTempC, double meanTempC, byte humidity, string algorithm = "paManBar")
            {
                return pressureHPa * PressureReductionRatio(pressureHPa, elevationM, currentTempC, meanTempC, humidity, algorithm);
            }

            public static double SensorToStationPressure(double pressureHPa, double sensorElevationM, double stationElevationM, double currentTempC)
            {
                // see ASOS training at http://www.nwstc.noaa.gov
                // from US units ASOS formula
                return inHg_to_mBar(mBar_to_inHg(pressureHPa) * Power10(0.00813 * Meters_To_Feet(sensorElevationM - stationElevationM) / FToR(Celsius_To_Fahrenheight(currentTempC))));
            }

            // still to do
            public static double AltimeterToStationPressure(double pressureHPa, double elevationM, string algorithm = "aaMADIS")
            {
                return pressureHPa;
            }

            public static double SeaLevelToStationPressure(double pressureHPa, double elevationM, double currentTempC, double meanTempC, byte humidity, string algorithm = "paManBar")
            {
                return (pressureHPa / PressureReductionRatio(pressureHPa, elevationM, currentTempC, meanTempC, humidity, algorithm));
            }

            public static double PressureReductionRatio(double pressureHPa, double elevationM, double currentTempC, double meanTempC, byte humidity, string algorithm = "paManBar")
            {
                double geopElevationM = 0;
                double hCorr = 0;

                switch (algorithm)
                {
                    case "paUnivie":
                        //  see http://www.univie.ac.at/IMG-Wien/daquamap/Parametergencom.html

                        geopElevationM = GeopotentialAltitude(elevationM);
                        return Math.Exp(((gravity / gasConstantAir) * geopElevationM) / (VirtualTempK(pressureHPa, meanTempC, humidity) + (geopElevationM * standardLapseRate / 2)));

                    case "paDavisVp":
                        // see http://www.exploratorium.edu/weather/barometer.html

                        if (humidity > 0)
                        {
                            hCorr = ((double)9 / 5) * HumidityCorrection(currentTempC, elevationM, humidity, "vaDavisVP");
                        }
                        else
                        {
                            hCorr = 0;
                        }
                        // In the case of DavisVp, take the constant values literally.
                        return Power(10, System.Convert.ToDouble(Meters_To_Feet(elevationM) / (122.8943111 * (Celsius_To_Fahrenheight(meanTempC) + 460 + (Meters_To_Feet(elevationM) * vpLapseRateUS / 2) + hCorr))));

                    case "paManBar":
                        // see WMO Instruments and Observing Methods Report No.19 at http://www.wmo.int/pages/prog/www/IMOP/publications/IOM-19-Synoptic-AWS.pdf
                        // see WMO Instruments and Observing Methods Report No.19 at http://www.wmo.ch/web/www/IMOP/publications/IOM-19-Synoptic-AWS.pdf

                        if (humidity > 0)
                        {
                            hCorr = ((double)9 / 5) * HumidityCorrection(currentTempC, elevationM, humidity, "vaBuck");
                        }
                        else
                        {
                            hCorr = 0;
                        }
                        geopElevationM = GeopotentialAltitude(elevationM);
                        return Math.Exp(geopElevationM * 0.061454 / (Celsius_To_Fahrenheight(meanTempC) + 459.7 + (geopElevationM * manBarLapseRate / 2) + hCorr));

                    default:
                        return 1; // unknown algorithm
                }
            }
            public static double HumidityCorrection(double tempC, double elevationM, byte humidity, string algorithm = "vaBolton")
            {
                double vapPress = 0;

                vapPress = ActualVaporPressure(tempC, humidity, algorithm);
                return (vapPress * ((0.0000000028322 * Math.Sqrt(elevationM)) + (0.00002225 * elevationM) + 0.10743));
            }
            public static double GeopotentialAltitude(double geometricAltitudeM)
            {

                return (earthRadius45 * 1000 * geometricAltitudeM) / ((earthRadius45 * 1000) + geometricAltitudeM);
            }
            public static double ActualVaporPressure(double tempC, byte humidity, string algorithm = "vaBolton")
            {

                return (humidity * SaturationVaporPressure(tempC, algorithm)) / 100;
            }
            public static double SaturationVaporPressure(double tempC, string algorithm = "vaBolton")
            {

                // see http://cires.colorado.edu/~voemel/vp.html   comparison of vapor pressure algorithms
                // see (for DavisVP) http://www.exploratorium.edu/weather/dewpoint.html
                switch (algorithm)
                {
                    case "vaDavisVp":
                        return 6.112 * Math.Exp((17.62 * tempC) / (243.12 + tempC)); // Davis Calculations Doc
                    case "vaBuck":
                        return 6.1121 * Math.Exp((18.678 - (tempC / 234.5)) * tempC / (257.14 + tempC)); // Buck(1996)
                    case "vaBuck81":
                        return 6.1121 * Math.Exp((17.502 * tempC) / (240.97 + tempC)); // Buck(1981)
                    case "vaBolton":
                        return 6.112 * Math.Exp(17.67 * tempC / (tempC + 243.5)); // Bolton(1980)
                    case "vaTetenNWS":
                        return 6.112 * Power(10, 7.5 * tempC / (tempC + 237.7)); // Magnus Teten see www.srh.weather.gov/elp/wxcalc/formulas/vaporPressure.html
                    case "vaTetenMurray":
                        return Power(10, (7.5 * tempC / (237.5 + tempC)) + 0.7858); // Magnus Teten (Murray 1967)
                    case "vaTeten":
                        return 6.1078 * Power(10, 7.5 * tempC / (tempC + 237.3)); // Magnus Teten see www.vivoscuola.it/US/RSIGPP3202/umidita/attivita/relhumONA.htm
                    default:
                        return 0; // unknown algorithm
                }
            }
            public static double VirtualTempK(double pressureHPa, double tempC, byte humidity)
            {

                double vapPres = 0;
                var epsilon = 1 - (moleWater / moleAir); // // 0.37802

                // see http://www.univie.ac.at/IMG-Wien/daquamap/Parametergencom.html
                // see also http://www.vivoscuola.it/US/RSIGPP3202/umidita/attiviat/relhumONA.htm
                // see also http://wahiduddin.net/calc/density_altitude.htm
                vapPres = ActualVaporPressure(tempC, humidity, "vaBuck");
                return (CtoK(tempC)) / (1 - (epsilon * (vapPres / pressureHPa)));
            }
            #endregion
            #region Misc
            public static double Power(double @base, double exponent)
            {

                if (exponent == 0.0)
                {
                    return 1.0; // n**0 = 1
                }
                else if ((@base == 0.0) && (exponent > 0.0))
                {
                    return 0.0; // 0**n = 0, n > 0
                }
                else
                {
                    return Math.Exp(exponent * Math.Log(@base));
                }
            }

            public static double Power10(double exponent)
            {
                var ln10 = 2.302585093; // Ln(10);

                if (exponent == 0.0)
                {
                    return 1.0;
                }
                else
                {
                    return Math.Exp(exponent * ln10);
                }
            }
            #endregion
        }
        public class MiscFunctions
        {
            /// <summary>
            /// Gets a List of all the Serial Ports on the PC, Including Virtual Ports
            /// </summary>
            /// <returns>A List of all the Serial Ports on the PC, Including Virtual Ports</returns>
            /// <remarks></remarks>
            public static List<string> GetSerialNames()
            {
                List<string> returnValue = default(List<string>);
                returnValue = new List<string>();
                try
                {
                    //Dim searcher As New ManagementObjectSearcher("root\CIMV2", "SELECT * FROM Win32_SerialPort")
                    //For Each queryObj As ManagementObject In searcher.Get()
                    //    GetSerialNames.Add(queryObj("DeviceID"))
                    //Next
                    //Dim _validSerialNames As New List(Of String)
                    for (var i = 1; i <= 255; i++)
                    {
                        returnValue.Add("COM" + System.Convert.ToString(i));
                    }
                    //For i = 1 To 100
                    //    Using tempSerialPort As New System.IO.Ports.SerialPort
                    //        tempSerialPort.PortName = ("COM" & i)
                    //        Try
                    //            tempSerialPort.Open()
                    //            If tempSerialPort.IsOpen Then
                    //                GetSerialNames.Add("COM" & i)
                    //                tempSerialPort.Close()
                    //            End If
                    //        Catch ex As Exception

                    //        End Try

                    //    End Using
                    //    'Threading.Thread.Sleep(2)
                    //Next
                }
                catch (ManagementException)
                {
                }
                return returnValue;
            }
        }
    }
}
