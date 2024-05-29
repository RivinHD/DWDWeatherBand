using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using static DWDWeatherBand.Settings;

namespace DWDWeatherBand
{
    /// <summary>
    /// Interaction logic for InformationPopup.xaml
    /// </summary>
    public partial class InformationPopup : UserControl, INotifyPropertyChanged
    {
        public static DependencyProperty ParentPopupProperty = DependencyProperty.Register("ParentPopup", typeof(Popup), typeof(InformationPopup));

        Settings.Properties property;
        private bool initMode = true;
        private bool captured = false;
        private bool indicatorEnabled = false;
        bool isDisposed = false;
        DispatcherTimer errorTimer;
        public InformationPopup()
        {
            property = Settings.LoadedProperties;
            InitializeComponent();
            initMode = false;
            errorTimer = new DispatcherTimer();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public Popup ParentPopup
        {
            get { return (Popup)GetValue(ParentPopupProperty); }
            set { SetValue(ParentPopupProperty, value); }
        }

        protected void Dispose(object sender, EventArgs e)
        {
            isDisposed = true;
            errorTimer.Stop();
        }

        private void TabItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (ParentPopup != null)
            {
                ParentPopup.StaysOpen = true;
                TabItem tab = (TabItem)sender;
                if (tab.IsSelected)
                {
                    return;
                }
                captured = true;
                Mouse.Capture(tab, CaptureMode.Element);
                Console.WriteLine("Captured");
            }
        }

        private void TabItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (ParentPopup != null)
            {
                ParentPopup.StaysOpen = false;
                if (captured)
                {
                    captured = false;
                    Mouse.Capture(null, CaptureMode.None);
                    Console.WriteLine("Uncaptured");
                }
            }
        }
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region DWDWeather Tab

        private TextBlock[] overviewDate;
        private TextBlock[] overviewTemperaturMinMax;
        private TextBlock[] overviewPrecipitation;
        private Label[] temperatureScale;
        private Label[] precipitationScale;
        private Label[] windScale;
        private Label[] humidityScale;
        private Label[] precipitationProbabilityScale;
        private RotateTransform[] windDirectionScale;
        private Label[] timeScale;
        private IDrawData drawDataTemperatur;
        private IDrawData drawDataPrecipitation;
        private IDrawData drawDataPrecipitationProbability;
        private IDrawData drawDataWind;
        private IDrawData drawDataWindMax;
        private IDrawData drawDataWindDirection;
        private IDrawData drawDataHumidity;
        private bool dataLoaded = false;
        private double currentTime = 0;
        private int currentYear = DateTime.UtcNow.Year;
        private double offset = 0; // in minutes

        private void WeatherDisplay_Loaded(object sender, RoutedEventArgs e)
        {
            dataLoaded = false;
            if (!ParentPopup.IsOpen)
            {
                return;
            }

            overviewDate = new TextBlock[9]
            {
                Overview0_Date,
                Overview1_Date,
                Overview2_Date,
                Overview3_Date,
                Overview4_Date,
                Overview5_Date,
                Overview6_Date,
                Overview7_Date,
                Overview8_Date
            };
            overviewTemperaturMinMax = new TextBlock[9]
            {
                Overview0_TemperaturMinMax,
                Overview1_TemperaturMinMax,
                Overview2_TemperaturMinMax,
                Overview3_TemperaturMinMax,
                Overview4_TemperaturMinMax,
                Overview5_TemperaturMinMax,
                Overview6_TemperaturMinMax,
                Overview7_TemperaturMinMax,
                Overview8_TemperaturMinMax
            };
            overviewPrecipitation = new TextBlock[9]
            {
                Overview0_Precipitation,
                Overview1_Precipitation,
                Overview2_Precipitation,
                Overview3_Precipitation,
                Overview4_Precipitation,
                Overview5_Precipitation,
                Overview6_Precipitation,
                Overview7_Precipitation,
                Overview8_Precipitation
            };
            temperatureScale = new Label[] {
                LabelTemperatur0,
                LabelTemperatur1,
                LabelTemperatur2,
                LabelTemperatur3,
                LabelTemperatur4,
                LabelTemperatur5
            };
            precipitationScale = new Label[] {
                LabelPrecipitation0,
                LabelPrecipitation1,
                LabelPrecipitation2,
                LabelPrecipitation3,
                LabelPrecipitation4,
                LabelPrecipitation5
            }; 
            windScale = new Label[] {
                LabelWind0,
                LabelWind1,
                LabelWind2,
                LabelWind3,
                LabelWind4,
                LabelWind5
            };
            humidityScale = new Label[] {
                LabelHumidity0,
                LabelHumidity1,
                LabelHumidity2,
                LabelHumidity3,
                LabelHumidity4,
                LabelHumidity5
            };
            precipitationProbabilityScale = new Label[]
            {
                PrecipitationProbability0,
                PrecipitationProbability1,
                PrecipitationProbability2,
                PrecipitationProbability3,
                PrecipitationProbability4,
                PrecipitationProbability5,
                PrecipitationProbability6,
                PrecipitationProbability7,
                PrecipitationProbability8,
                PrecipitationProbability9,
                PrecipitationProbability10,
                PrecipitationProbability11,
                PrecipitationProbability12,
                PrecipitationProbability13,
                PrecipitationProbability14,
                PrecipitationProbability15,
                PrecipitationProbability16,
                PrecipitationProbability17,
                PrecipitationProbability18,
                PrecipitationProbability19,
                PrecipitationProbability20,
                PrecipitationProbability21,
                PrecipitationProbability22,
                PrecipitationProbability23,
                PrecipitationProbability24
            };
            windDirectionScale = new RotateTransform[]
            {
                WindImageRotation0,
                WindImageRotation1,
                WindImageRotation2,
                WindImageRotation3,
                WindImageRotation4,
                WindImageRotation5,
                WindImageRotation6,
                WindImageRotation7,
                WindImageRotation8,
                WindImageRotation9,
                WindImageRotation10,
                WindImageRotation11,
                WindImageRotation12,
                WindImageRotation13,
                WindImageRotation14,
                WindImageRotation15,
                WindImageRotation16,
                WindImageRotation17,
                WindImageRotation18,
                WindImageRotation19,
                WindImageRotation20,
                WindImageRotation21,
                WindImageRotation22,
                WindImageRotation23,
                WindImageRotation24
            };
            timeScale = new Label[]
            {
                LabelTime0,
                LabelTime1,
                LabelTime2,
                LabelTime3,
                LabelTime4,
                LabelTime5,
                LabelTime6,
                LabelTime7,
                LabelTime8,
                LabelTime9,
                LabelTime10,
                LabelTime11,
                LabelTime12,
                LabelTime13,
                LabelTime14,
                LabelTime15,
                LabelTime16,
                LabelTime17,
                LabelTime18,
                LabelTime19,
                LabelTime20,
                LabelTime21,
                LabelTime22,
                LabelTime23,
                LabelTime24
            };

            CurrentTempratur.Content = "- °C";
            CurrentPrecipitation.Content = "- mm/h";
            CurrentWind.Content = "- km/h";
            CurrentHumidity.Content = "- %";
            CurrentTime.Content = "-";


            errorTimer.Interval = TimeSpan.FromSeconds(property.ErrorIntervall);
            errorTimer.Tick += new EventHandler(TickError);

            LoadData();
        }

