Public Class Debug_Output

    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        Me.MessageViewer.DataContext = API.API.DebugMessanger
    End Sub

    Private Sub OnDragMoveWindow(sender As Object, e As MouseButtonEventArgs)
        Me.DragMove()
    End Sub

    Private Sub WindowCloseButton_Click(sender As Object, e As RoutedEventArgs)
        Me.Close()
    End Sub
End Class
