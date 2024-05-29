using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CSDeskBand;
using UserControl = System.Windows.Controls.UserControl;

namespace DWDWeatherBand
{
    /// <summary>
    /// Interaction logic for TaskbarMonitor.xaml
    /// </summary>
    public partial class TaskbarMonitor : UserControl
    {
        CSDeskBandWpf bandWin;
        DispatcherTimer updateTimer;
        DispatcherTimer errorTimer;
        TaskbarInfo taskbarInfo;
        bool isDisposed = false;
        HwndSource source;
        double currentDpi;
        public TaskbarMonitor(CSDeskBandWpf w)
        {
            InitializeComponent();
            bandWin = w;
            updateTimer = new DispatcherTimer();
            errorTimer = new DispatcherTimer();
            Dispatcher.ShutdownStarted += new EventHandler(Dispose);
        }
#if DEBUG
        public TaskbarMonitor()
        {
            InitializeComponent();
            bandWin = new Deskband(false);
            updateTimer = new DispatcherTimer();
            errorTimer = new DispatcherTimer();
        }
#endif

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (isDisposed)
            {
                return;
            }

            // This UserControl is used in the Toolbar therefore needs a HwndSourceHook to adjust to Dpi Changes
            source = PresentationSource.FromVisual(this) as HwndSource;
            if (source != null)
            {
#if DEBUG
                System.Windows.Forms.MessageBox.Show(
                    $"Got the HwndSource",
                    "HwndSource",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
#endif
                source.AddHook(HwndSourceHook);
                currentDpi = GetDpiForWindow(source.Handle);
            }
            Settings.Properties properties = Settings.LoadedProperties;
            taskbarInfo = new TaskbarInfo();
            FontFamily font = new FontFamily(properties.Font);

            Resources["SelectedFont"] = font;

            BasePanel.MinHeight = taskbarInfo.Size.Height;
            BasePanel.MaxHeight = taskbarInfo.Size.Height;
            BasePanel.UpdateLayout();
            bandWin.Options.MinHorizontalSize = new DeskBandSize((int)(Math.Ceiling(BasePanel.ActualWidth) + 0.5), taskbarInfo.Size.Height);
            SetDefaulText();

            updateTimer.Interval = TimeSpan.FromMinutes(properties.UpdateIntervall);
#if DEBUG
            updateTimer.Interval = TimeSpan.FromMinutes(1);
#endif
            updateTimer.Tick += new EventHandler(TickUpdate);

            errorTimer.Interval = TimeSpan.FromSeconds(properties.ErrorIntervall);
            errorTimer.Tick += new EventHandler(TickUpdate);
            await Init();
        }

        // requires win 10 anniversary
        [DllImport("user32")]
        public static extern uint GetDpiForWindow(IntPtr hWnd);
        private IntPtr HwndSourceHook(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
        {
            const int WM_DPICHANGED_AFTERPARENT = 0x02E3;

            switch (msg)
            {
                case WM_DPICHANGED_AFTERPARENT:
                    // Used for the toolbar since we don't receive WM_DPICHANGED messages there.
                    UpdateSize();
                    handled = true;
                    break;
            }

            return IntPtr.Zero;
        }

        private async Task Init()
        {
            if (isDisposed)
            {
                return;
            }
            await Task.Run(() =>
            {
                if (isDisposed)
                {
                    return;
                }
                Settings.Properties properties = Settings.LoadedProperties;
                switch (properties.HowInit)
                {
                    //case (int)Settings.HowInit.City:
                    //    DWDWeather.Init(city);
                    //    break;
                    case (int)Settings.HowInit.GeoLocation:
                        DWDWeather.Init(properties.Latitude, properties.Longitude);
                        break;

                    default:  // also used for Settings.HowInit.Automatic
                        DWDWeather.Init();
                        break;
                }
            });

            CurrentStation.Content = DWDWeather.StationName;

            await UpdateText();
        }

        private async void TickUpdate(object sender, EventArgs e)
        {
            if (isDisposed)
            {
                return;
            }
            try
            {
                await UpdateText();
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(
                    ex.ToString(),
                    "Unhandled exception",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        private async Task UpdateText()
        {
            if (isDisposed)
            {
                return;
            }
            updateTimer.Stop();
            errorTimer.Stop();
            bool success = await DWDWeather.UpdateNow() && await DWDWeather.UpdateHighLow();
#if DEBUG
            System.Windows.Forms.MessageBox.Show(
                $"Update Text: {success}",
                "Information",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
#endif
            if (isDisposed)
            {
                return;
            }
            if (success)
            {
                SetText(DWDWeather.Now, DWDWeather.DailyLow.Temperature, DWDWeather.DailyHigh.Temperature);
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

        private void UpdateSize()
        {
            if (source == null)
            {
                return;
            }
            double newDpi = GetDpiForWindow(source.Handle);
            if (newDpi == currentDpi)
            {
                return;
            }
            double scale = newDpi / currentDpi;
            BasePanel.MinHeight *= scale;
            BasePanel.MaxHeight *= scale;
            BasePanel.UpdateLayout();
            bandWin.Options.MinHorizontalSize = new DeskBandSize((int)(Math.Ceiling(BasePanel.ActualWidth) + 0.5), (int)(Math.Ceiling(BasePanel.ActualHeight) + 0.5));
            bandWin.Options.HorizontalSize = bandWin.Options.MinHorizontalSize;
        }

        private void UpdateWithSettings()
        {
            Settings.Properties properties = Settings.LoadedProperties;
            FontFamily font = new FontFamily(properties.Font);

            Resources["SelectedFont"] = font;

            updateTimer.Interval = TimeSpan.FromMinutes(properties.UpdateIntervall);
#if DEBUG
            updateTimer.Interval = TimeSpan.FromMinutes(1);
#endif

            errorTimer.Interval = TimeSpan.FromSeconds(properties.ErrorIntervall);

            IconBlock.Visibility = properties.ShowIcon ? Visibility.Visible : Visibility.Collapsed;
            TemperaturMaxText.Visibility = properties.ShowMaxMinTemperature ? Visibility.Visible : Visibility.Collapsed;
            TemperaturMinText.Visibility = properties.ShowMaxMinTemperature ? Visibility.Visible : Visibility.Collapsed;
            Humidity.Visibility = properties.ShowHumidity ? Visibility.Visible : Visibility.Collapsed;
            Precipitation.Visibility = properties.ShowPrecipitation ? Visibility.Visible : Visibility.Collapsed;
            Wind.Visibility = properties.ShowWindSpeed ? Visibility.Visible : Visibility.Collapsed;
        }

        private void BasePanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (ShowUpdateTime.IsOpen || !DWDWeather.FinishedInit)
            {
                return;
            }
            ShowInformation.IsOpen = true;
        }

        private void BasePanel_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (ShowUpdateTime.IsOpen || !DWDWeather.FinishedInit)
            {
                return;
            }
            ShowInformation.IsOpen = true;
        }

        private void BasePanel_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (ShowInformation.IsOpen || !DWDWeather.FinishedInit)
            {
                return;
            }
            ShowUpdateTime.IsOpen = true;
        }

        private void BasePanel_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (ShowInformation.IsOpen || !DWDWeather.FinishedInit)
            {
                return;
            }
            ShowUpdateTime.IsOpen = false;
        }

        private void ShowInformation_Closed(object sender, EventArgs e)
        {
            UpdateWithSettings();
        }

        protected void Dispose(object sender, EventArgs e)
        {
            isDisposed = true;
            updateTimer.Stop();
            errorTimer.Stop();
        }
    }
}
