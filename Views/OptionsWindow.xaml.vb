Imports System.Drawing.Imaging

Public Class OptionsWindow
    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        AddinWx_Config.DataContext = MainWindow._config(0)
        Me.GlobalSettingsTemp.DataContext = API.API.ConfigManager._globalConfig.GlobalSettings(0).ConfigSettings(API.API.ConfigManager.GlobalConfig.configIndexes.Temp)
        Me.GlobalSettingsWind.DataContext = API.API.ConfigManager._globalConfig.GlobalSettings(0).ConfigSettings(API.API.ConfigManager.GlobalConfig.configIndexes.Wind)
        Me.GlobalSettingsBar.DataContext = API.API.ConfigManager._globalConfig.GlobalSettings(0).ConfigSettings(API.API.ConfigManager.GlobalConfig.configIndexes.Rain)
        Me.GlobalSettingsRain.DataContext = API.API.ConfigManager._globalConfig.GlobalSettings(0).ConfigSettings(API.API.ConfigManager.GlobalConfig.configIndexes.Bar)
        Me.GlobalSettingsLength.DataContext = API.API.ConfigManager._globalConfig.GlobalSettings(0).ConfigSettings(API.API.ConfigManager.GlobalConfig.configIndexes.Length)

        Stations_Config.DataContext = MainWindow.StationManager.WXStations
        Views_Config.DataContext = MainWindow.ViewManager.Views
        Background_Config.DataContext = MainWindow.BackgroundPluginManager.BackgroundPlugins
    End Sub

    Private Sub OnDragMoveWindow(sender As Object, e As MouseButtonEventArgs)
        Me.DragMove()
    End Sub

    Private Sub WindowCloseButton_Click(sender As Object, e As RoutedEventArgs)
        'Me.Close()
    End Sub

    Private Sub Button_Click(sender As Object, e As RoutedEventArgs)
        Dim x As New AddStationDialog
        x.Owner = Window.GetWindow(Me)
        x.DataContext = MainWindow.StationManager
        x.ShowDialog()
    End Sub

    Private Sub StationSelectionChanged(sender As Object, e As SelectionChangedEventArgs)
        ConfigGroupComboBx.SelectedIndex = 0
    End Sub

    Private Sub ViewsSelectionChanged(sender As Object, e As SelectionChangedEventArgs)
        ConfigGroupComboBx2.SelectedIndex = 0
    End Sub

    Private Sub PluginSelectionChanged(sender As Object, e As SelectionChangedEventArgs)
        ConfigGroupComboBx3.SelectedIndex = 0
    End Sub

    Private Sub RemoveStationClick(sender As Object, e As RoutedEventArgs)
        Dim button As Button = sender
        Dim clickedListItem As ListBoxItem = NamesList.ItemContainerGenerator.ContainerFromItem(button.DataContext)
        clickedListItem.IsSelected = True
        Dim _SelectedIndex As Integer = NamesList.SelectedIndex
        MainWindow.StationManager.RemoveStation(MainWindow.StationManager.WXStations(NamesList.SelectedIndex).Name)

        If _SelectedIndex = -1 Then
            NamesList.SelectedIndex = 0
        ElseIf _SelectedIndex > NamesList.Items.Count - 1 Then
            NamesList.SelectedIndex = NamesList.Items.Count - 1
        Else
            NamesList.SelectedIndex = _SelectedIndex
        End If
    End Sub

    Private Sub Background_Plugin_Manual_Update(sender As Object, e As RoutedEventArgs)
        Me.Cursor = Cursors.Wait
        MainWindow.BackgroundPluginManager.BackgroundPlugins(NamesList3.SelectedIndex).Update()
        Me.Cursor = Cursors.Arrow
    End Sub

    Private Sub BrowseImages_Click(sender As Object, e As RoutedEventArgs)
        Dim FileBrowser As New Microsoft.Win32.OpenFileDialog()

        FileBrowser.Filter = ""
        Dim codecs As ImageCodecInfo() = ImageCodecInfo.GetImageEncoders()
        Dim sep As String = String.Empty
        For Each c As ImageCodecInfo In codecs
            Dim codecName As String = c.CodecName.Substring(8).Replace("Codec", "Files").Trim()
            FileBrowser.Filter = String.Format("{0}{1}{2} ({3})|{3}", FileBrowser.Filter, sep, codecName, c.FilenameExtension)
            sep = "|"
        Next
        'FileBrowser.Filter = String.Format("{0}{1}{2} ({3})|{3}", FileBrowser.Filter, sep, "All Files", "*.*")


        'FileBrowser.Filter = "Image files (*.BMP;*.PNG;*.JPEG;*.JPG;*.TIFF)|*.BMP;*.PNG;*.JPEG;*.JPG*.TIFF"

        If FileBrowser.ShowDialog(Me) = True Then
            MainWindow._config(0).ConfigSettings(MainWindow.configGeneralIndexes.BackgroundImagePath).CurrentValue = FileBrowser.FileName
        End If

    End Sub
End Class
