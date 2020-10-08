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

Partial Public Class CustomButton_Test
	Public Sub New()
		MyBase.New()

		Me.InitializeComponent()

		' Insert code required on object creation below this point.
    End Sub

	Private Sub LayoutClicked(ByVal sender as Object, ByVal e as System.Windows.Input.MouseButtonEventArgs)
        'TODO: Add event handler implementation here.
    End Sub

    Private Sub CSelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles OpsList.SelectionChanged

    End Sub
End Class