        void TickError(object sender, EventArgs e)
        {
            dataLoaded = false;
            if(isDisposed)
            {
                errorTimer.Stop();
                return;
            }
            LoadData();
        }

        private async void LoadData()
        {
            errorTimer.Stop();
            LoadingLabel.Visibility = Visibility.Visible;
            if (!DWDWeather.FinishedInit)
            {
                errorTimer.Start();
                return;
            }

            bool success = false;
            Task<bool> updateForcast = DWDWeather.UpdateForcast();
            if (DWDWeather.AbsoluteLow == null || DWDWeather.AbsoluteHigh == null)
            {
                success = await DWDWeather.UpdateHighLow(); 
                if (!success)
                {
                    errorTimer.Start();
                    return;
                }
            }
            
            DWDWeather.Item low = DWDWeather.AbsoluteLow;
            DWDWeather.Item high = DWDWeather.AbsoluteHigh;

            DateTime now = DateTime.UtcNow;
            currentTime = (now.DayOfYear * 24 + now.Hour) * 60; // in Minutes
            double currentHour = currentTime + offset; // in Minutes
            double currentHourMax = currentHour + 24 * 60;
            Rectangle referencRect = new Rectangle()
            {
                Fill = FindResource("HumidityColor") as SolidColorBrush,
                Stroke = FindResource("BackgroundColor") as SolidColorBrush,
                StrokeThickness = 0.5,
                IsHitTestVisible = false,
                Width = 15
            };

            drawDataTemperatur = new DrawDataAsPath(gridCanvasTP, TemperaturGraph, BrushType.LinearGradientBrush)
            {
                MaxDataValueX = currentHourMax,
                MinDataValueX = currentHour,
                MaxDataValueY = GetUpperBound(high.Temperature, 5),
                MinDataValueY = GetLowerBound(low.Temperature, 5),
            };

            drawDataPrecipitation = new DrawDataAsRectangle(canvasPrecipitation, referencRect, BrushType.SolidColorBrush)
            {
                MaxDataValueX = currentHourMax,
                MinDataValueX = currentHour,
                MaxDataValueY = Math.Min(GetUpperBound(high.Precipitation, 5), 100),
                MinDataValueY = Math.Max((int)(low.Precipitation / 5) * 5, 0),
            };

            drawDataPrecipitationProbability = new VirtualData(canvasPrecipitation)
            {
                MaxDataValueX = currentHourMax,
                MinDataValueX = currentHour,
                MaxDataValueY = 100,
                MinDataValueY = 0,
            };

            drawDataWind = new DrawDataAsPath(gridCanvasWH, WindGraph, BrushType.SolidColorBrush)
            {
                MaxDataValueX = currentHourMax,
                MinDataValueX = currentHour,
                MaxDataValueY = GetUpperBound(high.MaxWind, 5),
                MinDataValueY = GetLowerBound(low.Wind, 5),
            };

            drawDataWindMax = new DrawDataAsPath(gridCanvasWH, WindMaxGraph, BrushType.SolidColorBrush)
            {
                MaxDataValueX = currentHourMax,
                MinDataValueX = currentHour,
                MaxDataValueY = GetUpperBound(high.MaxWind, 5),
                MinDataValueY = GetLowerBound(low.Wind, 5),
            };

            drawDataWindDirection = new VirtualData(gridCanvasWH)
            {
                MaxDataValueX = currentHourMax,
                MinDataValueX = currentHour,
                MaxDataValueY = 360,
                MinDataValueY = 0,
            };

            drawDataHumidity = new DrawDataAsRectangle(canvasHumidity, referencRect, BrushType.SolidColorBrush)
            {
                MaxDataValueX = currentHourMax,
                MinDataValueX = currentHour,
                MaxDataValueY = Math.Min(GetUpperBound(high.Humidity, 5), 100),
                MinDataValueY = Math.Max((int)(low.Humidity / 5) * 5, 0),
            };


            success = await updateForcast;
            if (!success)
            {
                errorTimer.Start();
                return;
            }
            LoadingLabel.Visibility = Visibility.Collapsed;

            KeyValuePair<double, double>[] parsedDataTemperature = new KeyValuePair<double, double>[DWDWeather.Forcast.Length];
            KeyValuePair<double, double>[] parsedDataPrecipitation = new KeyValuePair<double, double>[DWDWeather.Forcast.Length];
            KeyValuePair<double, double>[] parsedDataPrecipitationProbability = new KeyValuePair<double, double>[DWDWeather.Forcast.Length];
            KeyValuePair<double, double>[] parsedDataWind = new KeyValuePair<double, double>[DWDWeather.Forcast.Length];
            KeyValuePair<double, double>[] parsedDataWindMax = new KeyValuePair<double, double>[DWDWeather.Forcast.Length];
            KeyValuePair<double, double>[] parsedDataWindDirection = new KeyValuePair<double, double>[DWDWeather.Forcast.Length];
            KeyValuePair<double, double>[] parsedDataHumidity = new KeyValuePair<double, double>[DWDWeather.Forcast.Length];

            for (int i = 0; i < DWDWeather.Forcast.Length; i++)
            {
                DWDWeather.ItemTimedForcast item = DWDWeather.Forcast[i];
                double yearChanged = 0;
                if (item.Time.Year != now.Year)
                {
                    yearChanged = (new DateTime(item.Time.Year, 1, 1) - new DateTime(now.Year, 1, 1)).TotalMinutes;
                }
                double time = (item.Time.DayOfYear * 24 + item.Time.Hour) * 60 + item.Time.Minute + yearChanged;
                parsedDataTemperature[i] = new KeyValuePair<double, double>(time, item.Temperature);
                parsedDataPrecipitation[i] = new KeyValuePair<double, double>(time, item.Precipitation);
                parsedDataPrecipitationProbability[i] = new KeyValuePair<double, double>(time, item.PrecipitationProbability);
                parsedDataWind[i] = new KeyValuePair<double, double>(time, item.Wind);
                parsedDataWindMax[i] = new KeyValuePair<double, double>(time, item.MaxWind);
                parsedDataWindDirection[i] = new KeyValuePair<double, double>(time, item.WindDirection);
                parsedDataHumidity[i] = new KeyValuePair<double, double>(time, item.Humidity);
            }

            drawDataTemperatur.SetData(parsedDataTemperature);
            drawDataPrecipitation.SetData(parsedDataPrecipitation);
            drawDataPrecipitationProbability.SetData(parsedDataPrecipitationProbability);
            drawDataWind.SetData(parsedDataWind);
            drawDataWindMax.SetData(parsedDataWindMax);
            drawDataWindDirection.SetData(parsedDataWindDirection);
            drawDataHumidity.SetData(parsedDataHumidity);

            CultureInfo culture = CultureInfo.InvariantCulture;
            double[] xScaleNames = drawDataTemperatur.XScaleNames(timeScale.Length); 
            currentYear = DateTime.UtcNow.Year;
            double lastYear = (new DateTime(currentYear - 1, 1, 1) - new DateTime(currentYear, 1, 1)).TotalMinutes;
            double nextYear = (new DateTime(currentYear + 1, 1, 1) - new DateTime(currentYear, 1, 1)).TotalMinutes;
            for (int i = 0; i < timeScale.Length; i++)
            {
                double time = xScaleNames[i];
                int yearChange = time < 0 ? -1 : (time < nextYear ? 0 : 1);
                time += yearChange * (yearChange == -1 ? lastYear : nextYear);
                DateTime dateTime = new DateTime(currentYear + yearChange, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMinutes(time).AddDays(-1);
                timeScale[i].Content = $"{dateTime.ToLocalTime():HH}";
            }

            double[] yScaleNames = drawDataTemperatur.YScaleNames(temperatureScale.Length);
            for(int i = 0; i < temperatureScale.Length; i++)
            {
                temperatureScale[i].Content = $"{yScaleNames[i].ToString("F1", culture)} °C";
            }
            yScaleNames = drawDataPrecipitation.YScaleNames(precipitationScale.Length);
            for (int i = 0; i < precipitationScale.Length; i++)
            {
                precipitationScale[i].Content = $"{yScaleNames[i].ToString("F1", culture)} mm/h";
            }
            yScaleNames = drawDataWind.YScaleNames(windScale.Length);
            for (int i = 0; i < windScale.Length; i++)
            {
                windScale[i].Content = $"{yScaleNames[i].ToString("F1", culture)} km/h";
            }
            yScaleNames = drawDataHumidity.YScaleNames(humidityScale.Length);
            for (int i = 0; i < humidityScale.Length; i++)
            {
                humidityScale[i].Content = $"{yScaleNames[i].ToString("F1", culture)} %";
            }

            if (drawDataPrecipitationProbability.GetValueLocal(xScaleNames[0], out double precipitationProbabilityValue0, 0))
            {
                precipitationProbabilityScale[0].Content = precipitationProbabilityValue0.ToString("F0", culture);
            }
            else
            {
                precipitationProbabilityScale[0].Content = "-";
            }
            for (int i = 1; i < precipitationProbabilityScale.Length - 1; i++)
            {
                if (drawDataPrecipitationProbability.GetValueLocal(xScaleNames[i], out double precipitationProbabilityValue, 0))
                {
                    precipitationProbabilityScale[i].Content = $"{precipitationProbabilityValue.ToString("F0", culture)} %";
                }
                else
                {
                    precipitationProbabilityScale[i].Content = "- %";
                }
            }
            if (drawDataPrecipitationProbability.GetValueLocal(xScaleNames[precipitationProbabilityScale.Length - 1], out double precipitationProbabilityValue1, 0))
            {
                precipitationProbabilityScale[precipitationProbabilityScale.Length - 1].Content = precipitationProbabilityValue1.ToString("F0", culture);
            }
            else
            {
                precipitationProbabilityScale[precipitationProbabilityScale.Length - 1].Content = "-";
            }

            for (int i = 0; i < windDirectionScale.Length; i++)
            {
                if (drawDataWindDirection.GetValueLocal(xScaleNames[i], out double windDirectionValue, 4))
                {
                    windDirectionScale[i].Angle = windDirectionValue - 180;
                    ((FrameworkElement)precipitationProbabilityScale[i].Parent).Visibility = Visibility.Visible;
                }
                else
                {
                    ((FrameworkElement)precipitationProbabilityScale[i].Parent).Visibility = Visibility.Hidden;
                }
            }

            UpdateOverview(currentHour);

            dataLoaded = true;
        }

        private void UpdateDrawing()
        {
            if (!dataLoaded)
            {
                return;
            }

            double currentHour = currentTime + offset;
            double currentHourMax = currentHour + 24 * 60;

            drawDataTemperatur.SetDataValueX(currentHour, currentHourMax);
            drawDataPrecipitation.SetDataValueX(currentHour, currentHourMax);
            drawDataPrecipitationProbability.SetDataValueX(currentHour, currentHourMax);
            drawDataWind.SetDataValueX(currentHour, currentHourMax);
            drawDataWindMax.SetDataValueX(currentHour, currentHourMax);
            drawDataWindDirection.SetDataValueX(currentHour, currentHourMax);
            drawDataHumidity.SetDataValueX(currentHour, currentHourMax);

            drawDataTemperatur.Update();
            drawDataPrecipitation.Update();
            drawDataPrecipitationProbability.Update();
            drawDataWind.Update();
            drawDataWindMax.Update();
            drawDataWindDirection.Update();
            drawDataHumidity.Update();

            CultureInfo culture = CultureInfo.InvariantCulture;
            double[] xScaleNames = drawDataTemperatur.XScaleNames(timeScale.Length);
            double lastYear = (new DateTime(currentYear - 1, 1, 1) - new DateTime(currentYear, 1, 1)).TotalMinutes;
            double nextYear = (new DateTime(currentYear + 1, 1, 1) - new DateTime(currentYear, 1, 1)).TotalMinutes;
            for (int i = 0; i < timeScale.Length; i++)
            {
                double time = xScaleNames[i];
                int yearChange = time < 0 ? -1 : (time < nextYear ? 0 : 1);
                time += yearChange * (yearChange == -1 ? lastYear : nextYear);
                DateTime dateTime = new DateTime(currentYear + yearChange, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMinutes(time).AddDays(-1);
                timeScale[i].Content = $"{dateTime.ToLocalTime():HH}";
            }
            for (int i = 1; i < precipitationProbabilityScale.Length - 1; i++)
            {
                if (drawDataPrecipitationProbability.GetValueLocal(xScaleNames[i], out double precipitationProbabilityValue, 0))
                {
                    precipitationProbabilityScale[i].Content = $"{precipitationProbabilityValue.ToString("F0", culture)} %";
                }
                else
                {
                    precipitationProbabilityScale[i].Content = "- %";
                }
            }
            for (int i = 0; i < windDirectionScale.Length; i++)
            {
                if (drawDataWindDirection.GetValueLocal(xScaleNames[i], out double windDirectionValue, 4))
                {
                    windDirectionScale[i].Angle = windDirectionValue - 180;
                    ((FrameworkElement)precipitationProbabilityScale[i].Parent).Visibility = Visibility.Visible;
                }
                else
                {
                    ((FrameworkElement)precipitationProbabilityScale[i].Parent).Visibility = Visibility.Hidden;
                }
            }

            UpdateOverview(currentHour);
        }

        private void UpdateOverview(double currentHour)
        {
            CultureInfo culture = CultureInfo.InvariantCulture;
            DateTime currentDate = new DateTime(DateTime.Now.Year, 1, 1).AddMinutes(currentHour).AddDays(-1);
            string dateParse = "ddd d MMM";
            for (int i = 0; i < 9; i++)
            {
                DateTime date = currentDate.AddDays(i - 4);
                if (date.Month == DateTime.Now.Month && date.Day == DateTime.Now.Day)
                {
                    overviewDate[i].Text = "Today";
                }
                else
                {
                    overviewDate[i].Text = date.ToString(dateParse, culture);
                }
            }

            (float min, float max)[] temperatureMinMax = new (float, float)[9];
            float[] precipitation = new float[9];

            for (int i = 0; i < 9; i++)
            {
                temperatureMinMax[i].min = float.MaxValue;
                temperatureMinMax[i].max = float.MinValue;
                precipitation[i] = 0;
            }

            DateTime dateMin = currentDate.AddDays(-4);
            DateTime dateMax = currentDate.AddDays(4);
            int daysInYear = DateTime.IsLeapYear(dateMin.Year) ? 366 : 356;

            int itemStart = 0;
            for (; itemStart < DWDWeather.Forcast.Length; itemStart++)
            {
                DWDWeather.ItemTimedForcast item = DWDWeather.Forcast[itemStart];
                if(item.Time.DayOfYear >= dateMin.DayOfYear && item.Time.DayOfYear <= dateMax.DayOfYear)
                {
                    break;
                }
            }

            for (int i = itemStart; i < DWDWeather.Forcast.Length; i++)
            {
                DWDWeather.ItemTimedForcast item = DWDWeather.Forcast[i];
                int index = (item.Time.DayOfYear - dateMin.DayOfYear + daysInYear) % daysInYear;
                if (index >= 9)
                {
                    break;
                }
                temperatureMinMax[index].min = Math.Min(temperatureMinMax[index].min, item.Temperature);
                temperatureMinMax[index].max = Math.Max(temperatureMinMax[index].max, item.Temperature);
                precipitation[index] += item.Precipitation;
            }

            for (int i = 0; i < 9; i++)
            {
                if (temperatureMinMax[i].min == float.MaxValue)
                {
                    overviewTemperaturMinMax[i].Text = $"- °|- °";
                    overviewPrecipitation[i].Text = $"- mm";
                }
                else
                {
                    overviewTemperaturMinMax[i].Text = $"{temperatureMinMax[i].min.ToString("F0", culture)} °C | {temperatureMinMax[i].max.ToString("F0", culture)} °C";
                    overviewPrecipitation[i].Text = $"{precipitation[i].ToString("F0", culture)} mm";
                }
            }
        }

        int GetLowerBound(double input, int bound)
        {
            int divided = (int)(input / bound);
            return ((input % bound) > 0 ? divided : divided - 1) * bound;

        }

        int GetUpperBound(double input, int bound)
        {
            int divided = (int)(input / bound);
            return ((input % bound) > 0 ? divided + 1 : divided) * bound;

        }

        void SetCurrent(double position)
        {
            if (!dataLoaded)
            {
                return;
            }

            CurrentIndikatorTP.Visibility = Visibility.Visible;
            CurrentIndikatorTP.X1 = position;
            CurrentIndikatorTP.X2 = position;
            CurrentIndikatorWH.Visibility = Visibility.Visible;
            CurrentIndikatorWH.X1 = position;
            CurrentIndikatorWH.X2 = position;


            CultureInfo culture = CultureInfo.InvariantCulture;
            if (drawDataTemperatur.GetValue(position, out double tempratur))
            {
                CurrentTempratur.Content = $"{tempratur.ToString("F1", culture)} °C";
                double time = drawDataTemperatur.GetPosition(position);

                double lastYear = (new DateTime(DateTime.UtcNow.Year - 1, 1, 1) - new DateTime(DateTime.UtcNow.Year, 1, 1)).TotalMinutes;
                double nextYear = (new DateTime(DateTime.UtcNow.Year + 1, 1, 1) - new DateTime(DateTime.UtcNow.Year, 1, 1)).TotalMinutes;
                int yearChange = time < 0 ? -1 : (time < nextYear ? 0 : 1);
                int currentYear = DateTime.UtcNow.Year + yearChange;
                time += yearChange * (yearChange == -1 ? lastYear : nextYear);
                
                DateTime dateTime = new DateTime(currentYear, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMinutes(time).AddDays(-1);
                CurrentTime.Content = $"{dateTime.ToLocalTime():HH:mm d MMM}";
            }
            else
            {
                CurrentTempratur.Content = "- °C";
                CurrentTime.Content = "-";
            }

            if (drawDataPrecipitation.GetValue(position, out double precipitation))
            {
                CurrentPrecipitation.Content = $"{precipitation.ToString("F1", culture)} mm/h";
            }
            else
            {
                CurrentPrecipitation.Content = "- mm/h";
            }

            if (drawDataWind.GetValue(position, out double wind) && drawDataWindMax.GetValue(position, out double windMax))
            {
                CurrentWind.Content = $"{wind.ToString("F1", culture)}-{windMax.ToString("F1", culture)} km/h";
            }
            else
            {
                CurrentWind.Content = "- km/h";
            }

            if (drawDataHumidity.GetValue(position, out double humidity))
            {
                CurrentHumidity.Content = $"{humidity.ToString("F1", culture)} %";
            }
            else
            {
                CurrentHumidity.Content = "- %";
            }

            if (drawDataPrecipitationProbability.GetValue(position, out double precipitationProbability))
            {

                CurrentPrecipitationProbability.Content = $"{precipitationProbability.ToString("F0", culture)} %";
            }
            else
            {
                CurrentPrecipitationProbability.Content = "- %";
            }

            if (drawDataWindDirection.GetValue(position, out double windDirection))
            {
                WindImageRotation.Angle = windDirection - 180;
                WindImage.Visibility = Visibility.Visible;
            }
            else
            {
                WindImage.Visibility = Visibility.Hidden;
            }
        }
        private void Indikator_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            indicatorEnabled = true;
            Point position = e.GetPosition(InputPanel);
            SetCurrent(position.X);
        }

        private void Indikator_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            indicatorEnabled = false;
            CurrentIndikatorTP.Visibility = Visibility.Hidden;
            CurrentIndikatorWH.Visibility = Visibility.Hidden;
        }

        private void Indikator_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (indicatorEnabled)
            {
                Point position = e.GetPosition(InputPanel);
                SetCurrent(position.X);
            }
        }
        private void InputPanel_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            offset += e.Delta * 0.5;
            UpdateDrawing();
            if (indicatorEnabled)
            {
                SetCurrent(CurrentIndikatorTP.X1);
            }
        }

        private void NextLeft_Click(object sender, RoutedEventArgs e)
        {
            offset -= 720;  // 12 hours
            UpdateDrawing();
            if (indicatorEnabled)
            {
                SetCurrent(CurrentIndikatorTP.X1);
            }
        }

        private void NextRight_Click(object sender, RoutedEventArgs e)
        {
            offset += 720;  // 12 hours
            UpdateDrawing();
            if (indicatorEnabled)
            {
                SetCurrent(CurrentIndikatorTP.X1);
            }
        }
        private void Overview_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            int index = Grid.GetColumn(sender as UIElement);
            offset += 1440 * (index - 4);  // 24 hours * overview index
            Rectangle rectangle = (sender as Grid).Children[0] as Rectangle;
            rectangle.Style = FindResource("Overview") as Style;
            UpdateDrawing();
            if (indicatorEnabled)
            {
                SetCurrent(CurrentIndikatorTP.X1);
            }
        }
        private void Overview_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Rectangle rectangle = (sender as Grid).Children[0] as Rectangle;
            rectangle.Style = FindResource("OverviewSelected") as Style;
        }
        #endregion

        #region Settings Tab

        private void Settings_Loaded(object sender, RoutedEventArgs e)
        {
            FontChooser.SelectedValue = property.Font;
            UpdateIntervall.Text = $"{property.UpdateIntervall} minutes";
            ErrorIntervall.Text = $"{property.ErrorIntervall} seconds";
            HowInit.SelectedIndex = property.HowInit;
            ThemeSelector.SelectedValue = property.SelectedTheme;
        }

        private void RestoreDefault_Click(object sender, RoutedEventArgs e)
        {
            Settings.Write(new Settings.Properties());
            Settings.Read();
            property = Settings.LoadedProperties;

            FontChooser.SelectedValue = property.Font;
            UpdateIntervall.Text = $"{property.UpdateIntervall} minutes";
            ErrorIntervall.Text = $"{property.ErrorIntervall} seconds";
            HowInit.SelectedIndex = property.HowInit;
            ((Themes)ThemeSelector.ItemsSource).LoadCurrentThemes();
            SelectTheme(null);
            AssignTheme();
        }
        private void FontChooser_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string font = (string)FontChooser.SelectedValue;
            if (initMode)
            {
                return;
            }

            if (font == null)
            {
                FontChooser.SelectedValue = property.Font;
                return;
            }

            property.Font = font;
        }

        private void UpdateIntervall_TextChanged(object sender, EventArgs e)
        {
            if (initMode)
            {
                return;
            }

            string secondsToParse = UpdateIntervall.Text.Split(new char[] { ' ' }, 2)[0];
            int seconds = property.UpdateIntervall;
            try
            {
                seconds = Convert.ToInt32(secondsToParse);
            }
            catch (FormatException)
            {
                UpdateIntervall.Text = $"{seconds} minutes";
                return;
            }

            property.UpdateIntervall = seconds;
            UpdateIntervall.Text = $"{seconds} minutes";
        }

        private void UpdateIntervall_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
            {
                UpdateIntervall_TextChanged(sender, e);
            }
        }

        private void ErrorIntervall_TextChanged(object sender, EventArgs e)
        {
            if (initMode)
            {
                return;
            }

            string secondsToParse = ErrorIntervall.Text.Split(new char[] { ' ' }, 2)[0];
            int seconds = property.ErrorIntervall;
            try
            {
                seconds = Convert.ToInt32(secondsToParse);
            }
            catch (FormatException)
            {
                ErrorIntervall.Text = $"{seconds} seconds";
                return;
            }
            property.ErrorIntervall = seconds;
            ErrorIntervall.Text = $"{seconds} seconds";
        }

        private void ErrorIntervall_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ErrorIntervall_TextChanged(sender, e);
            }
        }

        private void WriteSettings(object sender, RoutedEventArgs e)
        {
            Settings.Write(property);
            Settings.Read();
            property = Settings.LoadedProperties;
        }

        private void HowInit_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (initMode || HowInit.SelectedIndex < 0)
            {
                return;
            }
            property.HowInit = HowInit.SelectedIndex;
        }

        private void Longitude_TextChanged(object sender, RoutedEventArgs e)
        {
            if (initMode)
            {
                return;
            }

            double longitude = property.Longitude;
            try
            {
                longitude = double.Parse(Longitude.Text, CultureInfo.InvariantCulture);
            }
            catch (FormatException)
            {
                Longitude.Text = longitude.ToString("F4", CultureInfo.InvariantCulture);
                return;
            }
            property.Longitude = longitude;
            Longitude.Text = longitude.ToString("F4", CultureInfo.InvariantCulture);
        }

        private void Longitude_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Longitude_TextChanged(sender, e);
            }
        }

        private void Latitude_TextChanged(object sender, RoutedEventArgs e)
        {
            if (initMode)
            {
                return;
            }

            double latitude = property.Latitude;
            try
            {
                latitude = double.Parse(Latitude.Text, CultureInfo.InvariantCulture);
            }
            catch (FormatException)
            {
                Latitude.Text = latitude.ToString("F4", CultureInfo.InvariantCulture);
                return;
            }
            property.Latitude = latitude;
            Latitude.Text = latitude.ToString("F4", CultureInfo.InvariantCulture);
        }

        private void Latitude_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Latitude_TextChanged(sender, e);
            }
        }
        private void AssignTheme()
        {
            ColorTemplate theme = property.Themes[property.SelectedTheme];
            Resources["BackgroundColorLight"] = theme.BackgroundLight;
            Resources["BackgroundColor"] = theme.Background;
            Resources["BackgroundColorTransparent50"] = theme.BackgroundTransparent50;
            Resources["BackgroundColorDark"] = theme.BackgroundDark;
            Resources["BackgroundColorDarkLight"] = theme.BackgroundDarkLight;
            Resources["ForegroundColor"] = theme.Foreground;
            Resources["DisabledForegroundColor"] = theme.DisabledForeground;
            Resources["SelectedForegroundColor"] = theme.SelectedForeground;
            NotifyPropertyChanged();
        }

        private bool IsStandardTheme(string name)
        {
            return name == THEME_DARK_NAME || name == THEME_LIGHT_NAME;
        }

        private void ThemeSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string themeName = (string)ThemeSelector.SelectedValue;
            if (themeName == null)
            {
                return;
            }
            Console.WriteLine($"Selection Changed: {themeName}");
            SelectTheme(themeName);
        }

        private void SelectTheme(string name)
        {
            if (name == null)
            {
                name = Settings.IsLightTheme() ? THEME_LIGHT_NAME : THEME_DARK_NAME;
                ThemeSelector.SelectedValue = name;
                return;
            }
            property.SelectedTheme = name;
            bool notStandard = !IsStandardTheme(name);
            ThemeRemove.IsEnabled = notStandard;
            ThemeSelector.IsEditable = notStandard;
            AssignTheme();
        }

        private void ThemeNew_Click(object sender, RoutedEventArgs e)
        {
            ColorTemplate theme = property.Themes[property.SelectedTheme];
            string themeName = $"{property.SelectedTheme} (Copy)";
            int n = 0;
            while (property.Themes.ContainsKey(themeName))
            {
                themeName = $"{property.SelectedTheme} (Copy{++n})";
            }
            property.Themes.Add(themeName, theme.Copy());
            ((Themes)ThemeSelector.ItemsSource).Add(themeName);
            ThemeSelector.SelectedValue = themeName;
        }

        private void ThemeRemove_Click(object sender, RoutedEventArgs e)
        {
            if (!ThemeRemove.IsEnabled)
            {
                return;
            }

            string themeName = property.SelectedTheme;
            property.Themes.Remove(themeName);
            ((Themes)ThemeSelector.ItemsSource).Remove(themeName);
        }

        public Color ColorForeground
        {
            get 
            {
                ColorTemplate theme = property.Themes[property.SelectedTheme];
                return theme.Foreground;
            }
            set
            {
                ColorTemplate theme = property.Themes[property.SelectedTheme];
                if (!IsStandardTheme(property.SelectedTheme))
                {
                    theme.Foreground = value;
                    AssignTheme();
                    NotifyPropertyChanged();
                }
            }
        }

        public Color ColorDisabledForeground
        {
            get
            {
                ColorTemplate theme = property.Themes[property.SelectedTheme];
                return theme.DisabledForeground;
            }
            set
            {
                ColorTemplate theme = property.Themes[property.SelectedTheme];
                if (!IsStandardTheme(property.SelectedTheme))
                {
                    theme.DisabledForeground = value;
                    AssignTheme();
                    NotifyPropertyChanged();
                }
            }
        }

        public Color ColorSelectedForeground
        {
            get
            {
                ColorTemplate theme = property.Themes[property.SelectedTheme];
                return theme.SelectedForeground;
            }
            set
            {
                ColorTemplate theme = property.Themes[property.SelectedTheme];
                if (!IsStandardTheme(property.SelectedTheme))
                {
                    theme.SelectedForeground = value;
                    AssignTheme();
                    NotifyPropertyChanged();
                }
            }
        }
        public Color ColorBackground
        {
            get
            {
                ColorTemplate theme = property.Themes[property.SelectedTheme];
                return theme.Background;
            }
            set
            {
                ColorTemplate theme = property.Themes[property.SelectedTheme];
                if (!IsStandardTheme(property.SelectedTheme))
                {
                    theme.Background = value;
                    AssignTheme();
                    NotifyPropertyChanged();
                }
            }
        }
        public Color ColorBackgroundDark
        {
            get
            {
                ColorTemplate theme = property.Themes[property.SelectedTheme];
                return theme.BackgroundDark;
            }
            set
            {
                ColorTemplate theme = property.Themes[property.SelectedTheme];
                if (!IsStandardTheme(property.SelectedTheme))
                {
                    theme.BackgroundDark = value;
                    AssignTheme();
                    NotifyPropertyChanged();
                }
            }
        }
        public Color ColorBackgroundLight
        {
            get
            {
                ColorTemplate theme = property.Themes[property.SelectedTheme];
                return theme.BackgroundLight;
            }
            set
            {
                ColorTemplate theme = property.Themes[property.SelectedTheme];
                if (!IsStandardTheme(property.SelectedTheme))
                {
                    theme.BackgroundLight = value;
                    AssignTheme();
                    NotifyPropertyChanged();
                }
            }
        }

        private void ThemeSelector_TextChanged(object sender, EventArgs e)
        {
            string oldName = property.SelectedTheme.Trim();
            if (IsStandardTheme(oldName))
            {
                SelectTheme(oldName);
            }
            string newName = ThemeSelector.Text.Trim();
            if (newName == String.Empty || oldName == newName || property.Themes.ContainsKey(newName))
            {
                ThemeSelector.Text = oldName;
                ThemeSelector.SelectedValue = oldName;
                return;
            }
            ColorTemplate theme = property.Themes[oldName].Copy();
            property.Themes.Remove(oldName);
            property.Themes.Add(newName, theme);
            Themes themesCollection = (Themes)ThemeSelector.ItemsSource;
            for (int i = 0; i < themesCollection.Count; i++)
            {
                if (themesCollection[i] == oldName)
                {
                    themesCollection[i] = newName;
                    break;
                }
            }
            ThemeSelector.SelectedValue = newName;
        }

        private void ThemeSelector_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ThemeSelector_TextChanged(sender, e);
                e.Handled = true;
            }
        }
        private void ShowIcon_Click(object sender, RoutedEventArgs e)
        {
            property.ShowIcon = ShowIcon.IsChecked == false ? false: true;
            e.Handled = true;
        }
        private void ShowTemperaturMaxMin_Click(object sender, RoutedEventArgs e)
        {
            property.ShowMaxMinTemperature = ShowTemperaturMaxMin.IsChecked == false ? false : true;
            e.Handled = true;
        }
        private void ShowHumidity_Click(object sender, RoutedEventArgs e)
        {
            property.ShowHumidity = ShowHumidity.IsChecked == false ? false : true;
            e.Handled = true;
        }
        private void ShowPrecipitation_Click(object sender, RoutedEventArgs e)
        {
            property.ShowPrecipitation = ShowPreciptiation.IsChecked == false ? false : true;
            e.Handled = true;
        }
        private void ShowWindSpeed_Click(object sender, RoutedEventArgs e)
        {
            property.ShowWindSpeed = ShowWindSpeed.IsChecked == false ? false : true;
            e.Handled = true;
        }
        #endregion
    }
}

