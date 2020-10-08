Imports System.Windows
Imports API.API
Imports System.ComponentModel.Composition

<Export(GetType(IView))>
Public Class MainView_v3
    Implements IView
    Public _stations As System.Collections.ObjectModel.ObservableCollection(Of Station)
    Private _viewName As [String] = "MainView v3"
    Private _SelectedStationIndex As Integer = -1
    Private UpdateUITimer As New System.Windows.Threading.DispatcherTimer(System.Windows.Threading.DispatcherPriority.Background)


    Public Sub New()
        InitializeComponent()
        'Me.drawItTest.Effect = New Effects.DropShadowEffect()
    End Sub

    Public Property Configuration() As System.Collections.ObjectModel.ObservableCollection(Of Config) Implements IView.Configuration
        Get
            Return drawItTest._config
        End Get
        Set(value As System.Collections.ObjectModel.ObservableCollection(Of Config))
            drawItTest._config = value
        End Set
    End Property

    Public Sub Intilize(Stations As System.Collections.ObjectModel.ObservableCollection(Of Station), ByRef StationManager As IViewHost, ByRef GlobalSettings As ConfigManager.GlobalConfig) Implements IView.Intilize
        drawItTest._GBLConfig = GlobalSettings
        _stations = Stations
        IntilizeSettings()
        UpdateUITimer.Interval = New TimeSpan(0, 0, 0, 1)
        AddHandler UpdateUITimer.Tick, AddressOf UpdateUITimer_Tick
        UpdateUITimer.Start()
    End Sub

    Private Sub IntilizeSettings()
        drawItTest._config = New System.Collections.ObjectModel.ObservableCollection(Of Config)()
        drawItTest._config.Add(New Config("Graphics Settings"))
        drawItTest._config(0).ConfigSettings.Add(New Setting("Upper Portion Box Transparency As Percent", "60"))
        drawItTest._config(0).ConfigSettings.Add(New Setting("Lower Portion Box Transparency As Percent", "30"))
        drawItTest._config(0).ConfigSettings.Add(New Setting("Overall Box Transparency As Percent", "70"))
        '    drawItTest._config(0).ConfigSettings(CInt(DrawIt.configIndexes.Temp)).AllowedParameters = New List(Of String)() From {DataUnitType.Temp_Fahrenheit.ToString(), DataUnitType.Temp_Celsius.ToString()}
        '    drawItTest._config(0).ConfigSettings.Add(New Setting("Wind Units", DataUnitType.Wind_Mph.ToString()))
        '    drawItTest._config(0).ConfigSettings(CInt(DrawIt.configIndexes.Wind)).AllowedParameters = New List(Of String)() From {DataUnitType.Wind_Mph.ToString(), DataUnitType.Wind_Kph.ToString()}
        '    drawItTest._config(0).ConfigSettings.Add(New Setting("Barometer Units", DataUnitType.Bar_InHg.ToString()))
        '    drawItTest._config(0).ConfigSettings(CInt(DrawIt.configIndexes.Bar)).AllowedParameters = New List(Of String)() From {DataUnitType.Bar_InHg.ToString(), DataUnitType.Bar_mbar.ToString(), DataUnitType.Bar_hpa.ToString()}
        '    drawItTest._config(0).ConfigSettings.Add(New Setting("Rain Units", DataUnitType.Precip_Inch.ToString()))
        '    drawItTest._config(0).ConfigSettings(CInt(DrawIt.configIndexes.Precip)).AllowedParameters = New List(Of String)() From {DataUnitType.Precip_Inch.ToString(), DataUnitType.Precip_Millimeter.ToString()}
        '    drawItTest._config(0).ConfigSettings.Add(New Setting("Other Units", DataUnitType.Other_Feet.ToString()))
        '    drawItTest._config(0).ConfigSettings(CInt(DrawIt.configIndexes.Other)).AllowedParameters = New List(Of String)() From {DataUnitType.Other_Feet.ToString(), DataUnitType.Other_Meter.ToString()}
    End Sub

    Private Sub UpdateUITimer_Tick(sender As Object, e As EventArgs)
        Me.drawItTest.InvalidateVisual()
    End Sub

    Public Sub SelectedStationChanged(SelectedStaitonIndex As Integer) Implements IView.SelectedStationChanged
        _SelectedStationIndex = SelectedStaitonIndex
        If _stations.Count > 0 Then
            _SelectedStationIndex = SelectedStaitonIndex
            If _SelectedStationIndex > -1 Then
                Me.drawItTest.DataContext = _stations(SelectedStaitonIndex).WXStation.WXData
            End If
        End If
    End Sub

    Public Property ViewName() As String Implements IView.ViewName
        Get
            Return _viewName
        End Get
        Set(value As String)
            _viewName = value
        End Set
    End Property

