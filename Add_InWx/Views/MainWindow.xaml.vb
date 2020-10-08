Imports API.API
Imports System.Reflection
Imports System.Collections.ObjectModel

Public Class MainWindow
    Public Shared ViewManager As ViewsManager
    Public Shared StationManager As StationsManger
    Public Shared BackgroundPluginManager As BackgroundPluginManager
    Public Shared MainSettingsDirectory As String = My.Computer.FileSystem.SpecialDirectories.MyDocuments & "\Add-InWx" 'Main Program Settings and Plugins Folder

    Public Shared StationPluginFolderPath As String = MainSettingsDirectory & "\Plugins" ' Directory for the Weather Station Plugins
    Public Shared StationSettingsDirectory As String = MainSettingsDirectory & "\Stations" 'Directory for the Station Settings
    Public Shared StationDataDirectory As String = MainSettingsDirectory & "\Station Data" 'Directory for the Station Settings
    Public Shared ViewsPluginFolderPath As String = MainSettingsDirectory & "\Plugins" 'Directory for the Views Plugins
    Public Shared ViewsSettingsDirectory As String = MainSettingsDirectory & "\Views Settings" 'Directory for the Views Settings
    Public Shared BackgroundPluginFolderPath As String = MainSettingsDirectory & "\Plugins" 'Directory for the Views Plugins
    Public Shared BackgroundPluginSettingsDirectory As String = MainSettingsDirectory & "\Background Plugin Settings" 'Directory for the Views Settings
    Private currentDevVersion As String = "1.0.2.3"
    Public Shared _config As New ObservableCollection(Of Config)

    Public Property Settings As ObservableCollection(Of Config)
        Get
            Return _config
        End Get
        Set(value As ObservableCollection(Of Config))
            _config = value
        End Set
    End Property
    Public Enum configGeneralIndexes
        BackgroundImagePath
    End Enum

    Public Enum configPathsIndexes
        SettingsDirectory
        StationPluginsPath
        StationSettingsPath
        ViewsPluginsPath
        ViewSettingsPath
        BackgroundPluginsPath
        BackgroundPluginSettingsPath
    End Enum
    'Other Windows
    Public Shared Options_Window As OptionsWindow
    Public Shared DebugWindow As Debug_Output
    Public Shared Credits_Window As CreditsWindow
    'Private MainProgramSettings As Config
    Sub New()
        ' This call is required by the designer.
        InitializeComponent()
        ' Add any initialization after the InitializeComponent() call.
        MediaTimeline.DesiredFrameRateProperty.OverrideMetadata(GetType(System.Windows.Media.Animation.Timeline), New FrameworkPropertyMetadata(25))
        IntilizeSettings()
        AddHandler _config(0).ConfigSettings(0).PropertyChanged, AddressOf PropChanged
        StationManager = New StationsManger
        ViewManager = New ViewsManager
        BackgroundPluginManager = New BackgroundPluginManager
        ViewManager.InitilizeViews(StationManager.WXStations, StationManager)
        BackgroundPluginManager.IntilizeBackgroundPlugins(StationManager.WXStations)
        ViewsTab.DataContext = ViewManager.Views

        API.API.DebugMessanger.SendMessage(Me.ToString(), "Add-InWx Started", "")
        Me.MainTitleBar.DataContext = StationManager.WXStations

        Me.DataContext = StationManager

        Dim obj As Version = New Version
        If System.Deployment.Application.ApplicationDeployment.IsNetworkDeployed Then
            obj = System.Deployment.Application.ApplicationDeployment.CurrentDeployment.CurrentVersion
        Else
            obj = New Version(currentDevVersion)
        End If

        Me.Title = String.Format("Add-In Weather [{0}] {1}", obj.ToString, "Beta")
        LoadMainSettings()
        IntWindowLocation()
    End Sub
    Private Sub IntWindowLocation()
        Select Case _config(0).ConfigSettings(1).CurrentValue
            Case CustomWindowMode.FullScreen.ToString
                MainUI.WindowMode.IsChecked = True
                SetWindowFullScreen()
            Case CustomWindowMode.Maximized.ToString
                MainUI.WindowState = Windows.WindowState.Maximized
            Case CustomWindowMode.Windowed.ToString
                MainUI.WindowState = Windows.WindowState.Normal
                MainUI.Top = Convert.ToDecimal(_config(1).ConfigSettings(0).CurrentValue)
                MainUI.Left = Convert.ToDecimal(_config(1).ConfigSettings(1).CurrentValue)
                MainUI.Width = Convert.ToDecimal(_config(1).ConfigSettings(2).CurrentValue)
                MainUI.Height = Convert.ToDecimal(_config(1).ConfigSettings(3).CurrentValue)
        End Select
    End Sub
    Private Sub SetWindowFullScreen()
        MainUI.ResizeMode = Windows.ResizeMode.NoResize
        MainUI.WindowStyle = Windows.WindowStyle.None
        MainUI.WindowState = Windows.WindowState.Normal
        MainUI.WindowState = Windows.WindowState.Maximized
    End Sub
    Private Sub UnSetWindowFullScreen()
        MainUI.ResizeMode = Windows.ResizeMode.CanResize
        MainUI.WindowStyle = Windows.WindowStyle.SingleBorderWindow
        MainUI.WindowState = Windows.WindowState.Maximized
    End Sub
    Enum CustomWindowMode
        Windowed
        Maximized
        FullScreen
    End Enum
    Private Sub IntilizeSettings()
        _config.Add(New Config("General Settings"))
        _config(0).ConfigSettings.Add(New Setting("Background Image Path", ""))
        _config(0).ConfigSettings.Add(New Setting("Window Startup Mode", CustomWindowMode.Maximized.ToString))
        _config(0).ConfigSettings(1).AllowedParameters = New List(Of String) From {CustomWindowMode.Maximized.ToString, CustomWindowMode.FullScreen.ToString, CustomWindowMode.Windowed.ToString}

        _config.Add(New Config("Window Startup Location"))
        _config(1).ConfigSettings.Add(New Setting("Start Location Top", "0"))
        _config(1).ConfigSettings.Add(New Setting("Start Location Left", "0"))
        _config(1).ConfigSettings.Add(New Setting("Window Width", "856"))
        _config(1).ConfigSettings.Add(New Setting("Window Height", "593"))
        '_config.Add(New Config("Direcory Paths"))
        '_config(1).ConfigSettings.Add(New Setting("Station Plugins Path", ""))
        '_config(1).ConfigSettings.Add(New Setting("Station Settings Path", ""))
        '_config(1).ConfigSettings.Add(New Setting("Views Plugins Path", ""))
        '_config(1).ConfigSettings.Add(New Setting("View Settings Path", ""))
        '_config(1).ConfigSettings.Add(New Setting("Background Plugins Path", ""))
        '_config(1).ConfigSettings.Add(New Setting("Background Plugin Settings Path", ""))
        'Me.SetBinding(ImageBrush.ImageSourceProperty, _config(0).ConfigSettings(0).CurrentValue)
        'MainBackground.ImageSource = New Binding(_config(0).ConfigSettings(0).CurrentValue)
        ConfigManager.intilizeGlobalConfig()
        ''_config.Add(ConfigManager._globalConfig.GlobalSettings(0)) ' add the Global Config Object to the Program Options so its more Intuitive for the end User
    End Sub
    Private Sub MainWindow_Loaded(sender As Object, e As RoutedEventArgs)
        StationManager.LoadAllStationSettings(StationSettingsDirectory)
        StationManager.LoadAllStationHigs_Lows(StationDataDirectory)
        BackgroundPluginManager.LoadAllPluginSettings(BackgroundPluginSettingsDirectory)
        ViewManager.LoadAllViewSettings(ViewsSettingsDirectory)
        ViewsList.SelectedIndex = 0
        If StationManager.WXStations.Count - 1 = -1 Then
            ShowOptionsWindow()
        End If
        StationManager.StartStationsUpdating()
    End Sub
    Private Sub Window_Closing(sender As Object, e As ComponentModel.CancelEventArgs)
        BackgroundPluginManager.ShutdownPlugins()
        StationManager.Disconnect()
    End Sub

    Private Sub WindowMode_Click(sender As Object, e As RoutedEventArgs)
        If WindowMode.IsChecked Then
            SetWindowFullScreen()
        Else
            UnSetWindowFullScreen()
        End If
    End Sub

    Public Sub SaveAllSettingsToFiles()
        'Main Settings Directory
        If My.Computer.FileSystem.DirectoryExists(MainSettingsDirectory) = False Then
            My.Computer.FileSystem.CreateDirectory(MainSettingsDirectory)
        End If
        'Station Settings Folder
        If My.Computer.FileSystem.DirectoryExists(StationSettingsDirectory) Then
            My.Computer.FileSystem.DeleteDirectory(StationSettingsDirectory, FileIO.DeleteDirectoryOption.DeleteAllContents)
        End If
        My.Computer.FileSystem.CreateDirectory(StationSettingsDirectory)
        'Station Data Folder
        If My.Computer.FileSystem.DirectoryExists(StationDataDirectory) Then
            My.Computer.FileSystem.DeleteDirectory(StationDataDirectory, FileIO.DeleteDirectoryOption.DeleteAllContents)
        End If
        My.Computer.FileSystem.CreateDirectory(StationDataDirectory)
        'View Settings Folder
        If My.Computer.FileSystem.DirectoryExists(ViewsSettingsDirectory) Then
            My.Computer.FileSystem.DeleteDirectory(ViewsSettingsDirectory, FileIO.DeleteDirectoryOption.DeleteAllContents)
        End If
        My.Computer.FileSystem.CreateDirectory(ViewsSettingsDirectory)
        'Background Plugin Settings Folder
        If My.Computer.FileSystem.DirectoryExists(BackgroundPluginSettingsDirectory) Then
            My.Computer.FileSystem.DeleteDirectory(BackgroundPluginSettingsDirectory, FileIO.DeleteDirectoryOption.DeleteAllContents)
        End If
        My.Computer.FileSystem.CreateDirectory(BackgroundPluginSettingsDirectory)

        StationManager.SaveAllStationSettings(StationSettingsDirectory)
        StationManager.SaveAllStationHigs_Lows(StationDataDirectory)
        ViewManager.SaveAllViewSettings(ViewsSettingsDirectory)
        BackgroundPluginManager.SaveAllPluginSettings(BackgroundPluginSettingsDirectory)
        SaveWindowStateSettings()
    End Sub
    Private Sub SaveWindowStateSettings()
        'If _config(0).ConfigSettings(1).CurrentValue IsNot CustomWindowMode.FullScreen.ToString And Me.WindowStyle <> Windows.WindowStyle.None Then
        '    If MainUI.WindowState = Windows.WindowState.Maximized Then
        '        _config(0).ConfigSettings(1).CurrentValue = CustomWindowMode.Maximized.ToString
        '    ElseIf Me.WindowState = Windows.WindowState.Normal Then
        '        _config(0).ConfigSettings(1).CurrentValue = CustomWindowMode.Windowed.ToString
        '        _config(1).ConfigSettings(0).CurrentValue = MainUI.Top
        '        _config(1).ConfigSettings(1).CurrentValue = MainUI.Left
        '        _config(1).ConfigSettings(2).CurrentValue = MainUI.Width
        '        _config(1).ConfigSettings(3).CurrentValue = MainUI.Height
        '    End If
        'End If
        '_config(0).ConfigSettings(1).CurrentValue = CustomWindowMode.Windowed.ToString
        _config(1).ConfigSettings(0).CurrentValue = MainUI.Top
        _config(1).ConfigSettings(1).CurrentValue = MainUI.Left
        _config(1).ConfigSettings(2).CurrentValue = MainUI.Width
        _config(1).ConfigSettings(3).CurrentValue = MainUI.Height
        ConfigManager.SaveSettings(MainSettingsDirectory & "\MainSettings.txt", _config)
    End Sub
    Private Sub LoadMainSettings()
        ConfigManager.LoadSettings(MainSettingsDirectory & "\MainSettings.txt", _config)
    End Sub
    Private Sub ShowOptionsWindow()
        Options_Window = New OptionsWindow
        Options_Window.Owner = Window.GetWindow(Me)
        AddHandler Options_Window.WindowCloseButton.Click, AddressOf Options_Window_Closing
        Options_Window.ShowDialog()
    End Sub
    Private Sub Options_Window_Closing(sender As Object, e As System.Windows.RoutedEventArgs)
        SaveWindowStateSettings()
        'SaveAllSettingsToFiles()
        StationManager.ReConnectAllStations()
        Options_Window.Close()
    End Sub

    Private Sub Button_Click(sender As Object, e As EventArgs)
        ShowOptionsWindow()
    End Sub

    Private Sub DebugOutput_Click(sender As Object, e As RoutedEventArgs)
        DebugWindow = New Debug_Output
        DebugWindow.Owner = Window.GetWindow(Me)
        DebugWindow.Show()
    End Sub

    Private Sub PropChanged(sender As Object, e As ComponentModel.PropertyChangedEventArgs)
        If e.PropertyName = "CurrentValue" Then
            If System.IO.File.Exists(_config(0).ConfigSettings(0).CurrentValue) Then
                'Me.MainBackground.ImageSource = New BitmapImage(New Uri(_config(0).ConfigSettings(0).CurrentValue))
                Me.Background = New ImageBrush(New BitmapImage(New Uri(_config(0).ConfigSettings(0).CurrentValue)))
            Else
                Me.Background = Me.FindResource("DefaultBackgroundImageBrush")
                'Me.Background = New ImageBrush(CType(Me.FindResource("MainBackgroundImage"), BitmapImage))
            End If
        End If
    End Sub

    Private Sub Button_Click_1(sender As Object, e As RoutedEventArgs)
        Credits_Window = New CreditsWindow
        Credits_Window.Owner = Window.GetWindow(Me)
        Credits_Window.Show()
    End Sub
