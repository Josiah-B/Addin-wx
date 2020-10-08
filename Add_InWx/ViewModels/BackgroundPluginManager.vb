Imports System.ComponentModel.Composition
Imports API.API
Imports System.ComponentModel.Composition.Hosting

Public Class BackgroundPluginManager
    <ImportMany(GetType(IBackgroundPlugin))>
    Public BackgroundPlugins As IEnumerable(Of IBackgroundPlugin)
    Private Comp_Container As CompositionContainer
    Private Comp_Catalog As New AggregateCatalog
    Sub New()

        Comp_Catalog.Catalogs.Add(New AssemblyCatalog(GetType(MainWindow).Assembly))
        Try
            If My.Computer.FileSystem.DirectoryExists(MainWindow.BackgroundPluginFolderPath) Then
                Comp_Catalog.Catalogs.Add(New DirectoryCatalog(MainWindow.BackgroundPluginFolderPath))
            Else
                My.Computer.FileSystem.CreateDirectory(MainWindow.BackgroundPluginFolderPath)
            End If
            Comp_Container = New CompositionContainer(Comp_Catalog)
            Comp_Container.ComposeParts(Me)
        Catch ex As Exception
            Debug.Print(ex.Message)
        End Try
    End Sub
    Public Sub IntilizeBackgroundPlugins(WXStations As ObjectModel.ObservableCollection(Of Station))
        If BackgroundPlugins.Count > 0 Then
            For i = 0 To BackgroundPlugins.Count - 1
                BackgroundPlugins(i).Initilize(WXStations)
            Next
        End If
    End Sub
    Public Sub ShutdownPlugins()
        For i = 0 To BackgroundPlugins.Count - 1
            BackgroundPlugins(i).Close()
        Next
    End Sub
    Public Sub SaveAllPluginSettings(ByRef SettingsFolderPath As String)
        For i = 0 To BackgroundPlugins.Count - 1
            ConfigManager.SaveSettings(String.Format("{0}\{1}.txt", SettingsFolderPath, BackgroundPlugins(i).Name), BackgroundPlugins(i).Configuration)
        Next
    End Sub
    Public Sub LoadAllPluginSettings(ByRef SettingsFolderPath As String)
        If My.Computer.FileSystem.DirectoryExists(SettingsFolderPath) Then
            For Each FilePath As String In My.Computer.FileSystem.GetFiles(SettingsFolderPath)
                Dim Name As String = FilePath.Split("\").Last
                Name = Name.Replace(".txt", "")
                For i = 0 To BackgroundPlugins.Count - 1
                    If BackgroundPlugins(i).Name = Name Then
                        ConfigManager.LoadSettings(FilePath, BackgroundPlugins(i).Configuration)
                        Exit For
                    End If
                Next
            Next
        End If
    End Sub
End Class
