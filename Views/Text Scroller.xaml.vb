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

Partial Public Class Text_Scroller
	Public Property ScrollTextProperty() as String
	Get
		Return me.TextScroller.Text
	End Get

	Set(Byval Value as String)
            Me.TextScroller.Text = Value
        End Set
    End Property

    Public Property ScrollWidthProperty() As Decimal
        Get
            Return Me.transferCurreny.X
        End Get
        Set(value As Decimal)
            Me.transferCurreny.X = value
        End Set
    End Property
    Public Property TextScroller2LeftProperty() As Thickness
        Get
            Return TextScroller2.Margin
        End Get
        Set(value As Thickness)
            TextScroller2.Margin = value
        End Set
    End Property

Private Sub UpdateScrollWidth()
        Me.canvas.UpdateLayout()
        Dim TempWidth As Decimal = 50
        If Me.TextScroller.ActualWidth > Me.canvas.ActualWidth Then
            TempWidth = Me.TextScroller.ActualWidth
        Else
            TempWidth = Me.canvas.ActualWidth
        End If
        me.TextScroller2LeftProperty = New Thickness(TempWidth, 0, 0, 0)
        If TempWidth > 0 Then
            TempWidth *= -1
        End If
        me.ScrollWidthProperty = TempWidth
    End Sub
Public Sub SetText(Byval Text as String)
	Me.ScrollTextProperty = Text
	me.UpdateScrollWidth()
End Sub

	Public Sub New()
		MyBase.New()
		Me.InitializeComponent()
		' Insert code required on object creation below this point.

	End Sub
End Class
