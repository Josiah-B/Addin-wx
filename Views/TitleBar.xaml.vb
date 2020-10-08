Imports System
Imports System.Collections.Generic
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Data
Imports System.Windows.Input
Imports System.Windows.Media
Imports System.Windows.Media.Animation
Imports System.Windows.Media.Imaging
Imports System.Windows.Navigation
Imports System.Windows.Shapes
Imports API.API

Partial Public Class TitleBar
    Public Property SelectedStationIndex As Integer = 0

    Public Sub New()
        MyBase.New()

        Me.InitializeComponent()

        ' Insert code required on object creation below this point.
    End Sub

    Private Sub MainButtonClicked(ByVal sender As Object, ByVal e As System.Windows.Input.MouseButtonEventArgs)
        'TODO: Add event handler implementation here.
        If StationViewer.Visibility = Windows.Visibility.Collapsed Then
            StationViewer.Visibility = Windows.Visibility.Visible
        Else
            StationViewer.Visibility = Windows.Visibility.Collapsed
        End If
    End Sub

    Private Sub Mouse_Leave(sender As Object, e As MouseEventArgs) Handles MainTitleBar.MouseLeave
        StationViewer.Visibility = Windows.Visibility.Collapsed
    End Sub
    Private Sub Mouse_Enter(sender As Object, e As MouseEventArgs) Handles MainTitleBar.MouseEnter
        StationViewer.Visibility = Windows.Visibility.Visible
    End Sub

    Private Sub SelectedStationChanged(sender As Object, e As SelectionChangedEventArgs) Handles MainList.SelectionChanged
        SelectedStationIndex = MainList.SelectedIndex
        For i = 0 To MainWindow.ViewManager.Views.Count - 1
            MainWindow.ViewManager.Views(i).SelectedStationChanged(SelectedStationIndex)
        Next
        'For Each TempView As IView In MainWindow.ViewManager.Views
        '    TempView.SelectedStationChanged(SelectedStationIndex)
        'Next
    End Sub

End Class
