using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel.Composition;
using api = API.API;

namespace MainView_v2
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    [Export(typeof(api.IView))]
    public partial class UserControl1 : UserControl, api.IView
    {
        //public System.Collections.ObjectModel.ObservableCollection<api.Config> _Configuration;
        public System.Collections.ObjectModel.ObservableCollection<api.Station> _stations;
        String _viewName = "MainView v2";
        int _SelectedStationIndex = -1;
        private System.Windows.Threading.DispatcherTimer UpdateUITimer = new System.Windows.Threading.DispatcherTimer(System.Windows.Threading.DispatcherPriority.Background);

        public UserControl1()
        {
            InitializeComponent();
        }

        public System.Collections.ObjectModel.ObservableCollection<api.Config> Configuration
        {
            get
            {
                return drawItTest._config;
            }
            set
            {
                drawItTest._config = value;
            }
        }

        public void Intilize(System.Collections.ObjectModel.ObservableCollection<api.Station> Stations, ref api.IViewHost StationManager)
        {
            //draw_it = new DrawIt();
            _stations = Stations;
            IntilizeSettings();
            UpdateUITimer.Interval = new TimeSpan(0, 0, 0, 1);
            UpdateUITimer.Tick += new EventHandler(UpdateUITimer_Tick);
            UpdateUITimer.Start();
            //draw_it._stations = Stations;
        }

        private void IntilizeSettings()
        {
            drawItTest._config = new System.Collections.ObjectModel.ObservableCollection<api.Config>();
            drawItTest._config.Add(new api.Config("Units"));
            drawItTest._config[0].ConfigSettings.Add(new api.Setting("Temp Units", api.DataUnitType.Temp_Fahrenheit.ToString()));
            drawItTest._config[0].ConfigSettings[(int)DrawIt.configIndexes.Temp].AllowedParameters = new List<string> { api.DataUnitType.Temp_Fahrenheit.ToString(), api.DataUnitType.Temp_Celsius.ToString() };
            drawItTest._config[0].ConfigSettings.Add(new api.Setting("Wind Units", api.DataUnitType.Wind_Mph.ToString()));
            drawItTest._config[0].ConfigSettings[(int)DrawIt.configIndexes.Wind].AllowedParameters = new List<string> { api.DataUnitType.Wind_Mph.ToString(), api.DataUnitType.Wind_Kph.ToString() };
            drawItTest._config[0].ConfigSettings.Add(new api.Setting("Barometer Units", api.DataUnitType.Bar_InHg.ToString()));
            drawItTest._config[0].ConfigSettings[(int)DrawIt.configIndexes.Bar].AllowedParameters = new List<string> { api.DataUnitType.Bar_InHg.ToString(), api.DataUnitType.Bar_mbar.ToString(), api.DataUnitType.Bar_hpa.ToString() };
            drawItTest._config[0].ConfigSettings.Add(new api.Setting("Rain Units", api.DataUnitType.Precip_Inch.ToString()));
            drawItTest._config[0].ConfigSettings[(int)DrawIt.configIndexes.Precip].AllowedParameters = new List<string> { api.DataUnitType.Precip_Inch.ToString(), api.DataUnitType.Precip_Millimeter.ToString() };
            drawItTest._config[0].ConfigSettings.Add(new api.Setting("Other Units", api.DataUnitType.Other_Feet.ToString()));
            drawItTest._config[0].ConfigSettings[(int)DrawIt.configIndexes.Other].AllowedParameters = new List<string> { api.DataUnitType.Other_Feet.ToString(), api.DataUnitType.Other_Meter.ToString() };
        }

        void UpdateUITimer_Tick(object sender, EventArgs e)
        {
            this.drawItTest.InvalidateVisual();
            //if (_SelectedStationIndex > -1) 
            //{ 
            //    SelectedStationChanged(_SelectedStationIndex); 
            //}
        }

        public void SelectedStationChanged(int SelectedStaitonIndex)
        {
            _SelectedStationIndex = SelectedStaitonIndex;
            if (_stations.Count > 0)
            {
                _SelectedStationIndex = SelectedStaitonIndex;
                if (_SelectedStationIndex > -1)
                { 
                    this.drawItTest.DataContext = _stations[SelectedStaitonIndex].WXStation.WXData;
                }
            }
            //draw_it._selectedStationIndex = SelectedStaitonIndex;
            //draw_it._stationData = _Stations[_selectedStationIndex].WXStation.WXData;

            //this.Dispatcher.BeginInvoke(new DrawIt.DrawAllTextD(draw_it.DrawAllText), _Stations[SelectedStaitonIndex].WXStation.WXData);
            //draw_it.DrawAllText(_Stations[SelectedStaitonIndex].WXStation.WXData);
        }

        public string ViewName
        {
            get
            {
                return _viewName;
            }
            set
            {
                _viewName = value;
            }
        }
    }


    public class DrawIt : FrameworkElement
    {
        private Rect[] Mainrects = new Rect[4];
        private Rect[,] DataTitlerects = new Rect[4, 7];
        private Rect[,] DataRects = new Rect[4, 7];
        private int RenderingAreaWidth = 1200;
        private int RenderingAreaHeight = 675;
        private Typeface MaintypeFace = new Typeface(new FontFamily("Segoe UI"), FontStyles.Oblique, FontWeights.Normal, FontStretches.Normal);
        private Typeface DataTitletypeFace = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Light, FontStretches.Normal);
        private int _mainTitleFontSize = 38;
        private int _mainDataFontSize = 97;
        private int _secTitleFontSize = 18;
        private int _secDataFontSize = 27;
        private int _timeFontSize = 18;
        public VisualCollection visuals;
        public static DependencyProperty StationDataProperty = DependencyProperty.Register("StationData", typeof(api.StationData), typeof(DrawIt), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender | FrameworkPropertyMetadataOptions.AffectsRender));
        public delegate void DrawAllTextD(api.StationData _stationData);
        public enum configIndexes
        {
            Temp,
            Wind,
            Bar,
            Precip,
            Other
        }
        public System.Collections.ObjectModel.ObservableCollection<api.Config> _config;

        public string StationData
        {
            get { return (string)GetValue(StationDataProperty); }
            set { SetValue(StationDataProperty, value); }
        }
        private void IntRects()
        {
            Mainrects[0] = new Rect(Math.Floor(RenderingAreaWidth * 0.025), Math.Floor(RenderingAreaHeight * 0.025), Math.Floor(RenderingAreaWidth * 0.47), Math.Floor(RenderingAreaHeight * 0.45));
            Mainrects[1] = new Rect(Math.Floor(RenderingAreaWidth * 0.505), Math.Floor(RenderingAreaHeight * 0.025), Math.Floor(RenderingAreaWidth * 0.47), Math.Floor(RenderingAreaHeight * 0.45));
            Mainrects[2] = new Rect(Math.Floor(RenderingAreaWidth * 0.025), Math.Floor(RenderingAreaHeight * 0.51), Math.Floor(RenderingAreaWidth * 0.47), Math.Floor(RenderingAreaHeight * 0.45));
            Mainrects[3] = new Rect(Math.Floor(RenderingAreaWidth * 0.505), Math.Floor(RenderingAreaHeight * 0.51), Math.Floor(RenderingAreaWidth * 0.47), Math.Floor(RenderingAreaHeight * 0.45));

            for (int i = 0; i < 4; i++)
            {
                DataTitlerects[i, 0] = new Rect(Mainrects[i].Left, Mainrects[i].Top, Math.Floor(Mainrects[i].Width * 0.65), Math.Floor(Mainrects[i].Height * 0.24));
                DataTitlerects[i, 1] = new Rect(DataTitlerects[i, 0].Right, DataTitlerects[i, 0].Top, Math.Floor(Mainrects[i].Width - DataTitlerects[i, 0].Width), Math.Floor(Mainrects[i].Height * 0.08));
                DataTitlerects[i, 2] = new Rect(DataTitlerects[i, 0].Right, DataTitlerects[i, 0].Bottom, DataTitlerects[i, 1].Width, DataTitlerects[i, 1].Height);
                DataTitlerects[i, 3] = new Rect(DataTitlerects[i, 0].Right, Math.Floor((Mainrects[i].Height * 0.5) + Mainrects[i].Top), DataTitlerects[i, 1].Width, DataTitlerects[i, 1].Height);
                DataTitlerects[i, 4] = new Rect(Mainrects[i].Left, Math.Floor((Mainrects[i].Height * 0.75) + Mainrects[i].Top), Math.Floor(DataTitlerects[i, 0].Width * 0.5), DataTitlerects[i, 1].Height);
                DataTitlerects[i, 5] = new Rect(DataTitlerects[i, 4].Right, Math.Floor((Mainrects[i].Height * 0.75) + Mainrects[i].Top), Math.Ceiling(DataTitlerects[i, 0].Width * 0.5), DataTitlerects[i, 1].Height);
                DataTitlerects[i, 6] = new Rect(DataTitlerects[i, 5].Right, Math.Floor((Mainrects[i].Height * 0.75) + Mainrects[i].Top), DataTitlerects[i, 1].Width, DataTitlerects[i, 1].Height);

                DataRects[i, 0] = new Rect(Mainrects[i].Left, DataTitlerects[i, 0].Bottom, DataTitlerects[i, 0].Width, DataTitlerects[i, 4].Top - DataTitlerects[i, 0].Bottom);
                DataRects[i, 1] = new Rect(DataTitlerects[i, 0].Right, DataTitlerects[i, 1].Bottom, DataTitlerects[i, 1].Width, DataTitlerects[i, 2].Top - DataTitlerects[i, 1].Bottom);
                DataRects[i, 2] = new Rect(DataTitlerects[i, 0].Right, DataTitlerects[i, 2].Bottom, DataTitlerects[i, 2].Width, DataTitlerects[i, 3].Top - DataTitlerects[i, 2].Bottom);
                DataRects[i, 3] = new Rect(DataTitlerects[i, 0].Right, DataTitlerects[i, 3].Bottom, DataTitlerects[i, 3].Width, DataTitlerects[i, 4].Top - DataTitlerects[i, 3].Bottom);
                DataRects[i, 4] = new Rect(DataTitlerects[i, 4].Left, DataTitlerects[i, 4].Bottom, DataTitlerects[i, 4].Width, Mainrects[i].Bottom - DataTitlerects[i, 4].Bottom);
                DataRects[i, 5] = new Rect(DataTitlerects[i, 5].Left, DataTitlerects[i, 5].Bottom, DataTitlerects[i, 5].Width, Mainrects[i].Bottom - DataTitlerects[i, 5].Bottom);
                DataRects[i, 6] = new Rect(DataTitlerects[i, 6].Left, DataTitlerects[i, 6].Bottom, DataTitlerects[i, 6].Width, Mainrects[i].Bottom - DataTitlerects[i, 6].Bottom);
            }
        }

        public DrawIt()
        {
            visuals = new VisualCollection(this);
            this.Loaded += new RoutedEventHandler(DrawIt_Loaded);
            //this.DataContextChanged += DrawIt_DataContextChanged;
        }

        //private void DrawIt_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        //{
        //    DrawAllText(_Stations[_selectedStationIndex].WXStation.WXData);
        //}

        void DCRR(DrawingContext dc, Brush brush, Pen pen, Rect rect, CornerRadius cornerRadius)
        {
            var geometry = new StreamGeometry();
            using (var context = geometry.Open())
            {
                bool isStroked = pen != null;
                const bool isSmoothJoin = true;

                context.BeginFigure(rect.TopLeft + new Vector(0, cornerRadius.TopLeft), brush != null, true);
                context.ArcTo(new Point(rect.TopLeft.X + cornerRadius.TopLeft, rect.TopLeft.Y),
                    new Size(cornerRadius.TopLeft, cornerRadius.TopLeft),
                    90, false, SweepDirection.Clockwise, isStroked, isSmoothJoin);
                context.LineTo(rect.TopRight - new Vector(cornerRadius.TopRight, 0), isStroked, isSmoothJoin);
                context.ArcTo(new Point(rect.TopRight.X, rect.TopRight.Y + cornerRadius.TopRight),
                    new Size(cornerRadius.TopRight, cornerRadius.TopRight),
                    90, false, SweepDirection.Clockwise, isStroked, isSmoothJoin);
                context.LineTo(rect.BottomRight - new Vector(0, cornerRadius.BottomRight), isStroked, isSmoothJoin);
                context.ArcTo(new Point(rect.BottomRight.X - cornerRadius.BottomRight, rect.BottomRight.Y),
                    new Size(cornerRadius.BottomRight, cornerRadius.BottomRight),
                    90, false, SweepDirection.Clockwise, isStroked, isSmoothJoin);
                context.LineTo(rect.BottomLeft + new Vector(cornerRadius.BottomLeft, 0), isStroked, isSmoothJoin);
                context.ArcTo(new Point(rect.BottomLeft.X, rect.BottomLeft.Y - cornerRadius.BottomLeft),
                    new Size(cornerRadius.BottomLeft, cornerRadius.BottomLeft),
                    90, false, SweepDirection.Clockwise, isStroked, isSmoothJoin);

                context.Close();
            }
            dc.DrawGeometry(brush, pen, geometry);
        }


        public void DrawAllText(DrawingContext dc)
        {
            api.StationData _stationData = (api.StationData)this.GetValue(StationDataProperty);
            
            if (_stationData != null)
            {
                
                if (_config[0].ConfigSettings [(int)DrawIt.configIndexes.Temp].CurrentValue == API.API.DataUnitType.Temp_Celsius.ToString())
                {
                    Draw_TempAsMetric(_stationData, dc);
                }
                else
                {
                    Draw_TempAsEnglish(_stationData, dc);
                }

                if (_config[0].ConfigSettings[(int)DrawIt.configIndexes.Wind].CurrentValue == API.API.DataUnitType.Wind_Kph.ToString())
                {
                    Draw_WindAsMetric(_stationData, dc);
                }
                else
                {
                    Draw_WindAsEnglish(_stationData, dc);
                }

                if (_config[0].ConfigSettings[(int)DrawIt.configIndexes.Bar].CurrentValue == API.API.DataUnitType.Bar_mbar.ToString() || _config[0].ConfigSettings[(int)DrawIt.configIndexes.Bar].CurrentValue == API.API.DataUnitType.Bar_hpa.ToString())
                {
                    Draw_BarAsMetric(_stationData, dc);
                }
                else
                {
                    Draw_BarAsEnglish(_stationData, dc);
                }

                if (_config[0].ConfigSettings[(int)DrawIt.configIndexes.Precip].CurrentValue == API.API.DataUnitType.Precip_Millimeter.ToString())
                {
                    Draw_PrecipAsMetric(_stationData, dc);
                }
                else
                {
                    Draw_PrecipAsEnglish(_stationData, dc);
                }

                if (_config[0].ConfigSettings[(int)DrawIt.configIndexes.Other].CurrentValue == API.API.DataUnitType.Other_Meter.ToString())
                {
                    Draw_OtherAsMetric(_stationData, dc);
                }
                else
                {
                    Draw_OtherAsEnglish(_stationData, dc);
                }

                drawTextTopRightCorner(_config[0].ConfigSettings[(int)DrawIt.configIndexes.Temp].CurrentValue.Replace("Temp_",""), Brushes.Black, DataTitletypeFace, DataRects[0, 0], _timeFontSize, dc, new Point(2, 0));
                drawTextTopRightCorner(_config[0].ConfigSettings[(int)DrawIt.configIndexes.Wind].CurrentValue.Replace("Wind_", ""), Brushes.Black, DataTitletypeFace, DataRects[1, 0], _timeFontSize, dc, new Point(2, 0));
                drawTextTopRightCorner(_config[0].ConfigSettings[(int)DrawIt.configIndexes.Bar].CurrentValue.Replace("Bar_", ""), Brushes.Black, DataTitletypeFace, DataRects[2, 0], _timeFontSize, dc, new Point(2, 0));
                drawTextTopRightCorner(_config[0].ConfigSettings[(int)DrawIt.configIndexes.Precip].CurrentValue.Replace("Precip_", ""), Brushes.Black, DataTitletypeFace, DataRects[3, 0], _timeFontSize, dc, new Point(2, 0));
                drawTextTopRightCorner(_config[0].ConfigSettings[(int)DrawIt.configIndexes.Other].CurrentValue.Replace("Other_", ""), Brushes.Black, DataTitletypeFace, DataRects[3, 6], 16, dc, new Point(2, 0));

                drawText(_stationData.UVIndex.AsEnglish, Brushes.Black, DataTitletypeFace, DataRects[0, 6], _secDataFontSize, dc);

                
                drawText(_stationData.WindBearing.AsEnglish, Brushes.Black, DataTitletypeFace, DataRects[1, 4], _secDataFontSize, dc);
                drawText(_stationData.WindDirection.AsEnglish, Brushes.Black, DataTitletypeFace, DataRects[1, 5], _secDataFontSize, dc);
                drawText("", Brushes.Black, DataTitletypeFace, DataRects[1, 6], _secDataFontSize, dc);

                
                drawText(_stationData.ActiveWeather.AsEnglish, Brushes.Black, DataTitletypeFace, DataRects[2, 1], _secDataFontSize, dc);
                drawText(_stationData.SkyConditions.AsEnglish, Brushes.Black, DataTitletypeFace, DataRects[2, 2], _secDataFontSize, dc);
                drawText(_stationData.HumidityOutCur.Value, Brushes.Black, DataTitletypeFace, DataRects[3, 5], _secDataFontSize, dc);
                
            }

        }
        public void Draw_TempAsEnglish(api.StationData _stationData, DrawingContext dc)
        {
            drawText(_stationData.TempOutCur.AsEnglish.ToString(), Brushes.Black, new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), DataRects[0, 0], _mainDataFontSize, dc);
            drawText(_stationData.TempInCur.AsEnglish.ToString(), Brushes.Black, DataTitletypeFace, DataRects[0, 1], _secDataFontSize, dc);
            drawText(_stationData.HeatIndex.AsEnglish.ToString(), Brushes.Black, DataTitletypeFace, DataRects[0, 2], _secDataFontSize, dc);
            drawText(_stationData.WindChill.AsEnglish.ToString(), Brushes.Black, DataTitletypeFace, DataRects[0, 3], _secDataFontSize, dc);
            drawText(_stationData.TempOutHigh.AsEnglish.ToString(), Brushes.Black, DataTitletypeFace, DataRects[0, 4], _secDataFontSize, dc);
            drawTextBottomRightCorner(_stationData.TempOutHigh.TimeOfValue.ToShortTimeString(), Brushes.Black, DataTitletypeFace, DataRects[0, 4], _timeFontSize, dc, new Point(0, 0));
            drawText(_stationData.TempOutLow.AsEnglish.ToString(), Brushes.Black, DataTitletypeFace, DataRects[0, 5], _secDataFontSize, dc);
            drawTextBottomRightCorner(_stationData.TempOutLow.TimeOfValue.ToShortTimeString(), Brushes.Black, DataTitletypeFace, DataRects[0, 5], _timeFontSize, dc, new Point(0, 0));
            drawText(_stationData.DewPoint.AsEnglish.ToString(), Brushes.Black, DataTitletypeFace, DataRects[3, 3], _secDataFontSize, dc);
        }
        private void Draw_TempAsMetric(api.StationData _stationData, DrawingContext dc)
        {
            drawText(_stationData.TempOutCur.AsMetric.ToString(), Brushes.Black, new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), DataRects[0, 0], _mainDataFontSize, dc);
            drawText(_stationData.TempInCur.AsMetric.ToString(), Brushes.Black, DataTitletypeFace, DataRects[0, 1], _secDataFontSize, dc);
            drawText(_stationData.HeatIndex.AsMetric.ToString(), Brushes.Black, DataTitletypeFace, DataRects[0, 2], _secDataFontSize, dc);
            drawText(_stationData.WindChill.AsMetric.ToString(), Brushes.Black, DataTitletypeFace, DataRects[0, 3], _secDataFontSize, dc);
            drawText(_stationData.TempOutHigh.AsMetric.ToString(), Brushes.Black, DataTitletypeFace, DataRects[0, 4], _secDataFontSize, dc);
            drawTextBottomRightCorner(_stationData.TempOutHigh.TimeOfValue.ToShortTimeString(), Brushes.Black, DataTitletypeFace, DataRects[0, 4], _timeFontSize, dc, new Point(0, 0));
            drawText(_stationData.TempOutLow.AsMetric.ToString(), Brushes.Black, DataTitletypeFace, DataRects[0, 5], _secDataFontSize, dc);
            drawTextBottomRightCorner(_stationData.TempOutLow.TimeOfValue.ToShortTimeString(), Brushes.Black, DataTitletypeFace, DataRects[0, 5], _timeFontSize, dc, new Point(0, 0));
            drawText(_stationData.DewPoint.AsMetric.ToString(), Brushes.Black, DataTitletypeFace, DataRects[3, 3], _secDataFontSize, dc);
        }
        private void Draw_WindAsEnglish(api.StationData _stationData, DrawingContext dc)
        {
            drawText(_stationData.WindSpeed.AsEnglish.ToString(), Brushes.Black, new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), DataRects[1, 0], _mainDataFontSize, dc);
            drawText(_stationData.WindPeakGust.AsEnglish.ToString(), Brushes.Black, DataTitletypeFace, DataRects[1, 1], _secDataFontSize, dc);
            drawTextBottomRightCorner(_stationData.WindPeakGust.TimeOfValue.ToShortTimeString(), Brushes.Black, DataTitletypeFace, DataRects[1, 1], _timeFontSize, dc, new Point(0, 0));
            drawText(_stationData.Wind5MinPeak.AsEnglish.ToString(), Brushes.Black, DataTitletypeFace, DataRects[1, 2], _secDataFontSize, dc);
            drawText(_stationData.Wind1MinAvg.AsEnglish.ToString(), Brushes.Black, DataTitletypeFace, DataRects[1, 3], _secDataFontSize, dc);
        }
        private void Draw_WindAsMetric(api.StationData _stationData, DrawingContext dc)
        {
            drawText(_stationData.WindSpeed.AsMetric.ToString(), Brushes.Black, new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), DataRects[1, 0], _mainDataFontSize, dc);
            drawText(_stationData.WindPeakGust.AsMetric.ToString(), Brushes.Black, DataTitletypeFace, DataRects[1, 1], _secDataFontSize, dc);
            drawTextBottomRightCorner(_stationData.WindPeakGust.TimeOfValue.ToShortTimeString(), Brushes.Black, DataTitletypeFace, DataRects[1, 1], _timeFontSize, dc, new Point(0, 0));
            drawText(_stationData.Wind5MinPeak.AsMetric.ToString(), Brushes.Black, DataTitletypeFace, DataRects[1, 2], _secDataFontSize, dc);
            drawText(_stationData.Wind1MinAvg.AsMetric.ToString(), Brushes.Black, DataTitletypeFace, DataRects[1, 3], _secDataFontSize, dc);
        }
        private void Draw_BarAsEnglish(api.StationData _stationData, DrawingContext dc)
        {
            drawText(_stationData.BarometerCur.AsEnglish.ToString(), Brushes.Black, new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), DataRects[2, 0], _mainDataFontSize, dc);
            drawText(_stationData.Barometer3HrChange.AsEnglish.ToString(), Brushes.Black, DataTitletypeFace, DataRects[2, 3], _secDataFontSize, dc);
            drawText(_stationData.BarometerHigh.AsEnglish.ToString(), Brushes.Black, DataTitletypeFace, DataRects[2, 4], _secDataFontSize, dc);
            drawTextBottomRightCorner(_stationData.BarometerHigh.TimeOfValue.ToShortTimeString(), Brushes.Black, DataTitletypeFace, DataRects[2, 4], _timeFontSize, dc, new Point(0, 0));
            drawText(_stationData.BarometerLow.AsEnglish.ToString(), Brushes.Black, DataTitletypeFace, DataRects[2, 5], _secDataFontSize, dc);
            drawTextBottomRightCorner(_stationData.BarometerLow.TimeOfValue.ToShortTimeString(), Brushes.Black, DataTitletypeFace, DataRects[2, 5], _timeFontSize, dc, new Point(0, 0));
            drawText("", Brushes.Black, DataTitletypeFace, DataRects[2, 6], _secDataFontSize, dc);
        }
        private void Draw_BarAsMetric(api.StationData _stationData, DrawingContext dc)
        {
            drawText(_stationData.BarometerCur.AsMetric.ToString(), Brushes.Black, new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), DataRects[2, 0], _mainDataFontSize, dc);
            drawText(_stationData.Barometer3HrChange.AsMetric.ToString(), Brushes.Black, DataTitletypeFace, DataRects[2, 3], _secDataFontSize, dc);
            drawText(_stationData.BarometerHigh.AsMetric.ToString(), Brushes.Black, DataTitletypeFace, DataRects[2, 4], _secDataFontSize, dc);
            drawTextBottomRightCorner(_stationData.BarometerHigh.TimeOfValue.ToShortTimeString(), Brushes.Black, DataTitletypeFace, DataRects[2, 4], _timeFontSize, dc, new Point(0, 0));
            drawText(_stationData.BarometerLow.AsMetric.ToString(), Brushes.Black, DataTitletypeFace, DataRects[2, 5], _secDataFontSize, dc);
            drawTextBottomRightCorner(_stationData.BarometerLow.TimeOfValue.ToShortTimeString(), Brushes.Black, DataTitletypeFace, DataRects[2, 5], _timeFontSize, dc, new Point(0, 0));
            drawText("", Brushes.Black, DataTitletypeFace, DataRects[2, 6], _secDataFontSize, dc);
        }
        private void Draw_PrecipAsEnglish(api.StationData _stationData, DrawingContext dc)
        {
            drawText(_stationData.RainTodayTotal.AsEnglish.ToString(), Brushes.Black, new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), DataRects[3, 0], _mainDataFontSize, dc);
            drawText(_stationData.RainRateAnHr.AsEnglish.ToString(), Brushes.Black, DataTitletypeFace, DataRects[3, 1], _secDataFontSize, dc);
            drawText("", Brushes.Black, DataTitletypeFace, DataRects[3, 2], _secDataFontSize, dc);
            drawText(_stationData.RainLongTermTotal.AsEnglish.ToString(), Brushes.Black, DataTitletypeFace, DataRects[3, 4], _secDataFontSize, dc);
        }
        private void Draw_PrecipAsMetric(api.StationData _stationData, DrawingContext dc)
        {
            drawText(_stationData.RainTodayTotal.AsMetric.ToString(), Brushes.Black, new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), DataRects[3, 0], _mainDataFontSize, dc);
            drawText(_stationData.RainRateAnHr.AsMetric.ToString(), Brushes.Black, DataTitletypeFace, DataRects[3, 1], _secDataFontSize, dc);
            drawText("", Brushes.Black, DataTitletypeFace, DataRects[3, 2], _secDataFontSize, dc);
            drawText(_stationData.RainLongTermTotal.AsMetric.ToString(), Brushes.Black, DataTitletypeFace, DataRects[3, 4], _secDataFontSize, dc);
        }
        private void Draw_OtherAsEnglish(api.StationData _stationData, DrawingContext dc)
        {
            drawText(_stationData.CloudBase.AsEnglish.ToString(), Brushes.Black, DataTitletypeFace, DataRects[3, 6], _secDataFontSize, dc);
        }
        private void Draw_OtherAsMetric(api.StationData _stationData, DrawingContext dc)
        {
            drawText(_stationData.CloudBase.AsMetric.ToString(), Brushes.Black, DataTitletypeFace, DataRects[3, 6], _secDataFontSize, dc);
        }


        public void drawBoxes(DrawingContext dc)
        {
            LinearGradientBrush grad = new LinearGradientBrush();
            grad.GradientStops.Add(new GradientStop(Color.FromArgb(204, 255, 255, 255), 0));
            grad.GradientStops.Add(new GradientStop(Color.FromArgb(204, 255, 255, 255), 0.188));
            grad.GradientStops.Add(new GradientStop(Color.FromArgb(89, 255, 255, 255), 0.355));
            grad.GradientStops.Add(new GradientStop(Color.FromArgb(89, 255, 255, 255), 1));
            grad.StartPoint = new Point(0.5, 0);
            grad.EndPoint = new Point(0.5, 0.1);
            grad.Opacity = 0.6;
            grad.SpreadMethod = GradientSpreadMethod.Pad;

            CornerRadius TitleCNRad = new CornerRadius(0, 20, 0, 0);
            CornerRadius DataCNRad = new CornerRadius(0, 0, 20, 0);
            api.StationData _stationData = (api.StationData)this.GetValue(StationDataProperty);
            for (int i = 0; i < 4; i++)
            {
                dc.DrawRoundedRectangle(grad, new Pen(Brushes.Black, 1), Mainrects[i], 30, 30);
                for (int q = 0; q < 7; q++)
                {
                    switch (q)
                    {
                        case 0:
                            {
                                TitleCNRad = new CornerRadius(30, 0, 0, 0);
                                DataCNRad = new CornerRadius(0, 0, 0, 0);
                                break;
                            }
                        case 1:
                            {
                                TitleCNRad = new CornerRadius(0, 30, 0, 0);
                                DataCNRad = new CornerRadius(0, 0, 0, 0);
                                break;
                            }
                        case 4:
                            {
                                TitleCNRad = new CornerRadius(0, 0, 0, 0);
                                DataCNRad = new CornerRadius(0, 0, 0, 30);
                                break;
                            }
                        case 6:
                            {
                                TitleCNRad = new CornerRadius(0, 0, 0, 0);
                                DataCNRad = new CornerRadius(0, 0, 30, 0);
                                break;
                            }
                        default:
                            {
                                TitleCNRad = new CornerRadius(0, 0, 0, 0);
                                DataCNRad = new CornerRadius(0, 0, 0, 0);
                                break;
                            }
                    }
                    DCRR(dc, new SolidColorBrush(Color.FromArgb(0, 0, 50, 250)), new Pen(Brushes.Black, 0.5), DataTitlerects[i, q], TitleCNRad);
                    DCRR(dc, new SolidColorBrush(Color.FromArgb(0, 0, 200, 50)), new Pen(Brushes.Black, 0.5), DataRects[i, q], DataCNRad);
                }
                if (_stationData != null)
                {
                drawText(_stationData.TempOutCur.Title, Brushes.Black, MaintypeFace, DataTitlerects[0, 0], _mainTitleFontSize, dc);
                drawText(_stationData.WindSpeed.Title, Brushes.Black, MaintypeFace, DataTitlerects[1, 0], _mainTitleFontSize, dc);
                drawText(_stationData.BarometerCur.Title, Brushes.Black, MaintypeFace, DataTitlerects[2, 0], _mainTitleFontSize, dc);
                drawText(_stationData.RainTodayTotal.Title, Brushes.Black, MaintypeFace, DataTitlerects[3, 0], _mainTitleFontSize, dc);

                drawText(_stationData.TempInCur.Title, Brushes.Black, DataTitletypeFace, DataTitlerects[0, 1], _secTitleFontSize, dc);
                drawText(_stationData.HeatIndex.Title, Brushes.Black, DataTitletypeFace, DataTitlerects[0, 2], _secTitleFontSize, dc);
                drawText(_stationData.WindChill.Title, Brushes.Black, DataTitletypeFace, DataTitlerects[0, 3], _secTitleFontSize, dc);
                drawText(_stationData.TempOutHigh.Title, Brushes.Black, DataTitletypeFace, DataTitlerects[0, 4], _secTitleFontSize, dc);
                drawText(_stationData.TempOutLow.Title, Brushes.Black, DataTitletypeFace, DataTitlerects[0, 5], _secTitleFontSize, dc);
                drawText(_stationData.UVIndex.Title, Brushes.Black, DataTitletypeFace, DataTitlerects[0, 6], _secTitleFontSize, dc);

                drawText(_stationData.WindPeakGust.Title, Brushes.Black, DataTitletypeFace, DataTitlerects[1, 1], _secTitleFontSize, dc);
                drawText(_stationData.Wind5MinPeak.Title, Brushes.Black, DataTitletypeFace, DataTitlerects[1, 2], _secTitleFontSize, dc);
                drawText(_stationData.Wind1MinAvg.Title, Brushes.Black, DataTitletypeFace, DataTitlerects[1, 3], _secTitleFontSize, dc);
                drawText(_stationData.WindDirection.Title, Brushes.Black, DataTitletypeFace, DataTitlerects[1, 4], _secTitleFontSize, dc);
                drawText(_stationData.WindBearing.Title, Brushes.Black, DataTitletypeFace, DataTitlerects[1, 5], _secTitleFontSize, dc);
                drawText("", Brushes.Black, DataTitletypeFace, DataTitlerects[1, 6], _secTitleFontSize, dc);

                drawText(_stationData.ActiveWeather.Title, Brushes.Black, DataTitletypeFace, DataTitlerects[2, 1], _secTitleFontSize, dc);
                drawText(_stationData.SkyConditions.Title, Brushes.Black, DataTitletypeFace, DataTitlerects[2, 2], _secTitleFontSize, dc);
                drawText(_stationData.Barometer3HrChange.Title, Brushes.Black, DataTitletypeFace, DataTitlerects[2, 3], _secTitleFontSize, dc);
                drawText(_stationData.BarometerHigh.Title, Brushes.Black, DataTitletypeFace, DataTitlerects[2, 4], _secTitleFontSize, dc);
                drawText(_stationData.BarometerLow.Title, Brushes.Black, DataTitletypeFace, DataTitlerects[2, 5], _secTitleFontSize, dc);
                drawText("", Brushes.Black, DataTitletypeFace, DataTitlerects[2, 6], _secTitleFontSize, dc);

                drawText(_stationData.RainRateAnHr.Title, Brushes.Black, DataTitletypeFace, DataTitlerects[3, 1], _secTitleFontSize, dc);
                drawText("", Brushes.Black, DataTitletypeFace, DataTitlerects[3, 2], _secTitleFontSize, dc);
                drawText(_stationData.DewPoint.Title, Brushes.Black, DataTitletypeFace, DataTitlerects[3, 3], _secTitleFontSize, dc);
                drawText(_stationData.RainLongTermTotal.Title, Brushes.Black, DataTitletypeFace, DataTitlerects[3, 4], _secTitleFontSize, dc);
                drawText(_stationData.HumidityOutCur.Title, Brushes.Black, DataTitletypeFace, DataTitlerects[3, 5], _secTitleFontSize, dc);
                drawText(_stationData.CloudBase.Title, Brushes.Black, DataTitletypeFace, DataTitlerects[3, 6], _secTitleFontSize, dc);

                //dc.DrawRoundedRectangle(grad, new Pen(Brushes.Black, 1), Mainrects[i], 30, 30);
                //DrawAllText(dc);
                }
            }
        }
        void DrawIt_Loaded(object sender, RoutedEventArgs e)
        {
            IntRects();
            //LinearGradientBrush grad = new LinearGradientBrush();
            //grad.GradientStops.Add(new GradientStop(Color.FromArgb(204, 255, 255, 255), 0));
            //grad.GradientStops.Add(new GradientStop(Color.FromArgb(204, 255, 255, 255), 0.188));
            //grad.GradientStops.Add(new GradientStop(Color.FromArgb(89, 255, 255, 255), 0.355));
            //grad.GradientStops.Add(new GradientStop(Color.FromArgb(89, 255, 255, 255), 1));
            //grad.StartPoint = new Point(0.5, 0);
            //grad.EndPoint = new Point(0.5, 0.1);
            //grad.Opacity = 0.6;
            //grad.SpreadMethod = GradientSpreadMethod.Pad;

            //DrawingVisual visual = new DrawingVisual();
            //CornerRadius TitleCNRad = new CornerRadius(0, 20, 0, 0);
            //CornerRadius DataCNRad = new CornerRadius(0, 0, 20, 0);
            //using (DrawingContext dc = visual.RenderOpen())
            //{
            //    for (int i = 0; i < 4; i++)
            //    {
            //        //dc.DrawRoundedRectangle(grad, new Pen(Brushes.Black, 1), Mainrects[i], 30, 30);
            //        for (int q = 0; q < 7; q++)
            //        {
            //            switch (q)
            //            {
            //                case 0:
            //                    {
            //                        TitleCNRad = new CornerRadius(30, 0, 0, 0);
            //                        DataCNRad = new CornerRadius(0, 0, 0, 0);
            //                        break;
            //                    }
            //                case 1:
            //                    {
            //                        TitleCNRad = new CornerRadius(0, 30, 0, 0);
            //                        DataCNRad = new CornerRadius(0, 0, 0, 0);
            //                        break;
            //                    }
            //                case 4:
            //                    {
            //                        TitleCNRad = new CornerRadius(0, 0, 0, 0);
            //                        DataCNRad = new CornerRadius(0, 0, 0, 30);
            //                        break;
            //                    }
            //                case 6:
            //                    {
            //                        TitleCNRad = new CornerRadius(0, 0, 0, 0);
            //                        DataCNRad = new CornerRadius(0, 0, 30, 0);
            //                        break;
            //                    }
            //                default:
            //                    {
            //                        TitleCNRad = new CornerRadius(0, 0, 0, 0);
            //                        DataCNRad = new CornerRadius(0, 0, 0, 0);
            //                        break;
            //                    }
            //            }
            //            DCRR(dc, new SolidColorBrush(Color.FromArgb(0, 0, 50, 200)), new Pen(Brushes.Black, 0.5), DataTitlerects[i, q], TitleCNRad);
            //            DCRR(dc, Brushes.Transparent, new Pen(Brushes.Black, 0.5), DataRects[i, q], DataCNRad);
            //        }
            //        drawText("Temperature", Brushes.Black, MaintypeFace, DataTitlerects[0, 0], 50, 0, dc);
            //        drawText("Wind", Brushes.Black, MaintypeFace, DataTitlerects[1, 0], 50, 0, dc);
            //        drawText("Barometer", Brushes.Black, MaintypeFace, DataTitlerects[2, 0], 50, 0, dc);
            //        drawText("Rainfall", Brushes.Black, MaintypeFace, DataTitlerects[3, 0], 50, 0, dc);

            //        drawText("Inside Temperature", Brushes.Black, DataTitletypeFace, DataTitlerects[0, 1], 20, 0, dc);
            //        drawText("Heat Index", Brushes.Black, DataTitletypeFace, DataTitlerects[0, 2], 20, 0, dc);
            //        drawText("Wind Chill", Brushes.Black, DataTitletypeFace, DataTitlerects[0, 3], 20, 0, dc);
            //        drawText("High Temp", Brushes.Black, DataTitletypeFace, DataTitlerects[0, 4], 20, 0, dc);
            //        drawText("Low Temp", Brushes.Black, DataTitletypeFace, DataTitlerects[0, 5], 20, 0, dc);
            //        drawText("UV Index", Brushes.Black, DataTitletypeFace, DataTitlerects[0, 6], 20, 0, dc);

            //        drawText("Peak Wind Gust", Brushes.Black, DataTitletypeFace, DataTitlerects[1, 1], 20, 0, dc);
            //        drawText("5 Min Wind Peak", Brushes.Black, DataTitletypeFace, DataTitlerects[1, 2], 20, 0, dc);
            //        drawText("1 Min Wind Average", Brushes.Black, DataTitletypeFace, DataTitlerects[1, 3], 20, 0, dc);
            //        drawText("Wind Direction", Brushes.Black, DataTitletypeFace, DataTitlerects[1, 4], 20, 0, dc);
            //        drawText("Wind Bearing", Brushes.Black, DataTitletypeFace, DataTitlerects[1, 5], 20, 0, dc);
            //        drawText("", Brushes.Black, DataTitletypeFace, DataTitlerects[1, 6], 20, 0, dc);

            //        drawText("Active Weather", Brushes.Black, DataTitletypeFace, DataTitlerects[2, 1], 20, 0, dc);
            //        drawText("Sky Conditions", Brushes.Black, DataTitletypeFace, DataTitlerects[2, 2], 20, 0, dc);
            //        drawText("3 Hr Pressure Change", Brushes.Black, DataTitletypeFace, DataTitlerects[2, 3], 20, 0, dc);
            //        drawText("High Barometer", Brushes.Black, DataTitletypeFace, DataTitlerects[2, 4], 20, 0, dc);
            //        drawText("Low Barometer", Brushes.Black, DataTitletypeFace, DataTitlerects[2, 5], 20, 0, dc);
            //        drawText("", Brushes.Black, DataTitletypeFace, DataTitlerects[2, 6], 20, 0, dc);

            //        drawText("Precip Last Hour", Brushes.Black, DataTitletypeFace, DataTitlerects[3, 1], 20, 0, dc);
            //        drawText("", Brushes.Black, DataTitletypeFace, DataTitlerects[3, 2], 20, 0, dc);
            //        drawText("Dew Point", Brushes.Black, DataTitletypeFace, DataTitlerects[3, 3], 20, 0, dc);
            //        drawText("Rainfall This Year", Brushes.Black, DataTitletypeFace, DataTitlerects[3, 4], 20, 0, dc);
            //        drawText("Outdoor Humidity", Brushes.Black, DataTitletypeFace, DataTitlerects[3, 5], 20, 0, dc);
            //        drawText("Cloud Base", Brushes.Black, DataTitletypeFace, DataTitlerects[3, 6], 20, 0, dc);

            //        //dc.DrawRoundedRectangle(grad, new Pen(Brushes.Black, 1), Mainrects[i], 30, 30);
            //        //DrawAllText(dc);
            //    }
            //}
            //if (visuals.Count == 0)
            //{
            //    visuals.Add(visual);
            //}
        }
        public void drawText(String Text, Brush brush, Typeface typeFace, Rect rect, int TextSize, DrawingContext dc)
        {
            if (Text == null)
            {
                Text = "null";
            }
            FormattedText fText = new FormattedText(Text, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeFace, TextSize, brush);

            Point centerPoint = getCenterPoint(rect);
            dc.DrawText(fText, new Point(centerPoint.X - (fText.WidthIncludingTrailingWhitespace / 2), centerPoint.Y - (fText.Height / 2)));
        }

        Point getCenterPoint(Rect layoutRect)
        {
            return new Point(((layoutRect.Right - layoutRect.Left) / 2) + layoutRect.Left, ((layoutRect.Bottom - layoutRect.Top) / 2) + layoutRect.Top);
        }

        public void drawTextBottomRightCorner(String Text, Brush brush, Typeface typeFace, Rect rect, int TextSize, DrawingContext dc, Point margrin)
        {
            FormattedText fText = new FormattedText(Text, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeFace, TextSize, brush);
            Point LowerRightRenderPoint = new Point(rect.Right - fText.WidthIncludingTrailingWhitespace - margrin.X, rect.Bottom - fText.Height - margrin.Y);
            dc.DrawText(fText, LowerRightRenderPoint);
        }
        public void drawTextTopRightCorner(String Text, Brush brush, Typeface typeFace, Rect rect, int TextSize, DrawingContext dc, Point margrin)
        {
            FormattedText fText = new FormattedText(Text, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeFace, TextSize, brush);
            Point UpperRightRenderPoint = new Point(rect.Right - fText.WidthIncludingTrailingWhitespace - margrin.X, rect.Top + margrin.Y);
            dc.DrawText(fText, UpperRightRenderPoint);
        }

        protected override Visual GetVisualChild(int index)
        {
            return visuals[index];
        }

        protected override int VisualChildrenCount
        {
            get
            {
                return visuals.Count;
            }
        }
        protected override void OnRender(DrawingContext drawingContext)
        {
            if ((api.StationData)this.GetValue(StationDataProperty) != null)
            { 
                drawBoxes(drawingContext);
                DrawAllText(drawingContext);
            }
            base.OnRender(drawingContext);
        }
    }

}