End Class

<ValueConversion(GetType(Boolean), GetType(WindowStyle))>
Public Class InvertedBoolToWindowStyle
    Implements IValueConverter
    Enum Parameters
        Normal
        Inverted
    End Enum
    Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As Globalization.CultureInfo) As Object Implements IValueConverter.Convert
        Dim boolValue As Boolean = CType(value, Boolean)
        Dim direction = DirectCast([Enum].Parse(GetType(Parameters), DirectCast(parameter, String)), Parameters)
        If direction = Parameters.Inverted Then
            Return If(Not boolValue, WindowStyle.None, WindowStyle.SingleBorderWindow)
        End If
        Return If(boolValue, WindowStyle.SingleBorderWindow, WindowStyle.None)
    End Function

    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As Globalization.CultureInfo) As Object Implements IValueConverter.ConvertBack
        Return Nothing
    End Function
End Class

<ValueConversion(GetType(Boolean), GetType(Boolean))>
Public Class BoolToInvertedBool
    Implements IValueConverter
    Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As Globalization.CultureInfo) As Object Implements IValueConverter.Convert
        Dim boolValue As Boolean = CType(value, Boolean)
        If boolValue = False Then
            Return True
        End If
        Return False
    End Function

    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As Globalization.CultureInfo) As Object Implements IValueConverter.ConvertBack
        Return Nothing
    End Function
