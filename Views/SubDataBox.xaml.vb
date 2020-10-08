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

Partial Public Class SubDataBox
	Public Property CornerRadProperty() As CornerRadius
        Get
            Return Me.Border3.CornerRadius
        End Get
        Set(Value As CornerRadius)
            Me.Border3.CornerRadius = Value
            Me.Border2.CornerRadius = New CornerRadius(0, Border3.CornerRadius.TopRight, 0, 0)
            Me.Border1.CornerRadius = New CornerRadius(0, 0, Border3.CornerRadius.BottomRight, Border3.CornerRadius.BottomLeft)
        End Set
    End Property
    Public Property TitleTextProperty() As String
        Get
            Return Me.DataTitle.Text
        End Get
        Set(value As String)
            Me.DataTitle.Text = Value
        End Set
    End Property
    Public Property MainDataTextProperty() As String
        Get
            Return Me.MainDataText.Text
        End Get
        Set(value As String)
            Me.MainDataText.Text = value
        End Set
    End Property
    Public Property UpperLeftTextProperty() As String
        Get
            Return Me.UPLeftText.Text
        End Get
        Set(value As String)
            Me.UPLeftText.Text = value
        End Set
    End Property
    Public Property UpperRightTextProperty() As String
        Get
            Return Me.UPRightText.Text
        End Get
        Set(value As String)
            Me.UPRightText.Text = value
        End Set
    End Property
    Public Property LowerLeftTextProperty() As String
        Get
            Return Me.LowerLeftText.Text
        End Get
        Set(value As String)
            Me.LowerLeftText.Text = value
        End Set
    End Property
    Public Property LowerRightTextProperty() As String
        Get
            Return Me.LowerRightText.Text
        End Get
        Set(value As String)
            Me.LowerRightText.Text = value
        End Set
    End Property
    Public Property LowerRightVis() As Windows.Visibility
        Get
            Return Me.LowerRightText.Visibility
        End Get
        Set(value As Windows.Visibility)
            Me.LowerRightText.Visibility = value
        End Set
    End Property
    Public Property LowerLeftVis() As Windows.Visibility
        Get
            Return Me.LowerLeftText.Visibility
        End Get
        Set(value As Windows.Visibility)
            Me.LowerLeftText.Visibility = value
        End Set
    End Property
    Public Property UpperRightVis() As Windows.Visibility
        Get
            Return Me.UPRightText.Visibility
        End Get
        Set(value As Windows.Visibility)
            Me.UPRightText.Visibility = value
        End Set
    End Property
    Public Property UpperLeftVis() As Windows.Visibility
        Get
            Return Me.UPLeftText.Visibility
        End Get
        Set(value As Windows.Visibility)
            Me.UPLeftText.Visibility = value
        End Set
    End Property
	Public Sub New()
		MyBase.New()
		Me.InitializeComponent()

		' Insert code required on object creation below this point.
	End Sub
    Public Sub UpdateSourcePropertys()
        If Me.DataTitle.GetBindingExpression(TextBlock.TextProperty) IsNot Nothing Then
            Me.DataTitle.GetBindingExpression(TextBlock.TextProperty).UpdateTarget()
        End If
        If Me.MainDataText.GetBindingExpression(TextBlock.TextProperty) IsNot Nothing Then
            Me.MainDataText.GetBindingExpression(TextBlock.TextProperty).UpdateTarget()
        End If
        If Me.UPLeftText.GetBindingExpression(TextBlock.TextProperty) IsNot Nothing Then
            Me.UPLeftText.GetBindingExpression(TextBlock.TextProperty).UpdateTarget()
        End If
        If Me.UPRightText.GetBindingExpression(TextBlock.TextProperty) IsNot Nothing Then
            Me.UPRightText.GetBindingExpression(TextBlock.TextProperty).UpdateTarget()
        End If
        If Me.LowerLeftText.GetBindingExpression(TextBlock.TextProperty) IsNot Nothing Then
            Me.LowerLeftText.GetBindingExpression(TextBlock.TextProperty).UpdateTarget()
        End If
        If Me.LowerRightText.GetBindingExpression(TextBlock.TextProperty) IsNot Nothing Then
            Me.LowerRightText.GetBindingExpression(TextBlock.TextProperty).UpdateTarget()
        End If
    End Sub

    Private Sub UserControl_DataContextChanged(sender As Object, e As DependencyPropertyChangedEventArgs)
        'Dim ty = TryCast(e.NewValue, API.API.IValue)
        'If ty IsNot Nothing Then
        '    Debug.WriteLine(ty.Value)
        '    'Me.MainDataText.DataContext = ty
        '    Dim bind As Binding = New Binding("")
        '    bind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        '    bind.Converter = New Add_InWx.FromDataValToDisplay()
        '    Me.MainDataText.SetBinding(TextBlock.TextProperty, bind)
        'End If
    End Sub
End Class