End Class
Public Class DrawIt
    Inherits FrameworkElement
    Private Mainrects As Rect() = New Rect(3) {}
    Private DataTitlerects As Rect(,) = New Rect(3, 6) {}
    Private DataRects As Rect(,) = New Rect(3, 6) {}
    Private RenderingAreaWidth As Integer = 1200
    Private RenderingAreaHeight As Integer = 675
    Private MaintypeFace As New Typeface(New FontFamily("Segoe UI"), FontStyles.Oblique, FontWeights.Normal, FontStretches.Normal)
    Private DataTitletypeFace As New Typeface(New FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Light, FontStretches.Normal)
    Private _mainTitleFontSize As Integer = 38
    Private _mainDataFontSize As Integer = 97
    Private _secTitleFontSize As Integer = 18
    Private _secDataFontSize As Integer = 27
    Private _timeFontSize As Integer = 18
    Public visuals As VisualCollection
    Public Shared StationDataProperty As DependencyProperty = DependencyProperty.Register("StationData", GetType(StationData), GetType(DrawIt), New FrameworkPropertyMetadata(Nothing, FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender Or FrameworkPropertyMetadataOptions.AffectsRender))

    Public _config As ObjectModel.ObservableCollection(Of Config)

    Public Delegate Sub DrawAllTextD(_stationData As StationData)
    Public _GBLConfig As ConfigManager.GlobalConfig

    Public Property StationData() As String
        Get
            Return DirectCast(GetValue(StationDataProperty), String)
        End Get
        Set(value As String)
            SetValue(StationDataProperty, value)
        End Set
    End Property
    Private Sub IntRects()
        Mainrects(0) = New Rect(Math.Floor(RenderingAreaWidth * 0.025), Math.Floor(RenderingAreaHeight * 0.025), Math.Floor(RenderingAreaWidth * 0.47), Math.Floor(RenderingAreaHeight * 0.45))
        Mainrects(1) = New Rect(Math.Floor(RenderingAreaWidth * 0.505), Math.Floor(RenderingAreaHeight * 0.025), Math.Floor(RenderingAreaWidth * 0.47), Math.Floor(RenderingAreaHeight * 0.45))
        Mainrects(2) = New Rect(Math.Floor(RenderingAreaWidth * 0.025), Math.Floor(RenderingAreaHeight * 0.51), Math.Floor(RenderingAreaWidth * 0.47), Math.Floor(RenderingAreaHeight * 0.45))
        Mainrects(3) = New Rect(Math.Floor(RenderingAreaWidth * 0.505), Math.Floor(RenderingAreaHeight * 0.51), Math.Floor(RenderingAreaWidth * 0.47), Math.Floor(RenderingAreaHeight * 0.45))

        For i As Integer = 0 To 3
            DataTitlerects(i, 0) = New Rect(Mainrects(i).Left, Mainrects(i).Top, Math.Floor(Mainrects(i).Width * 0.65), Math.Floor(Mainrects(i).Height * 0.24))
            DataTitlerects(i, 1) = New Rect(DataTitlerects(i, 0).Right, DataTitlerects(i, 0).Top, Math.Floor(Mainrects(i).Width - DataTitlerects(i, 0).Width), Math.Floor(Mainrects(i).Height * 0.08))
            DataTitlerects(i, 2) = New Rect(DataTitlerects(i, 0).Right, DataTitlerects(i, 0).Bottom, DataTitlerects(i, 1).Width, DataTitlerects(i, 1).Height)
            DataTitlerects(i, 3) = New Rect(DataTitlerects(i, 0).Right, Math.Floor((Mainrects(i).Height * 0.5) + Mainrects(i).Top), DataTitlerects(i, 1).Width, DataTitlerects(i, 1).Height)
            DataTitlerects(i, 4) = New Rect(Mainrects(i).Left, Math.Floor((Mainrects(i).Height * 0.75) + Mainrects(i).Top), Math.Floor(DataTitlerects(i, 0).Width * 0.5), DataTitlerects(i, 1).Height)
            DataTitlerects(i, 5) = New Rect(DataTitlerects(i, 4).Right, Math.Floor((Mainrects(i).Height * 0.75) + Mainrects(i).Top), Math.Ceiling(DataTitlerects(i, 0).Width * 0.5), DataTitlerects(i, 1).Height)
            DataTitlerects(i, 6) = New Rect(DataTitlerects(i, 5).Right, Math.Floor((Mainrects(i).Height * 0.75) + Mainrects(i).Top), DataTitlerects(i, 1).Width, DataTitlerects(i, 1).Height)

            DataRects(i, 0) = New Rect(Mainrects(i).Left, DataTitlerects(i, 0).Bottom, DataTitlerects(i, 0).Width, DataTitlerects(i, 4).Top - DataTitlerects(i, 0).Bottom)
            DataRects(i, 1) = New Rect(DataTitlerects(i, 0).Right, DataTitlerects(i, 1).Bottom, DataTitlerects(i, 1).Width, DataTitlerects(i, 2).Top - DataTitlerects(i, 1).Bottom)
            DataRects(i, 2) = New Rect(DataTitlerects(i, 0).Right, DataTitlerects(i, 2).Bottom, DataTitlerects(i, 2).Width, DataTitlerects(i, 3).Top - DataTitlerects(i, 2).Bottom)
            DataRects(i, 3) = New Rect(DataTitlerects(i, 0).Right, DataTitlerects(i, 3).Bottom, DataTitlerects(i, 3).Width, DataTitlerects(i, 4).Top - DataTitlerects(i, 3).Bottom)
            DataRects(i, 4) = New Rect(DataTitlerects(i, 4).Left, DataTitlerects(i, 4).Bottom, DataTitlerects(i, 4).Width, Mainrects(i).Bottom - DataTitlerects(i, 4).Bottom)
            DataRects(i, 5) = New Rect(DataTitlerects(i, 5).Left, DataTitlerects(i, 5).Bottom, DataTitlerects(i, 5).Width, Mainrects(i).Bottom - DataTitlerects(i, 5).Bottom)
            DataRects(i, 6) = New Rect(DataTitlerects(i, 6).Left, DataTitlerects(i, 6).Bottom, DataTitlerects(i, 6).Width, Mainrects(i).Bottom - DataTitlerects(i, 6).Bottom)
        Next
    End Sub

    Public Sub New()
        visuals = New VisualCollection(Me)
        AddHandler Me.Loaded, AddressOf DrawIt_Loaded
    End Sub

    Private Sub DCRR(dc As DrawingContext, brush As Brush, pen As Pen, rect As Rect, cornerRadius As CornerRadius)
        Dim geometry = New StreamGeometry()
        Using context = geometry.Open()
            Dim isStroked As Boolean = pen IsNot Nothing
            Const isSmoothJoin As Boolean = True

            context.BeginFigure(rect.TopLeft + New Vector(0, cornerRadius.TopLeft), brush IsNot Nothing, True)
            context.ArcTo(New Point(rect.TopLeft.X + cornerRadius.TopLeft, rect.TopLeft.Y), New Size(cornerRadius.TopLeft, cornerRadius.TopLeft), 90, False, SweepDirection.Clockwise, isStroked, isSmoothJoin)
            context.LineTo(rect.TopRight - New Vector(cornerRadius.TopRight, 0), isStroked, isSmoothJoin)
            context.ArcTo(New Point(rect.TopRight.X, rect.TopRight.Y + cornerRadius.TopRight), New Size(cornerRadius.TopRight, cornerRadius.TopRight), 90, False, SweepDirection.Clockwise, isStroked, isSmoothJoin)
            context.LineTo(rect.BottomRight - New Vector(0, cornerRadius.BottomRight), isStroked, isSmoothJoin)
            context.ArcTo(New Point(rect.BottomRight.X - cornerRadius.BottomRight, rect.BottomRight.Y), New Size(cornerRadius.BottomRight, cornerRadius.BottomRight), 90, False, SweepDirection.Clockwise, isStroked, isSmoothJoin)
            context.LineTo(rect.BottomLeft + New Vector(cornerRadius.BottomLeft, 0), isStroked, isSmoothJoin)
            context.ArcTo(New Point(rect.BottomLeft.X, rect.BottomLeft.Y - cornerRadius.BottomLeft), New Size(cornerRadius.BottomLeft, cornerRadius.BottomLeft), 90, False, SweepDirection.Clockwise, isStroked, isSmoothJoin)
            context.Close()
        End Using
        dc.DrawGeometry(brush, pen, geometry)
    End Sub


    Public Sub DrawAllText(dc As DrawingContext)
        Dim _stationData As StationData = DirectCast(Me.GetValue(StationDataProperty), StationData)

        If _stationData IsNot Nothing Then

            If _GBLConfig.GlobalSettings(0).ConfigSettings(CInt(ConfigManager.GlobalConfig.configIndexes.Temp)).CurrentValue = API.API.DataUnitType.Temp_Celsius.ToString() Then
                Draw_TempAsMetric(_stationData, dc)
            Else
                Draw_TempAsEnglish(_stationData, dc)
            End If

            If _GBLConfig.GlobalSettings(0).ConfigSettings(CInt(ConfigManager.GlobalConfig.configIndexes.Wind)).CurrentValue = API.API.DataUnitType.Wind_Kph.ToString() Then
                Draw_WindAsMetric(_stationData, dc)
            Else
                Draw_WindAsEnglish(_stationData, dc)
            End If

            If _GBLConfig.GlobalSettings(0).ConfigSettings(CInt(ConfigManager.GlobalConfig.configIndexes.Bar)).CurrentValue = API.API.DataUnitType.Bar_mbar.ToString() OrElse _GBLConfig.GlobalSettings(0).ConfigSettings(CInt(ConfigManager.GlobalConfig.configIndexes.Bar)).CurrentValue = API.API.DataUnitType.Bar_hpa.ToString() Then
                Draw_BarAsMetric(_stationData, dc)
            Else
                Draw_BarAsEnglish(_stationData, dc)
            End If

            If _GBLConfig.GlobalSettings(0).ConfigSettings(CInt(ConfigManager.GlobalConfig.configIndexes.Rain)).CurrentValue = API.API.DataUnitType.Precip_Millimeter.ToString() Then
                Draw_PrecipAsMetric(_stationData, dc)
            Else
                Draw_PrecipAsEnglish(_stationData, dc)
            End If

            If _GBLConfig.GlobalSettings(0).ConfigSettings(CInt(ConfigManager.GlobalConfig.configIndexes.Length)).CurrentValue = API.API.DataUnitType.Other_Meter.ToString() Then
                Draw_OtherAsMetric(_stationData, dc)
            Else
                Draw_OtherAsEnglish(_stationData, dc)
            End If

            drawTextTopRightCorner(_GBLConfig.GlobalSettings(0).ConfigSettings(CInt(ConfigManager.GlobalConfig.configIndexes.Temp)).CurrentValue.Replace("Temp_", ""), Brushes.Black, DataTitletypeFace, DataRects(0, 0), _timeFontSize, dc, New Point(2, 0))
            drawTextTopRightCorner(_GBLConfig.GlobalSettings(0).ConfigSettings(CInt(ConfigManager.GlobalConfig.configIndexes.Wind)).CurrentValue.Replace("Wind_", ""), Brushes.Black, DataTitletypeFace, DataRects(1, 0), _timeFontSize, dc, New Point(2, 0))
            drawTextTopRightCorner(_GBLConfig.GlobalSettings(0).ConfigSettings(CInt(ConfigManager.GlobalConfig.configIndexes.Bar)).CurrentValue.Replace("Bar_", ""), Brushes.Black, DataTitletypeFace, DataRects(2, 0), _timeFontSize, dc, New Point(2, 0))
            drawTextTopRightCorner(_GBLConfig.GlobalSettings(0).ConfigSettings(CInt(ConfigManager.GlobalConfig.configIndexes.Rain)).CurrentValue.Replace("Precip_", ""), Brushes.Black, DataTitletypeFace, DataRects(3, 0), _timeFontSize, dc, New Point(2, 0))
            drawTextTopRightCorner(_GBLConfig.GlobalSettings(0).ConfigSettings(CInt(ConfigManager.GlobalConfig.configIndexes.Length)).CurrentValue.Replace("Other_", ""), Brushes.Black, DataTitletypeFace, DataRects(3, 6), 16, dc, New Point(2, 0))

            drawText(_stationData.UVIndex.AsEnglish, Brushes.Black, DataTitletypeFace, DataRects(0, 6), _secDataFontSize, dc)


            drawText(_stationData.WindBearing.AsEnglish, Brushes.Black, DataTitletypeFace, DataRects(1, 4), _secDataFontSize, dc)
            drawText(_stationData.WindDirection.AsEnglish, Brushes.Black, DataTitletypeFace, DataRects(1, 5), _secDataFontSize, dc)
            drawText("", Brushes.Black, DataTitletypeFace, DataRects(1, 6), _secDataFontSize, dc)


            drawText(_stationData.ActiveWeather.AsEnglish, Brushes.Black, DataTitletypeFace, DataRects(2, 1), _secDataFontSize, dc)
            drawText(_stationData.SkyConditions.AsEnglish, Brushes.Black, DataTitletypeFace, DataRects(2, 2), _secDataFontSize, dc)

            drawText(_stationData.HumidityOutCur.Value, Brushes.Black, DataTitletypeFace, DataRects(3, 5), _secDataFontSize, dc)
        End If

    End Sub
    Public Sub Draw_TempAsEnglish(_stationData As StationData, dc As DrawingContext)
        drawText(_stationData.TempOutCur.AsEnglish.ToString(), Brushes.Black, New Typeface(New FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), DataRects(0, 0), _mainDataFontSize, dc)
        drawText(_stationData.TempInCur.AsEnglish.ToString(), Brushes.Black, DataTitletypeFace, DataRects(0, 1), _secDataFontSize, dc)
        drawText(_stationData.HeatIndex.AsEnglish.ToString(), Brushes.Black, DataTitletypeFace, DataRects(0, 2), _secDataFontSize, dc)
        drawText(_stationData.WindChill.AsEnglish.ToString(), Brushes.Black, DataTitletypeFace, DataRects(0, 3), _secDataFontSize, dc)
        drawText(_stationData.TempOutHigh.AsEnglish.ToString(), Brushes.Black, DataTitletypeFace, DataRects(0, 4), _secDataFontSize, dc)
        drawTextBottomRightCorner(_stationData.TempOutHigh.TimeOfValue.ToShortTimeString(), Brushes.Black, DataTitletypeFace, DataRects(0, 4), _timeFontSize, dc, New Point(0, 0))
        drawText(_stationData.TempOutLow.AsEnglish.ToString(), Brushes.Black, DataTitletypeFace, DataRects(0, 5), _secDataFontSize, dc)
        drawTextBottomRightCorner(_stationData.TempOutLow.TimeOfValue.ToShortTimeString(), Brushes.Black, DataTitletypeFace, DataRects(0, 5), _timeFontSize, dc, New Point(0, 0))
        drawText(_stationData.DewPoint.AsEnglish.ToString(), Brushes.Black, DataTitletypeFace, DataRects(3, 3), _secDataFontSize, dc)
    End Sub
    Private Sub Draw_TempAsMetric(_stationData As StationData, dc As DrawingContext)
        drawText(_stationData.TempOutCur.AsMetric.ToString(), Brushes.Black, New Typeface(New FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), DataRects(0, 0), _mainDataFontSize, dc)
        drawText(_stationData.TempInCur.AsMetric.ToString(), Brushes.Black, DataTitletypeFace, DataRects(0, 1), _secDataFontSize, dc)
        drawText(_stationData.HeatIndex.AsMetric.ToString(), Brushes.Black, DataTitletypeFace, DataRects(0, 2), _secDataFontSize, dc)
        drawText(_stationData.WindChill.AsMetric.ToString(), Brushes.Black, DataTitletypeFace, DataRects(0, 3), _secDataFontSize, dc)
        drawText(_stationData.TempOutHigh.AsMetric.ToString(), Brushes.Black, DataTitletypeFace, DataRects(0, 4), _secDataFontSize, dc)
        drawTextBottomRightCorner(_stationData.TempOutHigh.TimeOfValue.ToShortTimeString(), Brushes.Black, DataTitletypeFace, DataRects(0, 4), _timeFontSize, dc, New Point(0, 0))
        drawText(_stationData.TempOutLow.AsMetric.ToString(), Brushes.Black, DataTitletypeFace, DataRects(0, 5), _secDataFontSize, dc)
        drawTextBottomRightCorner(_stationData.TempOutLow.TimeOfValue.ToShortTimeString(), Brushes.Black, DataTitletypeFace, DataRects(0, 5), _timeFontSize, dc, New Point(0, 0))
        drawText(_stationData.DewPoint.AsMetric.ToString(), Brushes.Black, DataTitletypeFace, DataRects(3, 3), _secDataFontSize, dc)
    End Sub
    Private Sub Draw_WindAsEnglish(_stationData As StationData, dc As DrawingContext)
        drawText(_stationData.WindSpeed.AsEnglish.ToString(), Brushes.Black, New Typeface(New FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), DataRects(1, 0), _mainDataFontSize, dc)
        drawText(_stationData.WindPeakGust.AsEnglish.ToString(), Brushes.Black, DataTitletypeFace, DataRects(1, 1), _secDataFontSize, dc)
        drawTextBottomRightCorner(_stationData.WindPeakGust.TimeOfValue.ToShortTimeString(), Brushes.Black, DataTitletypeFace, DataRects(1, 1), _timeFontSize, dc, New Point(0, 0))
        'drawText(_stationData.Wind5MinPeak.AsEnglish.ToString(), Brushes.Black, DataTitletypeFace, DataRects(1, 2), _secDataFontSize, dc)
        'drawText(_stationData.Wind1MinAvg.AsEnglish.ToString(), Brushes.Black, DataTitletypeFace, DataRects(1, 3), _secDataFontSize, dc)
    End Sub
    Private Sub Draw_WindAsMetric(_stationData As StationData, dc As DrawingContext)
        drawText(_stationData.WindSpeed.AsMetric.ToString(), Brushes.Black, New Typeface(New FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), DataRects(1, 0), _mainDataFontSize, dc)
        drawText(_stationData.WindPeakGust.AsMetric.ToString(), Brushes.Black, DataTitletypeFace, DataRects(1, 1), _secDataFontSize, dc)
        drawTextBottomRightCorner(_stationData.WindPeakGust.TimeOfValue.ToShortTimeString(), Brushes.Black, DataTitletypeFace, DataRects(1, 1), _timeFontSize, dc, New Point(0, 0))
        'drawText(_stationData.Wind5MinPeak.AsMetric.ToString(), Brushes.Black, DataTitletypeFace, DataRects(1, 2), _secDataFontSize, dc)
        'drawText(_stationData.Wind1MinAvg.AsMetric.ToString(), Brushes.Black, DataTitletypeFace, DataRects(1, 3), _secDataFontSize, dc)
    End Sub
    Private Sub Draw_BarAsEnglish(_stationData As StationData, dc As DrawingContext)
        drawText(_stationData.BarometerCur.AsEnglish.ToString(), Brushes.Black, New Typeface(New FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), DataRects(2, 0), _mainDataFontSize, dc)
        drawText(_stationData.Barometer3HrChange.AsEnglish.ToString(), Brushes.Black, DataTitletypeFace, DataRects(2, 3), _secDataFontSize, dc)
        drawText(_stationData.BarometerHigh.AsEnglish.ToString(), Brushes.Black, DataTitletypeFace, DataRects(2, 4), _secDataFontSize, dc)
        drawTextBottomRightCorner(_stationData.BarometerHigh.TimeOfValue.ToShortTimeString(), Brushes.Black, DataTitletypeFace, DataRects(2, 4), _timeFontSize, dc, New Point(0, 0))
        drawText(_stationData.BarometerLow.AsEnglish.ToString(), Brushes.Black, DataTitletypeFace, DataRects(2, 5), _secDataFontSize, dc)
        drawTextBottomRightCorner(_stationData.BarometerLow.TimeOfValue.ToShortTimeString(), Brushes.Black, DataTitletypeFace, DataRects(2, 5), _timeFontSize, dc, New Point(0, 0))
        drawText("", Brushes.Black, DataTitletypeFace, DataRects(2, 6), _secDataFontSize, dc)
    End Sub
    Private Sub Draw_BarAsMetric(_stationData As StationData, dc As DrawingContext)
        drawText(_stationData.BarometerCur.AsMetric.ToString(), Brushes.Black, New Typeface(New FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), DataRects(2, 0), _mainDataFontSize, dc)
        drawText(_stationData.Barometer3HrChange.AsMetric.ToString(), Brushes.Black, DataTitletypeFace, DataRects(2, 3), _secDataFontSize, dc)
        drawText(_stationData.BarometerHigh.AsMetric.ToString(), Brushes.Black, DataTitletypeFace, DataRects(2, 4), _secDataFontSize, dc)
        drawTextBottomRightCorner(_stationData.BarometerHigh.TimeOfValue.ToShortTimeString(), Brushes.Black, DataTitletypeFace, DataRects(2, 4), _timeFontSize, dc, New Point(0, 0))
        drawText(_stationData.BarometerLow.AsMetric.ToString(), Brushes.Black, DataTitletypeFace, DataRects(2, 5), _secDataFontSize, dc)
        drawTextBottomRightCorner(_stationData.BarometerLow.TimeOfValue.ToShortTimeString(), Brushes.Black, DataTitletypeFace, DataRects(2, 5), _timeFontSize, dc, New Point(0, 0))
        drawText("", Brushes.Black, DataTitletypeFace, DataRects(2, 6), _secDataFontSize, dc)
    End Sub
    Private Sub Draw_PrecipAsEnglish(_stationData As StationData, dc As DrawingContext)
        drawText(_stationData.RainTodayTotal.AsEnglish.ToString(), Brushes.Black, New Typeface(New FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), DataRects(3, 0), _mainDataFontSize, dc)
        drawText(_stationData.RainRateAnHr.AsEnglish.ToString(), Brushes.Black, DataTitletypeFace, DataRects(3, 1), _secDataFontSize, dc)
        drawText("", Brushes.Black, DataTitletypeFace, DataRects(3, 2), _secDataFontSize, dc)
        drawText(_stationData.RainLongTermTotal.AsEnglish.ToString(), Brushes.Black, DataTitletypeFace, DataRects(3, 4), _secDataFontSize, dc)
    End Sub
    Private Sub Draw_PrecipAsMetric(_stationData As StationData, dc As DrawingContext)
        drawText(_stationData.RainTodayTotal.AsMetric.ToString(), Brushes.Black, New Typeface(New FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), DataRects(3, 0), _mainDataFontSize, dc)
        drawText(_stationData.RainRateAnHr.AsMetric.ToString(), Brushes.Black, DataTitletypeFace, DataRects(3, 1), _secDataFontSize, dc)
        drawText("", Brushes.Black, DataTitletypeFace, DataRects(3, 2), _secDataFontSize, dc)
        drawText(_stationData.RainLongTermTotal.AsMetric.ToString(), Brushes.Black, DataTitletypeFace, DataRects(3, 4), _secDataFontSize, dc)
    End Sub
    Private Sub Draw_OtherAsEnglish(_stationData As StationData, dc As DrawingContext)
        drawText(_stationData.CloudBase.AsEnglish.ToString(), Brushes.Black, DataTitletypeFace, DataRects(3, 6), _secDataFontSize, dc)
    End Sub
    Private Sub Draw_OtherAsMetric(_stationData As StationData, dc As DrawingContext)
        drawText(_stationData.CloudBase.AsMetric.ToString(), Brushes.Black, DataTitletypeFace, DataRects(3, 6), _secDataFontSize, dc)
    End Sub

    Public Sub drawBoxes(dc As DrawingContext)
        '        Dim grad As New LinearGradientBrush()
        Dim Up As Integer = 153
        If Decimal.TryParse(_config(0).ConfigSettings(0).CurrentValue, Up) Then
            Up *= 2.55
            If Up < 0 Or Up > 255 Then
                Up = 153
            End If
        Else
            Up = 153
        End If

        Dim Lower As Integer = 76
        If Decimal.TryParse(_config(0).ConfigSettings(1).CurrentValue, Lower) Then
            Lower *= 2.55
            If Lower < 0 Or Lower > 255 Then
                Lower = 76
            End If
        Else
            Lower = 76
        End If

        Dim Overall As Decimal = 100
        If Decimal.TryParse(_config(0).ConfigSettings(2).CurrentValue, Overall) Then
            Overall /= 100
            If Overall < 0 Or Overall > 1 Then
                Overall = 0.7
            End If
        Else
            Overall = 0.7
        End If
        'grad.GradientStops.Add(New GradientStop(Color.FromArgb(Up, 255, 255, 255), 0))
        'grad.GradientStops.Add(New GradientStop(Color.FromArgb(Up, 255, 255, 255), 0.2))
        'grad.GradientStops.Add(New GradientStop(Color.FromArgb(Lower, 255, 255, 255), 0.32))
        ''grad.GradientStops.Add(New GradientStop(Color.FromArgb(80, 255, 255, 255), 1))
        'grad.StartPoint = New Point(0.5, 0)
        'grad.EndPoint = New Point(0.5, 1)
        'grad.Opacity = Overall
        'grad.SpreadMethod = GradientSpreadMethod.Pad

            Dim TitleCNRad As New CornerRadius(0, 20, 0, 0)
            Dim DataCNRad As New CornerRadius(0, 0, 20, 0)
            Dim _stationData As StationData = DirectCast(Me.GetValue(StationDataProperty), StationData)
            For i As Integer = 0 To 3
            'dc.DrawRoundedRectangle(grad, New Pen(Brushes.Black, 1), Mainrects(i), 30, 30)
            dc.DrawRoundedRectangle(Brushes.Transparent, New Pen(Brushes.Black, 1), Mainrects(i), 30, 30)
                For q As Integer = 0 To 6
                    Select Case q
                        Case 0
                            If True Then
                                TitleCNRad = New CornerRadius(30, 0, 0, 0)
                                DataCNRad = New CornerRadius(0, 0, 0, 0)
                                Exit Select
                            End If
                        Case 1
                            If True Then
                                TitleCNRad = New CornerRadius(0, 30, 0, 0)
                                DataCNRad = New CornerRadius(0, 0, 0, 0)
                                Exit Select
                            End If
                        Case 4
                            If True Then
                                TitleCNRad = New CornerRadius(0, 0, 0, 0)
                                DataCNRad = New CornerRadius(0, 0, 0, 30)
                                Exit Select
                            End If
                        Case 6
                            If True Then
                                TitleCNRad = New CornerRadius(0, 0, 0, 0)
                                DataCNRad = New CornerRadius(0, 0, 30, 0)
                                Exit Select
                            End If
                        Case Else
                            If True Then
                                TitleCNRad = New CornerRadius(0, 0, 0, 0)
                                DataCNRad = New CornerRadius(0, 0, 0, 0)
                                Exit Select
                            End If
                    End Select
                    DCRR(dc, New SolidColorBrush(Color.FromArgb(0, 0, 50, 250)), New Pen(Brushes.Black, 0.5), DataTitlerects(i, q), TitleCNRad)
                    DCRR(dc, New SolidColorBrush(Color.FromArgb(0, 0, 200, 50)), New Pen(Brushes.Black, 0.5), DataRects(i, q), DataCNRad)
                Next
                If _stationData IsNot Nothing Then
                    drawText(_stationData.TempOutCur.Title, Brushes.Black, MaintypeFace, DataTitlerects(0, 0), _mainTitleFontSize, dc)
                    drawText(_stationData.WindSpeed.Title, Brushes.Black, MaintypeFace, DataTitlerects(1, 0), _mainTitleFontSize, dc)
                    drawText(_stationData.BarometerCur.Title, Brushes.Black, MaintypeFace, DataTitlerects(2, 0), _mainTitleFontSize, dc)
                    drawText(_stationData.RainTodayTotal.Title, Brushes.Black, MaintypeFace, DataTitlerects(3, 0), _mainTitleFontSize, dc)

                    drawText(_stationData.TempInCur.Title, Brushes.Black, DataTitletypeFace, DataTitlerects(0, 1), _secTitleFontSize, dc)
                    drawText(_stationData.HeatIndex.Title, Brushes.Black, DataTitletypeFace, DataTitlerects(0, 2), _secTitleFontSize, dc)
                    drawText(_stationData.WindChill.Title, Brushes.Black, DataTitletypeFace, DataTitlerects(0, 3), _secTitleFontSize, dc)
                    drawText(_stationData.TempOutHigh.Title, Brushes.Black, DataTitletypeFace, DataTitlerects(0, 4), _secTitleFontSize, dc)
                    drawText(_stationData.TempOutLow.Title, Brushes.Black, DataTitletypeFace, DataTitlerects(0, 5), _secTitleFontSize, dc)
                    drawText(_stationData.UVIndex.Title, Brushes.Black, DataTitletypeFace, DataTitlerects(0, 6), _secTitleFontSize, dc)

                    drawText(_stationData.WindPeakGust.Title, Brushes.Black, DataTitletypeFace, DataTitlerects(1, 1), _secTitleFontSize, dc)
                'drawText(_stationData.Wind5MinPeak.Title, Brushes.Black, DataTitletypeFace, DataTitlerects(1, 2), _secTitleFontSize, dc)
                'drawText(_stationData.Wind1MinAvg.Title, Brushes.Black, DataTitletypeFace, DataTitlerects(1, 3), _secTitleFontSize, dc)
                    drawText(_stationData.WindDirection.Title, Brushes.Black, DataTitletypeFace, DataTitlerects(1, 4), _secTitleFontSize, dc)
                    drawText(_stationData.WindBearing.Title, Brushes.Black, DataTitletypeFace, DataTitlerects(1, 5), _secTitleFontSize, dc)
                    drawText("", Brushes.Black, DataTitletypeFace, DataTitlerects(1, 6), _secTitleFontSize, dc)

                    drawText(_stationData.ActiveWeather.Title, Brushes.Black, DataTitletypeFace, DataTitlerects(2, 1), _secTitleFontSize, dc)
                    drawText(_stationData.SkyConditions.Title, Brushes.Black, DataTitletypeFace, DataTitlerects(2, 2), _secTitleFontSize, dc)
                    drawText(_stationData.Barometer3HrChange.Title, Brushes.Black, DataTitletypeFace, DataTitlerects(2, 3), _secTitleFontSize, dc)
                    drawText(_stationData.BarometerHigh.Title, Brushes.Black, DataTitletypeFace, DataTitlerects(2, 4), _secTitleFontSize, dc)
                    drawText(_stationData.BarometerLow.Title, Brushes.Black, DataTitletypeFace, DataTitlerects(2, 5), _secTitleFontSize, dc)
                    drawText("", Brushes.Black, DataTitletypeFace, DataTitlerects(2, 6), _secTitleFontSize, dc)

                    drawText(_stationData.RainRateAnHr.Title, Brushes.Black, DataTitletypeFace, DataTitlerects(3, 1), _secTitleFontSize, dc)
                    drawText("", Brushes.Black, DataTitletypeFace, DataTitlerects(3, 2), _secTitleFontSize, dc)
                    drawText(_stationData.DewPoint.Title, Brushes.Black, DataTitletypeFace, DataTitlerects(3, 3), _secTitleFontSize, dc)
                    drawText(_stationData.RainLongTermTotal.Title, Brushes.Black, DataTitletypeFace, DataTitlerects(3, 4), _secTitleFontSize, dc)
                    drawText(_stationData.HumidityOutCur.Title, Brushes.Black, DataTitletypeFace, DataTitlerects(3, 5), _secTitleFontSize, dc)

                    'dc.DrawRoundedRectangle(grad, new Pen(Brushes.Black, 1), Mainrects[i], 30, 30);
                    'DrawAllText(dc);
                    drawText(_stationData.CloudBase.Title, Brushes.Black, DataTitletypeFace, DataTitlerects(3, 6), _secTitleFontSize, dc)
                End If
            Next
    End Sub
    
    Public Sub drawText(Text As [String], brush As Brush, typeFace As Typeface, rect As Rect, TextSize As Integer, dc As DrawingContext)
        If Text Is Nothing Then
            Text = "null"
        End If
        Dim fText As New FormattedText(Text, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeFace, TextSize, brush)

        Dim centerPoint As Point = getCenterPoint(rect)
        dc.DrawText(fText, New Point(centerPoint.X - (fText.WidthIncludingTrailingWhitespace / 2), centerPoint.Y - (fText.Height / 2)))
    End Sub

    Private Function getCenterPoint(layoutRect As Rect) As Point
        Return New Point(((layoutRect.Right - layoutRect.Left) / 2) + layoutRect.Left, ((layoutRect.Bottom - layoutRect.Top) / 2) + layoutRect.Top)
    End Function

    Public Sub drawTextBottomRightCorner(Text As [String], brush As Brush, typeFace As Typeface, rect As Rect, TextSize As Integer, dc As DrawingContext, _
        margrin As Point)
        Dim fText As New FormattedText(Text, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeFace, TextSize, brush)
        Dim LowerRightRenderPoint As New Point(rect.Right - fText.WidthIncludingTrailingWhitespace - margrin.X, rect.Bottom - fText.Height - margrin.Y)
        dc.DrawText(fText, LowerRightRenderPoint)
    End Sub
    Public Sub drawTextTopRightCorner(Text As [String], brush As Brush, typeFace As Typeface, rect As Rect, TextSize As Integer, dc As DrawingContext, _
        margrin As Point)
        Dim fText As New FormattedText(Text, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeFace, TextSize, brush)
        Dim UpperRightRenderPoint As New Point(rect.Right - fText.WidthIncludingTrailingWhitespace - margrin.X, rect.Top + margrin.Y)
        dc.DrawText(fText, UpperRightRenderPoint)
    End Sub

    Protected Overrides Function GetVisualChild(index As Integer) As Visual
        Return visuals(index)
    End Function
    Private Sub DrawIt_Loaded(sender As Object, e As RoutedEventArgs)
        IntRects()
    End Sub
    Protected Overrides ReadOnly Property VisualChildrenCount() As Integer
        Get
            Return visuals.Count
        End Get
    End Property
    Protected Overrides Sub OnRender(drawingContext As DrawingContext)
        If DirectCast(Me.GetValue(StationDataProperty), StationData) IsNot Nothing Then
            drawBoxes(drawingContext)
            DrawAllText(drawingContext)
        End If
        MyBase.OnRender(drawingContext)
    End Sub
End Class