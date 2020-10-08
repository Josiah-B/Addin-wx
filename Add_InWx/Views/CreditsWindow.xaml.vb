Public Class CreditsWindow

    Private Sub OnDragMoveWindow(sender As Object, e As MouseButtonEventArgs)
        Me.DragMove()
    End Sub

    Private Sub WindowCloseButton_Click(sender As Object, e As RoutedEventArgs)
        Me.Close()
    End Sub
End Class
