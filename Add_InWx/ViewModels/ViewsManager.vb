Imports System.ComponentModel.Composition.Hosting
Imports System.ComponentModel.Composition
Imports System.Collections.ObjectModel
Imports API
Imports API.API
Public Class ViewsManager
    <ImportMany(GetType(IView))>
    Public Views As IEnumerable(Of IView)
    Private Comp_Container As CompositionContainer
    Private Comp_Catalog As New AggregateCatalog
    Sub New()

        Comp_Catalog.Catalogs.Add(New AssemblyCatalog(GetType(MainWindow).Assembly))
        Try
            If My.Computer.FileSystem.DirectoryExists(MainWindow.ViewsPluginFolderPath) Then
                Comp_Catalog.Catalogs.Add(New DirectoryCatalog(MainWindow.ViewsPluginFolderPath))
            Else
                My.Computer.FileSystem.CreateDirectory(MainWindow.ViewsPluginFolderPath)
            End If
            Comp_Container = New CompositionContainer(Comp_Catalog)
            Comp_Container.ComposeParts(Me)
        Catch ex As Exception
            DebugMessanger.SendMessage(Me.ToString(), "Error Loading Plugins", ex.Message)
            Debug.Print(ex.Message)
        End Try
    End Sub
    Public Sub InitilizeViews(ByRef Stations As ObservableCollection(Of Station), ByRef StationManager As IViewHost)
        ConfigManager.intilizeGlobalConfig()
        For i = 0 To Views.Count - 1
            Views(i).Intilize(Stations, StationManager, ConfigManager._globalConfig)
        Next
    End Sub

    Sub SaveAllViewSettings(ByRef SettingsFolderPath As String)
        ConfigManager.SaveSettings(String.Format("{0}\{1}.txt", SettingsFolderPath, "GlobalSettings"), ConfigManager._globalConfig.GlobalSettings)
        For i = 0 To Views.Count - 1
            ConfigManager.SaveSettings(String.Format("{0}\{1}.txt", SettingsFolderPath, Views(i).ViewName), Views(i).Configuration)
        Next
    End Sub
    Sub LoadAllViewSettings(ByRef SettingsFolderPath As String)
        If My.Computer.FileSystem.DirectoryExists(SettingsFolderPath) Then
            ConfigManager.LoadSettings(String.Format("{0}\{1}.txt", SettingsFolderPath, "GlobalSettings"), ConfigManager._globalConfig.GlobalSettings)

            For Each FilePath As String In My.Computer.FileSystem.GetFiles(SettingsFolderPath)
                Dim View As String = FilePath.Split("\").Last
                View = View.Replace(".txt", "")
                For i = 0 To Views.Count - 1
                    If Views(i).ViewName = View Then
                        ConfigManager.LoadSettings(FilePath, Views(i).Configuration)
                        Exit For
                    End If
                Next
            Next
        End If
    End Sub
End Class
