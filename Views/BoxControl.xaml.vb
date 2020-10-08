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

Partial Public Class BoxControl
    'Public Property TitleTextProperty() As String
    '       Get
    '           Return Me.DataTitleText.Text
    '       End Get
    '       Set(value As String)
    '           Me.DataTitleText.Text = Value
    '       End Set
    '   End Property
    '   Public Property MainDataTextProperty() As String
    '       Get
    '           Return Me.MainDataText.Text
    '       End Get
    '       Set(value As String)
    '           Me.MainDataText.Text = value
    '       End Set
    '   End Property
    '   Public Property UpperLeftTextProperty() As String
    '       Get
    '           Return Me.UPLeftText.Text
    '       End Get
    '       Set(value As String)
    '           Me.UPLeftText.Text = value
    '       End Set
    '   End Property
    '   Public Property UpperRightTextProperty() As String
    '       Get
    '           Return Me.UPRightText.Text
    '       End Get
    '       Set(value As String)
    '           Me.UPRightText.Text = value
    '       End Set
    '   End Property
    '   Public Property LowerLeftTextProperty() As String
    '       Get
    '           Return Me.LowerLeftText.Text
    '       End Get
    '       Set(value As String)
    '           Me.LowerLeftText.Text = value
    '       End Set
    '   End Property
    '   Public Property LowerRightTextProperty() As String
    '       Get
    '           Return Me.LowerRightText.Text
    '       End Get
    '       Set(value As String)
    '           Me.LowerRightText.Text = value
    '       End Set
    '   End Property

	Public Sub New()
		MyBase.New()
		Me.InitializeComponent()
        Me.SubData1.CornerRadProperty() = New CornerRadius(0, 30, 0, 0)
        Me.SubData4.CornerRadProperty() = New CornerRadius(0, 0, 0, 30)
        Me.SubData6.CornerRadProperty() = New CornerRadius(0, 0, 30, 0)
    End Sub
    ''' <summary>
    ''' Updates the Binding Source Propertys
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub UpdateSourcePropertys()
        Try

            If Me.DataTitleText.GetBindingExpression(TextBlock.TextProperty) IsNot Nothing Then
                Me.DataTitleText.GetBindingExpression(TextBlock.TextProperty).UpdateTarget()
            End If
            If MainDataText.GetBindingExpression(TextBlock.TextProperty) IsNot Nothing Then
                Me.MainDataText.GetBindingExpression(TextBlock.TextProperty).UpdateTarget()
            End If
            If UPLeftText.GetBindingExpression(TextBlock.TextProperty) IsNot Nothing Then
                Me.UPLeftText.GetBindingExpression(TextBlock.TextProperty).UpdateTarget()
            End If
            If UPRightText.GetBindingExpression(TextBlock.TextProperty) IsNot Nothing Then
                Me.UPRightText.GetBindingExpression(TextBlock.TextProperty).UpdateTarget()
            End If
            If LowerLeftText.GetBindingExpression(TextBlock.TextProperty) IsNot Nothing Then
                Me.LowerLeftText.GetBindingExpression(TextBlock.TextProperty).UpdateTarget()
            End If
            If LowerRightText.GetBindingExpression(TextBlock.TextProperty) IsNot Nothing Then
                Me.LowerRightText.GetBindingExpression(TextBlock.TextProperty).UpdateTarget()
            End If
        Catch ex As Exception

        End Try

        Me.SubData1.UpdateSourcePropertys()
        Me.SubData2.UpdateSourcePropertys()
        Me.SubData3.UpdateSourcePropertys()
        Me.SubData4.UpdateSourcePropertys()
        Me.SubData5.UpdateSourcePropertys()
        Me.SubData6.UpdateSourcePropertys()
    End Sub
End Class