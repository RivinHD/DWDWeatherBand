using CSDeskBand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using UserControl = System.Windows.Controls.UserControl;
using System.Net.Http;
using System.Windows.Controls.Primitives;
using Microsoft.VisualBasic;
using System.Threading;
using Timer = System.Windows.Forms.Timer;
using System.Windows.Threading;
using System.Globalization;

namespace DWDWeatherBand
{
    /// <summary>
    /// Interaction logic for TaskbarMonitor.xaml
    /// </summary>
    public partial class TaskbarMonitor : UserControl
    {
        CSDeskBandWpf bandWin;
        DWDWeather weather;
        DispatcherTimer updateTimer;
        DispatcherTimer errorTimer;
        TaskbarInfo taskbarInfo;
        public TaskbarMonitor(CSDeskBandWpf w)
        {
            InitializeComponent();
            bandWin = w;
        }
#if DEBUG
        public TaskbarMonitor()
        {
            InitializeComponent();
            bandWin = new Deskband(false);
        }
#endif

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Settings.Properties properties = Settings.LoadedProperties;
            taskbarInfo = new TaskbarInfo();
            FontFamily font = new FontFamily(properties.Font);

            taskbarInfo.TaskbarSizeChanged += new EventHandler<TaskbarSizeChangedEventArgs>(UpdateSize);
            Resources["SelectedFont"] = font;

            BasePanel.Height = taskbarInfo.Size.Height;
            BasePanel.MaxHeight = taskbarInfo.Size.Height;
            BasePanel.MinHeight = taskbarInfo.Size.Height;
            BasePanel.UpdateLayout();
            bandWin.Options.MinHorizontalSize = new DeskBandSize((int)(Math.Ceiling(BasePanel.ActualWidth) + 0.5), taskbarInfo.Size.Height);
            SetDefaulText();

            updateTimer = new DispatcherTimer {
                Interval = TimeSpan.FromMinutes(properties.UpdateIntervall)
            };
#if DEBUG
            updateTimer.Interval = TimeSpan.FromMinutes(1);
#endif
            updateTimer.Tick += new EventHandler(TickUpdate);

            errorTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(properties.ErrorIntervall)
            };
            errorTimer.Tick += new EventHandler(TickUpdate);
            await Init();
        }

        private async Task Init()
        {
            await Task.Run(() =>
            {
                Settings.Properties properties = Settings.LoadedProperties;
                switch (properties.HowInit)
                {
                    case (int)Settings.HowInit.Automatic:
                        weather = new DWDWeather();
                        break;
                    //case (int)Settings.HowInit.City:
                    //    weather = new DWDWeather();
                    //    break;
                    case (int)Settings.HowInit.GeoLocation:
                        weather = new DWDWeather(properties.Latitude, properties.Longitude);
                        break;

                    default:
                        weather = new DWDWeather();
                        break;
                }
            });

            await UpdateText();
        }

        private async void TickUpdate(object sender, EventArgs e)
        {
            await UpdateText();
        }

        private async Task UpdateText()
        {
            updateTimer.Stop();
            errorTimer.Stop();
            bool success = await weather.UpdateNow() && await weather.UpdateHighLow();
#if DEBUG
            System.Windows.Forms.MessageBox.Show(
                $"Update Text: {success}",
                "Information",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
#endif
            if (success)
            {
                SetText(weather.Now, weather.DailyLow.Temperature, weather.DailyHigh.Temperature);
                updateTimer.Start();
            }
            else
            {
                SetDefaulText();
                errorTimer.Start();
            }
        }

        private void SetDefaulText()
        {
            IconBlock.Visibility = Visibility.Collapsed;
            TemperaturText.Text = $"-°C";
            TemperaturMinText.Text = $"-°C";
            TemperaturMaxText.Text = $"-°C";
            HumidityText.Text = $"- %";
            PrecipitationText.Text = $"- mm/h";
            WindText.Text = $"- km/h";
            WindImageRotation.Angle = 0;
            LastUpdated.Content = DateTime.Now.ToString("HH:mm");
            BasePanel.UpdateLayout();
            bandWin.Options.MinHorizontalSize = new DeskBandSize((int)(Math.Ceiling(BasePanel.ActualWidth) + 0.5), (int)(Math.Ceiling(BasePanel.ActualHeight) + 0.5));
            bandWin.Options.HorizontalSize = bandWin.Options.MinHorizontalSize;
        }

        private void SetText(DWDWeather.Item item, float TemperaturLow, float TemperaturHigh)
        {
            if(item.Icon == WeatherIcon.None)
            {
                IconBlock.Visibility = Visibility.Collapsed;
            }
            else
            {
                IconBlock.Visibility = Visibility.Visible;
                IconBlock.Source = new BitmapImage(new Uri($"Resources/wettericons/{Enum.GetName(typeof(WeatherIcon), item.Icon)}.png", UriKind.Relative));
            }
            CultureInfo culture = CultureInfo.InvariantCulture;
            TemperaturText.Text = $"{item.Temperature.ToString("0.0", culture)}°C";
            TemperaturMinText.Text = $"{TemperaturLow.ToString("0.0", culture)}°C";
            TemperaturMaxText.Text = $"{TemperaturHigh.ToString("0.0", culture)}°C";
            HumidityText.Text = $"{item.Humidity.ToString("0.0", culture)} %";
            PrecipitationText.Text = $"{item.Precipitation.ToString("0.0", culture)} mm/h";
            WindText.Text = $"{item.Wind.ToString("0.0", culture)}-{item.MaxWind.ToString("0.0", culture)} km/h";
            WindImageRotation.Angle = item.WindDirection - 180;
            LastUpdated.Content = DateTime.Now.ToString("HH:mm");
            BasePanel.UpdateLayout();
            bandWin.Options.MinHorizontalSize = new DeskBandSize((int)(Math.Ceiling(BasePanel.ActualWidth) + 0.5), (int)(Math.Ceiling(BasePanel.ActualHeight) + 0.5));
            bandWin.Options.HorizontalSize = bandWin.Options.MinHorizontalSize;
        }

        private void UpdateSize(object sender, TaskbarSizeChangedEventArgs e)
        {
            BasePanel.Height = e.Size.Height;
            BasePanel.UpdateLayout();
            bandWin.Options.MinHorizontalSize = new DeskBandSize((int)(Math.Ceiling(BasePanel.ActualWidth) + 0.5), (int)(Math.Ceiling(BasePanel.ActualHeight) + 0.5));
            bandWin.Options.HorizontalSize = bandWin.Options.MinHorizontalSize;
        }

        private void BasePanel_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            ShowInformation.IsOpen = true;
        }

        private void BasePanel_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            ShowInformation.IsOpen = false;
        }
    }
}
