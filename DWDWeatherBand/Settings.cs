using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Media;
using Microsoft.Win32;
using Newtonsoft.Json;
using static DWDWeatherBand.Settings;

namespace DWDWeatherBand
{
    public class Themes : ObservableCollection<string>
    {
        public Themes() : base() 
        {
            LoadCurrentThemes();
        }

        public void LoadCurrentThemes()
        {
            Clear();
            foreach (string key in LoadedProperties.Themes.Keys)
            {
                Add(key);
            }
        }
    }

    static public class Settings
    {
        public const string THEME_DARK_NAME = "Dark";
        public const string THEME_LIGHT_NAME = "Light";

        public enum HowInit
        {
            Automatic,
            //City,
            GeoLocation
        }

        public class ColorTemplate
        {
            public Color Foreground { get; set; }
            public Color DisabledForeground { get; set; }
            public Color SelectedForeground { get; set; }
            public Color Background { get; set; }
            public Color BackgroundDark { get; set; }
            public Color BackgroundLight { get; set; }
            [JsonIgnore]
            public Color BackgroundDarkLight 
            { 
                get 
                {
                    return new Color()
                    {
                        R = (byte)(Background.R >= 15 ? Background.R - 15 : 0),
                        G = (byte)(Background.R >= 15 ? Background.G - 15 : 0),
                        B = (byte)(Background.R >= 15 ? Background.B - 15 : 0),
                        A = Background.A
                    };
                } 
            }
            [JsonIgnore]
            public Color BackgroundTransparent50 
            {
                get
                {
                    return new Color()
                    {
                        R = Background.R,
                        G = Background.G,
                        B = Background.B,
                        A = 127
                    };
                }
            }

            static public ColorTemplate getDefaultDark()
            {
                return new ColorTemplate()
                {
                    BackgroundLight = Color.FromArgb(255, 76, 76, 76),
                    Background = Color.FromArgb(255, 62, 62, 62),
                    BackgroundDark = Color.FromArgb(255, 33, 33, 33),
                    Foreground = Color.FromArgb(255, 255, 255, 255),
                    DisabledForeground = Color.FromArgb(255, 211, 211, 211),
                    SelectedForeground = Color.FromArgb(255, 124, 223, 255)
                };
            }
            static public ColorTemplate getDefaultLight()
            {
                return new ColorTemplate()
                {
                    BackgroundLight = Color.FromArgb(255, 250, 250, 250),
                    Background = Color.FromArgb(255, 230, 230, 230),
                    BackgroundDark = Color.FromArgb(255, 200, 200, 200),
                    Foreground = Color.FromArgb(255, 0, 0, 0),
                    DisabledForeground = Color.FromArgb(255, 30, 30, 30),
                    SelectedForeground = Color.FromArgb(255, 0, 84, 197)
                };
            }

            public ColorTemplate Copy()
            {
                return new ColorTemplate()
                {
                    Foreground = Foreground,
                    DisabledForeground = DisabledForeground,
                    SelectedForeground = SelectedForeground,
                    Background = Background,
                    BackgroundDark = BackgroundDark,
                    BackgroundLight = BackgroundLight
                };
            }
                
        }

        public const string FileName = "Settings.json";
        public const string AppName = "DWDWeatherBand";
        public static string Path { get { return _path; } }
        private static string _path;
        public class Properties
        {
            public string Font { get; set; } = "Segoe UI";
            public int UpdateIntervall { get; set; } = 10; // minutes
            public int ErrorIntervall { get; set; } = 30; // seconds
            public int HowInit { get; set; } = 0;
            public double Longitude { get; set; } = double.NaN;
            public double Latitude { get; set; } = double.NaN;

            public bool ShowIcon { get; set; } = true;
            public bool ShowMaxMinTemperature { get; set;} = true;
            public bool ShowHumidity { get; set; } = true;
            public bool ShowPrecipitation { get; set; } = true;
            public bool ShowWindSpeed { get; set; } = true;

            public string SelectedTheme { get; set; } = IsLightTheme() ? THEME_LIGHT_NAME : THEME_DARK_NAME;

            public Dictionary<string, ColorTemplate> Themes { get; set; } = new Dictionary<string, ColorTemplate>()
            {
                { THEME_DARK_NAME, ColorTemplate.getDefaultDark() },
                { THEME_LIGHT_NAME, ColorTemplate.getDefaultLight() }
            };
        }
        public static Properties LoadedProperties { 
            get 
            {
                if (_loadedProperties == null) {
                    Read();
                }     
                return _loadedProperties;
            } 
        }
        private static Properties _loadedProperties = null;

        private static string GetDirectory()
        {
            string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string directory = System.IO.Path.Combine(appdata, AppName);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            return directory;
        }
        public static void Read()
        {
            _path = System.IO.Path.Combine(GetDirectory(), FileName);
            if (!File.Exists(_path))
            {
                _loadedProperties = new Properties();
                Write(_loadedProperties);
                return;
            }
            using (StreamReader sr = new StreamReader(_path, Encoding.UTF8))
            using (JsonTextReader jsonIn = new JsonTextReader(sr))
            {
                _loadedProperties = new JsonSerializer().Deserialize<Properties>(jsonIn);
            }
            // Need because file exists but is empty
            if (_loadedProperties == null)
            {
                _loadedProperties = new Properties();
                Write(_loadedProperties);
            }
        }

        public static void Write(Properties properties)
        {
            _path = System.IO.Path.Combine(GetDirectory(), FileName);

            JsonSerializerSettings jsonSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented
            };
            JsonSerializer jsonSerializer = JsonSerializer.CreateDefault(jsonSettings);
            using (StreamWriter sw = new StreamWriter(_path, false, Encoding.UTF8))
            using (JsonWriter jsonOut = new JsonTextWriter(sw))
            {
                jsonSerializer.Serialize(jsonOut, properties);
            }
        }

        public static bool IsLightTheme()
        {
            var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            return value is int i && i > 0;
        }

        // This presumes that weeks start with Monday.
        // Week 1 is the 1st week of the year with a Thursday in it.
        public static int GetIso8601WeekOfYear(DateTime time)
        {
            // Seriously cheat.  If its Monday, Tuesday or Wednesday, then it'll 
            // be the same week# as whatever Thursday, Friday or Saturday are,
            // and we always get those right
            DayOfWeek day = CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(time);
            if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
            {
                time = time.AddDays(3);
            }

            // Return the week of our adjusted day
            return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(time, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }

        public static DateTime Iso8601WeekOfYearMonday(int year, int week)
        {
            DateTime firstDay = new DateTime(year, 1, 1);
            DateTime firstMonday = firstDay.AddDays(-((int)firstDay.DayOfWeek + 6) % 7);
            return firstMonday.AddDays(week * 7 - (firstMonday.AddDays(3).Year == year ? 7 : 0));
        }
    }
}
