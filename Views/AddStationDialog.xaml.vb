Public Class AddStationDialog

    Private Sub WindowCloseButton_Click(sender As Object, e As RoutedEventArgs)
        Me.Close()
    End Sub

    Private Sub OnDragMoveWindow(sender As Object, e As MouseButtonEventArgs)
        Me.DragMove()
    End Sub

    Private Sub AddStation_Click(sender As Object, e As RoutedEventArgs)
        MainWindow.StationManager.AddWXStation(StationTypeNamesComBox.SelectedItem.ToString, StationNameTextBox.Text, True)
        Me.Close()
    End Sub
End Class
