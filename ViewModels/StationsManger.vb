Imports System.IO
Imports System.Net.Sockets
'Imports System.Management
Imports System.Collections.ObjectModel
Imports API
Imports API.API
Imports API.API.StationData
Imports System.Reflection
Imports System.ComponentModel.Composition.Hosting
Imports System.ComponentModel.Composition
Imports Microsoft.ComponentModel.Composition.Hosting
Imports Microsoft.ComponentModel.Composition
Imports System.Text

Public Class StationsManger
    Implements IViewHost
    'This will manage the loading of the WXStationTypes plugins
    'It will also contain a list of all the Active Stations
    'And Will Manage the loading and Saving of the Station Configuration
    <ImportMany(AllowRecomposition:=False, RequiredCreationPolicy:=CreationPolicy.NonShared)>
    Public StationTypes As IEnumerable(Of ExportFactory(Of IWXStation, IStationMetaData))
    Private Comp_Container As CompositionContainer
    Public Property Status As String
    Private Comp_Catalog As New AggregateCatalog

    Public ReadOnly Property StationTypeNames As List(Of String)
        Get
            Dim TempList As New List(Of String)
            For i = 0 To StationTypes.Count - 1
                TempList.Add(StationTypes(i).Metadata.Type)
            Next
            Return TempList
        End Get
    End Property

    Public WXStations As New ObservableCollection(Of Station)
    'Private StationWorkers As New List(Of WXStationUpdaterWorker)

    Sub New()
        Comp_Catalog.Catalogs.Add(New AssemblyCatalog(GetType(StationsManger).Assembly))
        Try
            If My.Computer.FileSystem.DirectoryExists(MainWindow.StationPluginFolderPath) Then
                Comp_Catalog.Catalogs.Add(New DirectoryCatalog(MainWindow.StationPluginFolderPath))
            Else
                My.Computer.FileSystem.CreateDirectory(MainWindow.StationPluginFolderPath)
            End If
            Dim Ep As ExportFactoryProvider = New ExportFactoryProvider
            Comp_Container = New CompositionContainer(Comp_Catalog, Ep)
            Ep.SourceProvider = Comp_Container
            Comp_Container.ComposeParts(Me)

        Catch ex As Exception
            MsgBox("An error has occoured while loading WX Station plugins." & vbCrLf & ex.Message)
        End Try
        If StationTypes.Count > 1 Then
            'AddWXStation(StationTypes(1).Metadata.Type, "USB Davis Station", True)
            'AddWXStation(StationTypes(0).Metadata.Type, "Home Station", True)
        End If
    End Sub

    Public Sub ApplyConfigToWXStation(ByVal Station_Name As String, ByVal NewSettings As ObservableCollection(Of Config))
        For i = 0 To WXStations.Count - 1
            If WXStations(i).Name = Station_Name Then
                WXStations(i).WXStation.ApplySettings(NewSettings)
                Exit For
            End If
        Next
    End Sub
    Public Function GetStationNameList() As List(Of String)
        'This is to get a List of the WXStation Names because we need a way to retrive a list of the WX stations that are setup.
        Dim TempList As New List(Of String)
        For i = 0 To WXStations.Count - 1
            TempList.Add(WXStations(i).Name)
        Next
        Return TempList
    End Function
    Sub Disconnect()
        For i = 0 To WXStations.Count - 1
            WXStations(i).CloseStation()
        Next
        Dim IsStillBusy As Boolean = True
        Do
            IsStillBusy = False
            For Each Stn In WXStations
                If Stn.IsBusy Then
                    IsStillBusy = True
                    Exit For
                End If
            Next
            System.Threading.Thread.Sleep(250)
        Loop While IsStillBusy
    End Sub

    Public Function AddWXStation(Station_Type As String, StationName As String, IsNew As Boolean) As Integer Implements API.API.IViewHost.AddStation
        For i = 0 To StationTypes.Count - 1
            If StationTypes(i).Metadata.Type = Station_Type Then
                WXStations.Add(New Station(StationName, StationTypes(i).CreateExport().Value, Station_Type))
                Exit For
            End If
        Next
        Return 0
    End Function

    Public Sub StartStationsUpdating()
        For i = 0 To WXStations.Count() - 1
            WXStations(i).StartUpdating()
        Next
    End Sub

    Public Sub ReConnectAllStations()
        For i = 0 To WXStations.Count() - 1
            WXStations(i).Reconnect()
        Next
    End Sub

    Public Function AvailableStationTypes() As List(Of String) Implements IViewHost.AvailableStationTypes
        Dim TempStationTypes As New List(Of String)
        For i = 0 To StationTypes.Count - 1
            TempStationTypes.Add(StationTypes(i).Metadata.Type)
        Next
        Return TempStationTypes
    End Function

    Public Function RemoveStation(StationName As String) As Integer Implements IViewHost.RemoveStation
        For i = 0 To WXStations.Count - 1
            If WXStations(i).Name = StationName Then
                WXStations(i).CloseStation()
                WXStations.RemoveAt(i)
                Exit For
            End If
        Next
        Return 0
    End Function

    Sub SaveAllStationSettings(ByRef DirectoryPath As String)
        For i = 0 To WXStations.Count - 1
            ConfigManager.SaveSettings(String.Format("{0}\{1}-{2}.txt", DirectoryPath, WXStations(i).Name, WXStations(i).WXStationType), WXStations(i).WXStation.Settings)
        Next
    End Sub

    Sub LoadAllStationSettings(ByRef DirectoryPath As String)
        If My.Computer.FileSystem.DirectoryExists(DirectoryPath) Then
            For Each FilePath As String In My.Computer.FileSystem.GetFiles(DirectoryPath)

                Dim _station() As String = FilePath.Split("\").Last.Split("-")
                _station(1) = _station.Last.Replace(".txt", "")

                AddWXStation(_station(1), _station(0), False)
                ConfigManager.LoadSettings(FilePath, WXStations.Last.WXStation.Settings)
            Next
        End If
    End Sub

    Sub SaveAllStationHigs_Lows(ByRef DirectoryPath As String)
        For i = 0 To WXStations.Count - 1
            SaveStationHighs_Lows(DirectoryPath + "\" + WXStations(i).Name + ".txt", WXStations(i).WXStation.WXData)
        Next
    End Sub
    Sub SaveStationHighs_Lows(ByRef FilePath As String, ByRef _stationData As StationData)
        Dim dataArray As List(Of String) = New List(Of String)
        dataArray.Add(_stationData.TempOutHigh.Title & "," & _stationData.TempOutHigh.Value & "," & _stationData.TempOutHigh.TimeOfValue.ToString())
        dataArray.Add(_stationData.TempOutLow.Title & "," & _stationData.TempOutLow.Value & "," & _stationData.TempOutLow.TimeOfValue.ToString())
        dataArray.Add(_stationData.WindPeakGust.Title & "," & _stationData.WindPeakGust.Value & "," & _stationData.WindPeakGust.TimeOfValue.ToString())
        dataArray.Add(_stationData.BarometerHigh.Title & "," & _stationData.BarometerHigh.Value & "," & _stationData.BarometerHigh.TimeOfValue.ToString())
        dataArray.Add(_stationData.BarometerLow.Title & "," & _stationData.BarometerLow.Value & "," & _stationData.BarometerLow.TimeOfValue.ToString())
        dataArray.Add(_stationData.HumidityOutHigh.Title & "," & _stationData.HumidityOutHigh.Value & "," & _stationData.HumidityOutHigh.TimeOfValue.ToString())
        dataArray.Add(_stationData.HumidityOutLow.Title & "," & _stationData.HumidityOutLow.Value & "," & _stationData.HumidityOutLow.TimeOfValue.ToString())
        File.WriteAllLines(FilePath, dataArray)
    End Sub
    Sub LoadAllStationHigs_Lows(ByRef DirectoryPath As String)
        If Directory.Exists(DirectoryPath) Then
            Dim FileNames() As String = Directory.GetFiles(DirectoryPath)
            For i = 0 To WXStations.Count - 1
                For q = 0 To FileNames.Count - 1
                    If FileNames(i).Split("\").Last.Replace(".txt", "") = WXStations(i).Name Then
                        ReadStationHighs_Lows(FileNames(q), WXStations(i).WXStation.WXData)
                        Exit For
                    End If
                Next
            Next
        End If
    End Sub
    Sub ReadStationHighs_Lows(ByRef FilePath As String, ByRef _stationData As StationData)
        If File.Exists(FilePath) Then
            Dim dataArray() As String = File.ReadAllLines(FilePath)
            For i = 0 To dataArray.Count - 1
                Dim currentItem() As String = dataArray(i).Split(",")
                Select Case currentItem(0)
                    Case _stationData.TempOutHigh.Title
                        TryGetDataVal(currentItem(1), _stationData.TempOutHigh.Value, currentItem(2), _stationData.TempOutHigh.TimeOfValue)
                    Case _stationData.TempOutLow.Title
                        TryGetDataVal(currentItem(1), _stationData.TempOutLow.Value, currentItem(2), _stationData.TempOutLow.TimeOfValue)
                    Case _stationData.WindPeakGust.Title
                        TryGetDataVal(currentItem(1), _stationData.WindPeakGust.Value, currentItem(2), _stationData.WindPeakGust.TimeOfValue)
                    Case _stationData.BarometerHigh.Title
                        TryGetDataVal(currentItem(1), _stationData.BarometerHigh.Value, currentItem(2), _stationData.BarometerHigh.TimeOfValue)
                    Case _stationData.BarometerLow.Title
                        TryGetDataVal(currentItem(1), _stationData.BarometerLow.Value, currentItem(2), _stationData.BarometerLow.TimeOfValue)
                    Case _stationData.HumidityOutHigh.Title
                        TryGetDataVal(currentItem(1), _stationData.HumidityOutHigh.Value, currentItem(2), _stationData.HumidityOutHigh.TimeOfValue)
                    Case _stationData.HumidityOutLow.Title
                        TryGetDataVal(currentItem(1), _stationData.HumidityOutLow.Value, currentItem(2), _stationData.HumidityOutLow.TimeOfValue)
                End Select
            Next
        End If
    End Sub
    Private Sub TryGetDataVal(ByVal val As String, ByRef outVal As Decimal, ByVal timeOfval As DateTime, ByRef outTimeval As DateTime)
        Dim decVal As Decimal = 0.0
        Dim timeVal As DateTime
        If Decimal.TryParse(val, decVal) And DateTime.TryParse(timeOfval, timeVal) Then
            outVal = decVal
            outTimeval = timeOfval
        End If
    End Sub
End Class



' Station Plugins
<ExportMetadata("Name", "PeetBros Ultimeter")>
<ExportMetadata("Type", "PeetBros Ultimeter")>
<ExportMetadata("Version", "1.0b")>
<Export(GetType(IWXStation))>
Public Class PeetBros
#Region "Genaric Station Code"
    Implements IWXStation
    Public Property UpdateInterval_Milliseconds As Integer = 250 Implements IWXStation.UpdateInterval_Milliseconds
    Public Sub ApplySettings(ByVal NewSettings As ObservableCollection(Of Config)) Implements IWXStation.ApplySettings
        'This is used for when the user changes any settings for the station, It gives us a chance to make the needed changes gracefully
        Settings = NewSettings
        Intilize()
    End Sub

    Public Sub CloseStation() Implements IWXStation.Disconnect
        'Close the Serial Port if Its open
        If SerialConnection IsNot Nothing Then
            If SerialConnection.IsOpen Then SerialConnection.Close()
        End If
    End Sub

    Public Property WXData As StationData Implements IWXStation.WXData
        Get
            Return WXStationData
        End Get
        Set(value As StationData)
            WXStationData = value
        End Set
    End Property

    Public Property Settings As ObservableCollection(Of Config) Implements IWXStation.Settings
        Get
            Return StationSettings
        End Get
        Set(value As ObservableCollection(Of Config))
            StationSettings = value
        End Set
    End Property

    Public Property Status As String Implements IWXStation.Status

    Public Property StationErrorMessage As String Implements IWXStation.StationErrorMessage

    Public Sub Update() Implements IWXStation.Update
        If SerialConnection IsNot Nothing AndAlso SerialConnection.IsOpen Then
            SerialPortDataRecived()
        Else
            IntSerialPort()
        End If
    End Sub

#End Region
#Region "Station Specific Code"
#Region "Indexing Enumerations"
    Enum RainIncrements As Integer
        oneHundrethInch = 0
        oneTenthInch = 1
        oneTenthmm = 2
    End Enum
    Enum SettingTitle As Integer
        SerialName = 0
        RainGaugeIncrements = 1
    End Enum
#End Region
    Private SerialConnection As New System.IO.Ports.SerialPort
    Private StationSettings As ObservableCollection(Of Config)
    Private WXStationData As New StationData
    'Private PrecipRateTimer As New Timers.Timer(60000)
    'Private PrecipRateVals As New List(Of Decimal)
    'Private Bar3HrChange As New List(Of Decimal)
    'Private FirstDataUpdate As Boolean = True
    'Private UpdatePrecipTimerData As Boolean = True

    Private Sub Intilize()
        SetupSettings()
        SetDataTypes()
        'IntSerialPort()
    End Sub
    Sub IntSerialPort()
        'We are Going to try intilization code here
        SerialConnection.BaudRate = 2400
        SerialConnection.ReadTimeout = 5000
        SerialConnection.PortName = StationSettings(0).ConfigSettings(0).CurrentValue
        'Make 3 attempts to open the serialconnection
        DebugMessanger.SendMessage(Me.ToString, String.Format("Trying to Open a serial Connection on {0}.", SerialConnection.PortName), "")
        For i = 3 To 0 Step -1
            Try
                If SerialConnection.IsOpen Then
                    Exit For
                Else
                    SerialConnection.Open()
                    DebugMessanger.SendMessage(Me.ToString, String.Format("{0} - Serial Connection Opened.", SerialConnection.PortName), "")
                End If
            Catch ex As Exception
                DebugMessanger.SendMessage(Me.ToString, String.Format("Error Opening Serial Port {0}. Attempting {1} more Times", SerialConnection.PortName, i), "")
                System.Threading.Thread.Sleep(1000)
            End Try
        Next
        If SerialConnection.IsOpen Then
            Status = "Connection Opened"
            StationErrorMessage = Nothing
        Else
            Status = "Disconnected"
        End If
        'We are Reading in the New Station Data with a Event so we don't need to have the program in a LOOP
        'AddHandler SerialConnection.DataReceived, AddressOf SerialPortDataRecived
    End Sub
    Private Sub SetupSettings()
        StationSettings.Add(New Config("Options"))
        StationSettings(0).ConfigSettings.Add(New Setting("SerialName", "COM1"))
        StationSettings(0).ConfigSettings(0).AllowedParameters = API.API.MiscFunctions.GetSerialNames()
        StationSettings(0).ConfigSettings.Add(New Setting("RainGaugeIncrements", RainIncrements.oneHundrethInch.ToString))
    End Sub

    Sub SetDataTypes()
        WXStationData.WindSpeed.UnitType = DataUnitType.Wind_Mph 'Wind Speed
        WXStationData.WindBearing.UnitType = DataUnitType.NOConversions 'Wind Bearing
        WXStationData.TempOutCur.UnitType = DataUnitType.Temp_Fahrenheit 'Outdoor Temp
        WXStationData.RainLongTermTotal.UnitType = DataUnitType.Precip_Inch 'Long Term Rain Total
        WXStationData.BarometerCur.UnitType = DataUnitType.Bar_InHg 'Barometer
        WXStationData.TempInCur.UnitType = DataUnitType.Temp_Fahrenheit 'Indoor Temp
        WXStationData.HumidityOutCur.UnitType = DataUnitType.NOConversions 'Outdoor Humidity
        WXStationData.HumidityIn.UnitType = DataUnitType.NOConversions 'Indoor Humidity
        WXStationData.RainTodayTotal.UnitType = DataUnitType.Precip_Inch 'Today's Rain Total
    End Sub
    
    Sub SerialPortDataRecived()
        If SerialConnection IsNot Nothing And SerialConnection.IsOpen Then
            Status = "Recieving Data.."
            SerialConnection.DiscardInBuffer() 'clear the buffer
            Dim SerialString As String = SerialConnection.ReadLine() 'read a line to get to a CRLF
            SerialString = SerialConnection.ReadLine() ' then get a complete line
            If SerialString.StartsWith("!!") Then
                DataLoggerMode(SerialString.Remove(0, 2))
            ElseIf SerialString.StartsWith("&CR&") Then ' Complete Record Mode
                CompleteRecoredMode(SerialString.Remove(0, 4))
            ElseIf SerialString.StartsWith("~CH~") Then 'Complete History Mode
                CompleteHistoryMode(SerialString.Remove(0, 4))
            End If
            UpdateTheDateTimes()
            WXStationData.LastUpdated = DateTime.Now
            Status = "Updated"
        Else
            DebugMessanger.SendMessage(Me.ToString, String.Format("Error opening Serial Port '{0}'", StationSettings(0).ConfigSettings(0).CurrentValue), "The Serial Port is closed")

        End If
    End Sub
    Private Sub DataLoggerMode(ByVal StationDataString As String)
        WXStationData.WindSpeed.Value = PullDataVal(StationDataString, 0, 4) 'Wind Speed
        WXStationData.WindBearing.Value = PullDataVal(StationDataString, 4, 8) 'Wind Bearing
        'WXStationData(2).Val = 'Wind Direction, ie. NNW SW WSW W E etc.
        WXStationData.TempOutCur.Value = PullDataVal(StationDataString, 8, 12) 'Outdoor Temp
        WXStationData.RainLongTermTotal.Value = PullDataVal(StationDataString, 12, 16) 'Long Term Rain Total
        WXStationData.BarometerCur.Value = PullDataVal(StationDataString, 16, 20) 'Barometer
        WXStationData.TempInCur.Value = PullDataVal(StationDataString, 20, 24) 'Indoor Temp
        WXStationData.HumidityOutCur.Value = PullDataVal(StationDataString, 24, 28) 'Outdoor Humidity
        WXStationData.HumidityIn.Value = PullDataVal(StationDataString, 28, 32) 'Indoor Humidity
        WXStationData.RainTodayTotal.Value = PullDataVal(StationDataString, 40, 44) 'Today's Rain Total
        ConvertToUsableUnits()
    End Sub
    Private Sub CompleteRecoredMode(ByVal StationDataString As String)
        WXStationData.WindSpeed.Value = PullDataVal(StationDataString, 0, 3) 'Wind Speed
        WXStationData.WindBearing.Value = PullDataVal(StationDataString, 4, 7) 'Wind Bearing
        'WXStationData(2).Val = 'Wind Direction, ie. NNW SW WSW W E etc.
        WXStationData.TempOutCur.Value = PullDataVal(StationDataString, 20, 23) 'Outdoor Temp
        WXStationData.RainLongTermTotal.Value = PullDataVal(StationDataString, 428, 431) 'Long Term Rain Total
        WXStationData.BarometerCur.Value = PullDataVal(StationDataString, 28, 31) 'Barometer
        WXStationData.TempInCur.Value = PullDataVal(StationDataString, 44, 47) 'Indoor Temp
        WXStationData.HumidityOutCur.Value = PullDataVal(StationDataString, 48, 51) 'Outdoor Humidity
        WXStationData.HumidityIn.Value = PullDataVal(StationDataString, 52, 55) 'Indoor Humidity
        WXStationData.RainTodayTotal.Value = PullDataVal(StationDataString, 24, 27) 'Today's Rain Total
        ConvertToUsableUnits()
    End Sub
    Private Sub CompleteHistoryMode(StationDataString As String)
        WXStationData.WindSpeed.Value = PullDataVal(StationDataString, 16, 19) 'Wind Speed
        WXStationData.WindBearing.Value = PullDataVal(StationDataString, 20, 23) 'Wind Bearing
        'WXStationData(2).Val = 'Wind Direction, ie. NNW SW WSW W E etc.
        WXStationData.TempOutCur.Value = PullDataVal(StationDataString, 36, 39) 'Outdoor Temp
        WXStationData.RainLongTermTotal.Value = PullDataVal(StationDataString, 1156, 1159) 'Long Term Rain Total
        WXStationData.BarometerCur.Value = PullDataVal(StationDataString, 56, 59) 'Barometer
        WXStationData.TempInCur.Value = PullDataVal(StationDataString, 40, 43) 'Indoor Temp
        WXStationData.HumidityOutCur.Value = PullDataVal(StationDataString, 44, 47) 'Outdoor Humidity
        WXStationData.HumidityIn.Value = PullDataVal(StationDataString, 48, 51) 'Indoor Humidity
        WXStationData.RainTodayTotal.Value = PullDataVal(StationDataString, 52, 56) 'Today's Rain Total
        ConvertToUsableUnits()
    End Sub
    Function PullDataVal(ByRef StationDataString As String, ByVal DataStartIndex As Integer, ByVal DataEndIndex As Integer) As String
        Dim ErrorVal As String = "----"
        If DataEndIndex - DataStartIndex = 1 Then
            ErrorVal = "--"
        End If
        Dim SubString As String = StationDataString.Substring(DataStartIndex, DataEndIndex - DataStartIndex)
        If SubString <> ErrorVal Then

            Return Convert.ToInt16(SubString, 16)
        End If
        Return DataErrorVal
    End Function
    Sub ConvertToUsableUnits()
        If WXStationData.WindSpeed.Value <> DataErrorVal Then
            WXStationData.WindSpeed.Value = Math.Round((Conversion.Kph_To_Mph(WXStationData.WindSpeed.Value / 10)), 1) 'Convert Wind speed From 0.1 Kph to Mph 
        End If
        If WXStationData.WindBearing.Value <> DataErrorVal Then
            WXStationData.WindBearing.Value = Math.Round((CDec(WXStationData.WindBearing.Value) * 1.411764705882353)) 'This Converts the wind-Bearing from a value between 0 & 255 to a value between 0 & 360
            'WXStationData(DataIndex.WindBearing).Value = Conversion.GetWindDirection(WXStationData(DataIndex.WindDirection).Value) 'Get Wind-Bearing
            'Else
            '    WXStationData(DataIndex.WindBearing).Value = DataErrorVal
        End If
        If WXStationData.TempOutCur.Value <> DataErrorVal Then
            WXStationData.TempOutCur.Value /= 10
        End If
        If WXStationData.RainLongTermTotal.Value <> DataErrorVal Then
            If Settings(0).ConfigSettings(SettingTitle.RainGaugeIncrements).CurrentValue = RainIncrements.oneTenthmm.ToString Then
                WXStationData.RainLongTermTotal.Value = Conversion.mm_to_in(WXStationData.RainLongTermTotal.Value / 10)
            ElseIf Settings(0).ConfigSettings(SettingTitle.RainGaugeIncrements).CurrentValue = RainIncrements.oneHundrethInch.ToString Or Settings(0).ConfigSettings(SettingTitle.RainGaugeIncrements).CurrentValue = RainIncrements.oneTenthInch.ToString Then
                WXStationData.RainLongTermTotal.Value /= 100
            End If
        End If
        If WXStationData.RainTodayTotal.Value <> DataErrorVal Then
            If Settings(0).ConfigSettings(SettingTitle.RainGaugeIncrements).CurrentValue = RainIncrements.oneTenthmm.ToString Then
                WXStationData.RainTodayTotal.Value = Conversion.mm_to_in(WXStationData.RainTodayTotal.Value / 10)
            ElseIf Settings(0).ConfigSettings(SettingTitle.RainGaugeIncrements).CurrentValue = RainIncrements.oneHundrethInch.ToString Or Settings(0).ConfigSettings(SettingTitle.RainGaugeIncrements).CurrentValue = RainIncrements.oneTenthInch.ToString Then
                WXStationData.RainTodayTotal.Value *= 0.01
            End If
        End If
        If WXStationData.BarometerCur.Value <> DataErrorVal Then
            WXStationData.BarometerCur.Value = Conversion.mBar_to_inHg(WXStationData.BarometerCur.Value * 0.1)
            'WXStationData.BarometerCur.Value = Conversion.mBar_to_inHg(Conversion.SensorToStationPressure((WXStationData.BarometerCur.Value / 10), Conversion.Feet_To_Meters(940), Conversion.Feet_To_Meters(940), CDbl(Conversion.Fahrenheight_To_Celsius(WXData.TempOutCur.Value))))
        End If
        If WXStationData.TempInCur.Value <> DataErrorVal Then
            WXStationData.TempInCur.Value /= 10
        End If
        If WXStationData.HumidityOutCur.Value <> DataErrorVal Then
            WXStationData.HumidityOutCur.Value /= 10
        End If
        If WXStationData.HumidityIn.Value <> DataErrorVal Then
            WXStationData.HumidityIn.Value /= 10
        End If
        If WXStationData.RainTodayTotal.Value <> DataErrorVal Then
            If Settings(0).ConfigSettings(SettingTitle.RainGaugeIncrements).CurrentValue = "0.1 mm" Then
                WXStationData.RainTodayTotal.Value = Conversion.mm_to_in(WXStationData.RainTodayTotal.Value / 10)
            ElseIf Settings(0).ConfigSettings(SettingTitle.RainGaugeIncrements).CurrentValue = "0.01 In" Or Settings(0).ConfigSettings(SettingTitle.RainGaugeIncrements).CurrentValue = "0.1 In" Then
                WXStationData.RainTodayTotal.Value /= 100
            End If
        End If
    End Sub
    Private Sub UpdateTheDateTimes()
        WXStationData.TempOutCur.TimeOfValue = DateTime.Now
        WXStationData.TempInCur.TimeOfValue = DateTime.Now
        WXStationData.WindSpeed.TimeOfValue = DateTime.Now
        WXStationData.WindDirection.TimeOfValue = DateTime.Now
        WXStationData.BarometerCur.TimeOfValue = DateTime.Now
        WXStationData.HumidityOutCur.TimeOfValue = DateTime.Now
        WXStationData.HumidityIn.TimeOfValue = DateTime.Now
    End Sub
#End Region

End Class

<ExportMetadata("Name", "Davis")>
<ExportMetadata("Type", "Davis")>
<ExportMetadata("Version", "1.0b")>
<Export(GetType(IWXStation))>
Public Class Davis
    Implements IWXStation
    Private TCPConnection As System.Net.Sockets.Socket
    'Private TCPStream As NetworkStream
    Private SerialConnection As Ports.SerialPort

    Private StationSettings As ObservableCollection(Of Config)
    Private WXStationData As New StationData

    Public Sub ApplySettings(ByVal NewConfig As ObservableCollection(Of Config)) Implements IWXStation.ApplySettings
        Settings = NewConfig
        IntilizeDefaultSettings()
    End Sub

    Public Sub CloseStation() Implements IWXStation.Disconnect
        'Close the TCP Connections
        If TCPConnection IsNot Nothing Then
            If TCPConnection.Connected Then
                TCPConnection.Disconnect(False)
            End If
        End If
        'Close the Serial Connections
        If SerialConnection IsNot Nothing Then
            If SerialConnection.IsOpen Then
                SerialConnection.Close()
            End If
        End If
    End Sub

    Public Property Settings As ObservableCollection(Of Config) Implements IWXStation.Settings
        Get
            Return StationSettings
        End Get
        Set(value As ObservableCollection(Of Config))
            StationSettings = value
        End Set
    End Property

    Public Property StationErrorMessage As String Implements IWXStation.StationErrorMessage

    Public Property Status As String Implements IWXStation.Status

    Public Sub Update() Implements IWXStation.Update
        If IntilizeConnection() Then
            'Then we Need to read the data String
            Dim LoopArray() As Byte
            If Settings(0).ConfigSettings(ConfigSettings.ConnectionType).CurrentValue = "TCP" Then
                If TCPConnection IsNot Nothing Then
                    Using TCPStream As NetworkStream = New NetworkStream(TCPConnection)
                        If Wake_Telnet_Vantage(TCPStream) Then
                            LoopArray = Retrieve_Telnet_Command(TCPStream, "LOOP 1", 99)
                            If LoopArray IsNot Nothing Then
                                ProcessData(LoopArray)
                            Else
                                StationErrorMessage = "Lost Connection to Davis Weatherstation"
                            End If
                        Else
                            StationErrorMessage = "Cannot Connect to Davis Weatherstation"
                        End If
                    End Using

                    'WXStation.TCPClientConnection.Close() 'This Could be causing it to only update Data on the first connection
                End If
            ElseIf Settings(0).ConfigSettings(ConfigSettings.ConnectionType).CurrentValue = "Serial" Or Settings(0).ConfigSettings(ConfigSettings.ConnectionType).CurrentValue = "USB" Then
                If SerialConnection IsNot Nothing Then
                    If Wake_Serial_Vantage() Then
                        LoopArray = Retrieve_Serial_Command("LOOP 1", 99)
                        If LoopArray IsNot Nothing Then
                            ProcessData(LoopArray)
                        Else
                            StationErrorMessage = "Lost Connection to Davis Weatherstation"
                        End If
                    Else
                        StationErrorMessage = "Cannot Connect to Davis Weatherstation"
                    End If

                End If

            End If
            'Else 'We know that there was an error while conneting to the Station
        End If
    End Sub

    Public Property WXData As StationData Implements IWXStation.WXData
        Get
            Return WXStationData
        End Get
        Set(value As StationData)
            WXStationData = value
        End Set
    End Property

    Public Property UpdateInterval_Milliseconds As Integer = 3000 Implements IWXStation.UpdateInterval_Milliseconds


#Region "Settings"
    Enum ConfigSettings
        TCPport
        IPAddress
        ConnectionType
        SerialName
        SerialSpeed
    End Enum
    Private Sub IntilizeDefaultSettings()
        'StationSettings = New Config("Options")
        StationSettings = New ObservableCollection(Of Config)
        StationSettings.Add(New Config("Options"))
        StationSettings(0).ConfigSettings.Add(New Setting("TCP Port", "5512"))
        StationSettings(0).ConfigSettings.Add(New Setting("IP Address", "10.0.0.10"))
        StationSettings(0).ConfigSettings.Add(New Setting("Connection Type", "Serial"))
        StationSettings(0).ConfigSettings.Add(New Setting("Serial Name", "COM3"))
        StationSettings(0).ConfigSettings.Add(New Setting("Serial Speed", "19200"))

        StationSettings(0).ConfigSettings(ConfigSettings.SerialName).AllowedParameters = MiscFunctions.GetSerialNames
        StationSettings(0).ConfigSettings(ConfigSettings.ConnectionType).AllowedParameters.AddRange(New String() {"Serial", "TCP", "USB"})
        'For Each Str As String In My.Computer.Ports.SerialPortNames
        '    StationSettings.Settings(ConfigSettings.SerialName).AllowedParameters.Add(Str)
        'Next
        SetDataTypes()
    End Sub
    Private Sub SetDataTypes()
        WXStationData.WindSpeed.UnitType = DataUnitType.Wind_Mph 'Wind Speed
        WXStationData.WindDirection.UnitType = DataUnitType.NOConversions 'Wind Bearing
        WXStationData.TempOutCur.UnitType = DataUnitType.Temp_Fahrenheit 'Outdoor Temp
        WXStationData.RainLongTermTotal.UnitType = DataUnitType.Precip_Inch 'Long Term Rain Total
        WXStationData.BarometerCur.UnitType = DataUnitType.Bar_InHg 'Barometer
        WXStationData.TempInCur.UnitType = DataUnitType.Temp_Fahrenheit 'Indoor Temp
        WXStationData.HumidityOutCur.UnitType = DataUnitType.NOConversions 'Outdoor Humidity
        WXStationData.HumidityIn.UnitType = DataUnitType.NOConversions 'Indoor Humidity
        WXStationData.RainTodayTotal.UnitType = DataUnitType.Precip_Inch 'Today's Rain Total
    End Sub
    Private Sub OpenConnect()
        Status = "Connecting.."
        IntilizeConnection()
    End Sub
#End Region
    Private Function IntilizeConnection() As Boolean
        Select Case Settings(0).ConfigSettings(ConfigSettings.ConnectionType).CurrentValue
            Case "TCP"
                If TCPConnection IsNot Nothing Then
                    If TCPConnection.Connected Then
                        Return True
                    Else
                        Return OpenTCPConnection()
                    End If
                Else
                    Return OpenTCPConnection()
                End If
            Case "Serial"
                If SerialConnection IsNot Nothing Then
                    If Not SerialConnection.IsOpen Then
                        Return OpenSerialConnection()
                    Else
                        Return True
                    End If
                Else
                    Return OpenSerialConnection()
                End If
            Case "USB"
                If SerialConnection IsNot Nothing Then
                    If Not SerialConnection.IsOpen Then
                        Return OpenSerialConnection()
                    Else
                        Return True
                    End If
                Else
                    Return OpenSerialConnection()
                End If
            Case Else
                Return False
        End Select
    End Function
    Private Function OpenTCPConnection() As Boolean
        Try
            Dim TempIPEndPoint As New Net.IPEndPoint(System.Net.IPAddress.Parse(StationSettings(0).ConfigSettings(ConfigSettings.IPAddress).CurrentValue), Settings(0).ConfigSettings(ConfigSettings.TCPport).CurrentValue)
            TCPConnection = New Socket(TempIPEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
            TCPConnection.ReceiveTimeout = 500
            TCPConnection.SendTimeout = 500
            TCPConnection.Connect(TempIPEndPoint)

            Using TCPStream As NetworkStream = New NetworkStream(TCPConnection)
                TCPStream.ReadTimeout = 500
                TCPStream.WriteTimeout = 500

                If Wake_Telnet_Vantage(TCPStream) Then
                    Return True
                Else
                    Return False
                End If
            End Using
        Catch ex As Exception
            'WXStation.UpdateTimerValue = 30
            'FileIO.FileSystem.WriteAllText("Error_log.txt", "At Open TCP Connection  " & DateTime.Now & vbCrLf & ex.Message & vbCrLf, True)
            Status = "Error Opening TCP Connection"
            Return False
        End Try
    End Function
    Private Function OpenSerialConnection() As Boolean
        Try
            SerialConnection = New IO.Ports.SerialPort
            SerialConnection.BaudRate = CInt(Settings(0).ConfigSettings(ConfigSettings.SerialSpeed).CurrentValue)
            SerialConnection.PortName = Settings(0).ConfigSettings(ConfigSettings.SerialName).CurrentValue
            SerialConnection.DtrEnable = True
            SerialConnection.StopBits = Ports.StopBits.One
            SerialConnection.DataBits = 8
            SerialConnection.ReadTimeout = 1200
            SerialConnection.WriteTimeout = 1200
            SerialConnection.NewLine = vbCr
            SerialConnection.Open()
            If Wake_Serial_Vantage() Then
                Return True
            Else
                Return False
            End If
        Catch ex As Exception
            'WXStation.UpdateTimerValue = 30
            'FileIO.FileSystem.WriteAllText("Error_log.txt", "At Open Serial  " & DateTime.Now & vbCrLf & ex.Message & vbCrLf, True)
            Status = "Error Opening Serial Port"
            Return False
        End Try
        Return True
    End Function
    Private Function Wake_Telnet_Vantage(ByRef TCPStream As NetworkStream) As Boolean
        Dim newLineASCII As Byte = 10
        Dim passCount As Integer = 1, maxPasses As Integer = 4

        Try
            'WXStation.TCPStream = WXStation.TCPClientConnection.GetStream()

            ' Put a newline character ('\n') out the serial port
            TCPStream.WriteByte(newLineASCII)
            ' Wait .5 seconds to see if anything's been returned
            System.Threading.Thread.Sleep(500)
            ' Now check and see if anything's been returned.  If nothing, ping the Vantage with another newline.
            While Not TCPStream.DataAvailable AndAlso passCount < maxPasses
                TCPStream.WriteByte(newLineASCII)
                ' The Vantage documentation states that 1.2 seconds is the maximum delay - since we're looping
                ' anyway, let's try somthing more agressive
                System.Threading.Thread.Sleep(500)
                passCount += 1
            End While

            If passCount < maxPasses Then
                Return (True)
            Else
                Return (False)
            End If
        Catch ex As Exception
            'FileIO.FileSystem.WriteAllText("Error_log.txt", "At Wake TCP Station  " & DateTime.Now & vbCrLf & ex.Message & vbCrLf, True)
            Status = "Failed to Wake Station."
            Return False
        End Try
    End Function
    ' Retrieve_Command retrieves data from the Vantage weather station using the specified command
    Private Function Retrieve_Telnet_Command(ByRef TCPStream As NetworkStream, commandString As String, returnLength As Integer) As Byte()
        Dim Found_ACK As Boolean = False
        ' ASCII 6
        Dim currChar As Integer, ACK As Integer = 6, passCount As Integer = 1, maxPasses As Integer = 4
        Dim termCommand As String

        Try
            ' Set a local variable so that it's easier to work with the stream underlying the TCP socket
            'Dim theStream As NetworkStream = thePort.GetStream()

            ' Try the command until we get a clean ACKnowledge from the Vantage.  We count the number of passes since
            ' a timeout will never occur reading from the sockets buffer.  If we try a bunch of times (maxPasses) and
            ' we get nothing back, we assume that the connection is busted
            While Not Found_ACK AndAlso passCount < maxPasses
                termCommand = commandString & vbLf
                ' Convert the command string to an ASCII byte array - required for the .Write method - and send
                'TCPStream.Write(System.Text.Encoding.ASCII.GetBytes(termCommand), 0, termCommand.Length)
                TCPStream.Write(System.Text.Encoding.UTF8.GetBytes(termCommand), 0, termCommand.Length)


                ' According to the Davis documentation, the LOOP command sends its response every 2 seconds.  It's
                ' not clear if there is a 2-second delay for the first response.  My trials have show that this can
                ' move faster, but still needs some delay.
                System.Threading.Thread.Sleep(500)

                ' Wait for the Vantage to acknowledge the the receipt of the command - sometimes we get a '\r\n'
                ' in the buffer first or nor response is given.  If all else fails, try again.
                While TCPStream.DataAvailable AndAlso Not Found_ACK
                    ' Read the current character
                    currChar = TCPStream.ReadByte()
                    If currChar = ACK Then
                        Found_ACK = True
                    End If
                End While

                passCount += 1
            End While

            ' We've tried a bunch of times and have heard nothing back from the port (nothing's in the buffer).  Let's 
            ' bounce outta here
            If passCount = maxPasses Then
                Return (Nothing)
            Else
                ' Allocate a byte array to hold the return data that we care about - up to, but not including the '\n'
                ' Size is determined by LOOP data return - this procedure has no way of knowing if it is not passed in.
                Dim loopString As Byte() = New Byte(returnLength - 1) {}

                ' Wait until the buffer is full - we've received returnLength characters from the command response
                While Not TCPStream.DataAvailable '<= loopString.Length
                    ' Wait a short period to let more data load into the buffer
                    System.Threading.Thread.Sleep(200)
                End While

                ' Read the first 95 bytes of the buffer into the array
                TCPStream.Read(loopString, 0, returnLength)

                Return loopString
            End If
        Catch ex As Exception
            'FileIO.FileSystem.WriteAllText("Error_log.txt", "At Retrive TCP Data  " & DateTime.Now & vbCrLf & ex.Message & vbCrLf, True)
            Status = "Retrive TCP Data error."
            Return Nothing
        End Try
    End Function

    Private Function Wake_Serial_Vantage() As Boolean
        Dim passCount As Integer = 1, maxPasses As Integer = 4

        Try
            ' Clear out both input and output buffers just in case something is in there already
            SerialConnection.DiscardInBuffer()
            SerialConnection.DiscardOutBuffer()

            ' Put a newline character ('\n') out the serial port - the Writeline method terminates with a '\n' of its own
            SerialConnection.WriteLine("")
            ' Wait for 1.2 seconds - this is being very conservative - shorten if things look good
            System.Threading.Thread.Sleep(1200)
            ' Now check and see if anything's been returned.  If nothing, ping the Vantage with another newline.
            While SerialConnection.BytesToRead = 0 AndAlso passCount < maxPasses
                SerialConnection.WriteLine("")
                ' The Vantage documentation states that 1.2 seconds is the maximum delay - wait for that amount of time
                System.Threading.Thread.Sleep(1200)
                passCount += 1
            End While

            ' Vantage found and awakened
            If passCount < maxPasses Then
                ' Now that the Vantage is awake, clean out the input buffer again for good measure.
                SerialConnection.DiscardInBuffer()
                Return True
            Else
                Return False
            End If
        Catch ex As Exception
            'FileIO.FileSystem.WriteAllText("Error_log.txt", "At Wake Serial  " & DateTime.Now & vbCrLf & ex.Message & vbCrLf, True)
            Status = "Failed To wake Serial Station."
            Return False
        End Try
    End Function
    Private Function Retrieve_Serial_Command(ByVal commandString As String, ByVal returnLength As Integer) As Byte()
        Dim Found_ACK As Boolean = False
        ' ASCII 6
        Dim ACK As Integer = 6, passCount As Integer = 1, maxPasses As Integer = 4
        Dim currChar As Integer

        Try
            ' Clean out the input (receive) buffer just in case something showed up in it
            SerialConnection.DiscardInBuffer()
            ' . . . and clean out the output buffer while we're at it for good measure
            SerialConnection.DiscardOutBuffer()

            ' Try the command until we get a clean ACKnowledge from the Vantage.  We count the number of passes since
            ' a timeout will never occur reading from the sockets buffer.  If we try a bunch of times (maxPasses) and
            ' we get nothing back, we assume that the connection is busted
            While Not Found_ACK AndAlso passCount < maxPasses
                SerialConnection.WriteLine(commandString)
                ' I'm using the LOOP command as the baseline here because many its parameters are a superset of
                ' those for other commands.  The most important part of this is that the LOOP command is iterative
                ' and the station waits 2 seconds between its responses.  Although it's not clear from the documentation, 
                ' I'm assuming that the first packet isn't sent for 2 seconds.  In any event, the conservative nature
                ' of waiting this amount of time probably makes sense to deal with serial IO in this manner anyway.
                System.Threading.Thread.Sleep(2000)

                ' Wait for the Vantage to acknowledge the the receipt of the command - sometimes we get a '\n\r'
                ' in the buffer first or nor response is given.  If all else fails, try again.
                While SerialConnection.BytesToRead > 0 AndAlso Not Found_ACK
                    ' Read the current character
                    currChar = SerialConnection.ReadChar()
                    If currChar = ACK Then
                        Found_ACK = True
                    End If
                End While

                passCount += 1
            End While

            ' We've tried a bunch of times and have heard nothing back from the port (nothing's in the buffer).  Let's 
            ' bounce outta here
            If passCount = maxPasses Then
                Return Nothing
            Else
                ' Allocate a byte array to hold the return data that we care about - up to, but not including the '\n'
                ' Size the array according to the data passed to the procedure
                Dim loopString As Byte() = New Byte(returnLength - 1) {}

                ' Wait until the buffer is full - we've received returnLength characters from the LOOP response, 
                ' including the final '\n' 
                While SerialConnection.BytesToRead < loopString.Length
                    ' Wait a short period to let more data load into the buffer
                    System.Threading.Thread.Sleep(200)
                End While

                ' Read the first returnLength bytes of the buffer into the array
                SerialConnection.Read(loopString, 0, returnLength)

                Return loopString
            End If
        Catch ex As Exception
            'WxStation.UpdateTimerValue = 30
            'FileIO.FileSystem.WriteAllText("Error_log.txt", "At Retrive Serial Data  " & DateTime.Now & vbCrLf & ex.Message & vbCrLf, True)
            Status = "Retrive Serial Data error."
            Return Nothing
        End Try
    End Function
    Private Sub ProcessData(ByRef DataToProcess As Byte())
        'Now Here's Where We start Pullling The Data values out of the recived data string
        If DataToProcess.Count = 99 Then 'Check to see if the data string contains anything
            Dim CRC_Val As UShort = CRC_Check(DataToProcess)
            If CRC_Val = 0 Then
                WXData.TempOutCur.Value = BitConverter.ToInt16(DataToProcess, 12) * 0.1 'Out Temp
                WXData.TempOutCur.TimeOfValue = DateTime.Now
                WXData.TempInCur.Value = BitConverter.ToInt16(DataToProcess, 9) * 0.1 'In Temp
                WXData.TempInCur.TimeOfValue = DateTime.Now
                WXData.WindSpeed.Value = Convert.ToInt32(DataToProcess(14)) 'Wind Speed
                WXData.WindSpeed.TimeOfValue = DateTime.Now
                WXData.WindBearing.Value = BitConverter.ToInt16(DataToProcess, 16) 'Wind Bearing
                WXData.WindBearing.TimeOfValue = DateTime.Now
                'WXData.WindDirection.Value = Conversion.GetWindDirection(WXData.WindBearing.Value) 'Wind Direction
                'WXData.WindDirection.TimeOfValue = DateTime.Now
                WXData.BarometerCur.Value = Decimal.Round(Convert.ToDecimal(BitConverter.ToInt16(DataToProcess, 7) / 1000), 2)
                'WXData.BarometerCur.Value = Conversion.mBar_to_inHg(Conversion.SeaLevelToStationPressure(Conversion.inHg_to_mBar(BitConverter.ToInt16(DataToProcess, 7) / 1000), Conversion.Feet_To_Meters(920), Conversion.Fahrenheight_To_Celsius(CDbl(WXData.TempOutCur.Value)), Conversion.Fahrenheight_To_Celsius(CDbl(WXData.TempOutCur.Value)), CByte(DataToProcess(33)))) 'Barometer
                WXData.BarometerCur.TimeOfValue = DateTime.Now
                WXData.RainTodayTotal.Value = BitConverter.ToInt16(DataToProcess, 50) * 0.01    'Today's Precip
                WXData.RainTodayTotal.TimeOfValue = DateTime.Now
                WXData.RainLongTermTotal.Value = BitConverter.ToInt16(DataToProcess, 54) * 0.01 'Total Precip This Year
                WXData.RainLongTermTotal.TimeOfValue = DateTime.Now
                WXData.HumidityOutCur.Value = Convert.ToInt32(DataToProcess(33)) 'OHumidity
                WXData.HumidityOutCur.TimeOfValue = DateTime.Now


                If Convert.ToInt32(DataToProcess(43)) <> 255 Then
                    WXData.UVIndex.Value = Convert.ToInt32(DataToProcess(43)) * 0.1 'UV Index
                    WXData.UVIndex.TimeOfValue = DateTime.Now
                End If

                WXData.SolarRaidiation.Value = BitConverter.ToInt16(DataToProcess, 44) 'Solar Radiation
                WXData.SolarRaidiation.TimeOfValue = DateTime.Now

                WXData.LastUpdated = DateTime.Now
                Status = "Updated"
            Else
                'FileIO.FileSystem.WriteAllText("Error_log.txt", "At Data Processing  " & DateTime.Now & vbCrLf & "CRC Check Failed. CRC=" & CRC_Val & " Data String=", True)
                'For i = 0 To DataToProcess.Count - 1
                '    FileIO.FileSystem.WriteAllBytes("Error_log.txt", DataToProcess, True)
                'Next
                'FileIO.FileSystem.WriteAllText("Error_log.txt", vbCrLf, True)
                Status = "CRC Check failed"
            End If
        Else
            'FileIO.FileSystem.WriteAllText("Error_log.txt", "At Data Processing  " & DateTime.Now & vbCrLf & "Record length was invalid   " & DataToProcess.Count & vbCrLf, True)
            Status = "Data Length Is Invalid."
        End If
    End Sub
    Private Shared CRC_Table() As UShort = {&H0, &H1021, &H2042, &H3063, &H4084, &H50A5, &H60C6, &H70E7,
        &H8108, &H9129, &HA14A, &HB16B, &HC18C, &HD1AD, &HE1CE, &HF1EF,
        &H1231, &H210, &H3273, &H2252, &H52B5, &H4294, &H72F7, &H62D6,
        &H9339, &H8318, &HB37B, &HA35A, &HD3BD, &HC39C, &HF3FF, &HE3DE,
        &H2462, &H3443, &H420, &H1401, &H64E6, &H74C7, &H44A4, &H5485,
        &HA56A, &HB54B, &H8528, &H9509, &HE5EE, &HF5CF, &HC5AC, &HD58D,
        &H3653, &H2672, &H1611, &H630, &H76D7, &H66F6, &H5695, &H46B4,
        &HB75B, &HA77A, &H9719, &H8738, &HF7DF, &HE7FE, &HD79D, &HC7BC,
        &H48C4, &H58E5, &H6886, &H78A7, &H840, &H1861, &H2802, &H3823,
        &HC9CC, &HD9ED, &HE98E, &HF9AF, &H8948, &H9969, &HA90A, &HB92B,
        &H5AF5, &H4AD4, &H7AB7, &H6A96, &H1A71, &HA50, &H3A33, &H2A12,
        &HDBFD, &HCBDC, &HFBBF, &HEB9E, &H9B79, &H8B58, &HBB3B, &HAB1A,
        &H6CA6, &H7C87, &H4CE4, &H5CC5, &H2C22, &H3C03, &HC60, &H1C41,
        &HEDAE, &HFD8F, &HCDEC, &HDDCD, &HAD2A, &HBD0B, &H8D68, &H9D49,
        &H7E97, &H6EB6, &H5ED5, &H4EF4, &H3E13, &H2E32, &H1E51, &HE70,
        &HFF9F, &HEFBE, &HDFDD, &HCFFC, &HBF1B, &HAF3A, &H9F59, &H8F78,
        &H9188, &H81A9, &HB1CA, &HA1EB, &HD10C, &HC12D, &HF14E, &HE16F,
        &H1080, &HA1, &H30C2, &H20E3, &H5004, &H4025, &H7046, &H6067,
        &H83B9, &H9398, &HA3FB, &HB3DA, &HC33D, &HD31C, &HE37F, &HF35E,
        &H2B1, &H1290, &H22F3, &H32D2, &H4235, &H5214, &H6277, &H7256,
        &HB5EA, &HA5CB, &H95A8, &H8589, &HF56E, &HE54F, &HD52C, &HC50D,
        &H34E2, &H24C3, &H14A0, &H481, &H7466, &H6447, &H5424, &H4405,
        &HA7DB, &HB7FA, &H8799, &H97B8, &HE75F, &HF77E, &HC71D, &HD73C,
        &H26D3, &H36F2, &H691, &H16B0, &H6657, &H7676, &H4615, &H5634,
        &HD94C, &HC96D, &HF90E, &HE92F, &H99C8, &H89E9, &HB98A, &HA9AB,
        &H5844, &H4865, &H7806, &H6827, &H18C0, &H8E1, &H3882, &H28A3,
        &HCB7D, &HDB5C, &HEB3F, &HFB1E, &H8BF9, &H9BD8, &HABBB, &HBB9A,
        &H4A75, &H5A54, &H6A37, &H7A16, &HAF1, &H1AD0, &H2AB3, &H3A92,
        &HFD2E, &HED0F, &HDD6C, &HCD4D, &HBDAA, &HAD8B, &H9DE8, &H8DC9,
        &H7C26, &H6C07, &H5C64, &H4C45, &H3CA2, &H2C83, &H1CE0, &HCC1,
        &HEF1F, &HFF3E, &HCF5D, &HDF7C, &HAF9B, &HBFBA, &H8FD9, &H9FF8,
        &H6E17, &H7E36, &H4E55, &H5E74, &H2E93, &H3EB2, &HED1, &H1EF0}
    Private Shared Function CRC_Check(ByRef Data_Array As Byte()) As UShort
        Dim crc As UShort = 0
        For Each Tempbyte As Byte In Data_Array
            'crc = (CRC_Table((crc >> 8) ^ Tempbyte) ^ ((crc & &HFF) << 8))
            crc = CRC_Table((crc >> 8) Xor Tempbyte) Xor ((crc And &HFF) << 8)
        Next
        Return crc
    End Function
End Class