End Class

Public NotInheritable Class CustomBooleanToVisibilityConverter
    Inherits BooleanConverter(Of Visibility)
    Public Sub New()
        MyBase.New(Visibility.Visible, Visibility.Collapsed)
    End Sub
End Class
Public Class BooleanConverter(Of T)
    Implements IValueConverter
    Public Sub New(trueValue As T, falseValue As T)
        [True] = trueValue
        [False] = falseValue
    End Sub

    Public Property [True]() As T
        Get
            Return m_True
        End Get
        Set(value As T)
            m_True = value
        End Set
    End Property
    Private m_True As T
    Public Property [False]() As T
        Get
            Return m_False
        End Get
        Set(value As T)
            m_False = value
        End Set
    End Property
    Private m_False As T

    Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As Globalization.CultureInfo) As Object Implements IValueConverter.Convert
        Return If(TypeOf value Is Boolean AndAlso CBool(value), [True], [False])
    End Function

    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As Globalization.CultureInfo) As Object Implements IValueConverter.ConvertBack
        Return TypeOf value Is T AndAlso EqualityComparer(Of T).[Default].Equals(DirectCast(value, T), [True])
    End Function

End Class

<ValueConversion(GetType(API.API.DataUnitType), GetType(String))>
Class FromUnitTypeToDisplay
    Implements IValueConverter

    Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As Globalization.CultureInfo) As Object Implements IValueConverter.Convert
        If value = "" Then
            Return "Unkown"
        End If
        Dim val As String = CType(value, String)
        Return val.Split("_").Last
    End Function

    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As Globalization.CultureInfo) As Object Implements IValueConverter.ConvertBack
        Return Nothing
    End Function
End Class
