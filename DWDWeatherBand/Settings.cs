using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.Globalization;
using System.Reflection;

namespace DWDWeatherBand
{
    static public class Settings
    {
        public enum HowInit
        {
            Automatic,
            //City,
            GeoLocation
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
            public float Longitude { get; set; } = float.NaN;
            public float Latitude { get; set; } = float.NaN;
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
