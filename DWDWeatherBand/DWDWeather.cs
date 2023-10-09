using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Markup;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using static DWDWeatherBand.Settings;
using static System.Net.WebRequestMethods;
using System.Device.Location;
using Newtonsoft.Json;
using Microsoft.VisualBasic.FileIO;
using System.Windows.Controls;
using System.IO.Compression;
using System.Xml.Linq;
using System.Security.Cryptography;
using System.ComponentModel.Design;
using System.Security.Policy;
using TimeSpan = System.TimeSpan;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Runtime.InteropServices;
using System.Security;
using System.IO.Pipes;
using CSDeskBand;
using Microsoft.VisualBasic;
using System.Windows.Forms;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using HtmlAgilityPack;

namespace DWDWeatherBand
{
    public enum Resolution
    {
        Minutes10,
        Hourly,
        Daily,
        Monthly
    }

    public enum DataTyp
    {
        Tempratur,
        Precipitation,
        Wind,
        Solar
    }

    public enum WeatherIcon
    {
        None = -1,
        Wolkenlos,
        Bewoelkt_leicht,
        Bewoelkt_schwer,
        Bedeckt,
        Regen_leicht,
        Regen_mittel,
        Regen_schwer,
        Regenschauer_leicht,
        Regenschauer_schwer,
        Schneefall_leicht,
        Schneefall_mittel,
        Schneefall_schwer,
        Schneeschauer_leicht,
        Schneeschauer_schwer,
        Schneeregen_leicht,
        Schneeregen_schwer,
        Schneeregenschauer_leicht,
        Schneeregenschauer_schwer,
        Graupelschauer,
        Hagel_leicht,
        Hagel_schwer,
        Gewitter_leicht,
        Gewitter_mittel,
        Gewitter_schwer,
        Gewitter_Hagel_leicht,
        Gewitter_Hagel_schwer,
        Nebel,
        Nebel_gefrierend,
        Glatteis,
        Regen_gefrierend_leicht,
        Regen_gefrierend_schwer,
        Sturm
    }

    public static class DWDSettings
    {
        // Help Information from https://www.dwd.de/DE/leistungen/opendata/hilfe.html?nn=495490
        public static string[] Resolutions = new string[] { "10_minutes", "hourly", "daily", "monthly" };
        public static string[] DataTypes = new string[] { "air_temperature", "precipitation", "wind", "solar" };
        public static string[] ResolutionsStation = new string[] { "zehn", "Stundenwerte", "Tageswerte", "Monatswerte" };
        public static string[] DataTypesStations = new string[] { "tu", "rr", "ff", "sd"};

        public static Uri HistroyBaseUri = new Uri("https://opendata.dwd.de/climate_environment/CDC/observations_germany/climate/");
        public static Regex HistoryStationRegex = new Regex("(\\S\\d{3,4}) +\\d{8} +\\d{8} +-?\\d{1,4} +(-?\\d{1,2}.\\d{4}) +(-?\\d{1,2}.\\d{4}) +(.*?) {10} +\\S+ +");

        // Mosmix is used for the forcast and to calculate the current temperatur
        // More information on https://www.dwd.de/DE/leistungen/met_verfahren_mosmix/met_verfahren_mosmix.html
        public static Uri MosmixStaionsUri = new Uri("https://www.dwd.de/DE/leistungen/klimadatendeutschland/statliste/statlex_html.html?view=nasPublication&nn=16102");
        public static Regex MosmixStationRegex = new Regex("<tr><td>(.*?)<\\/td><td.*?<\\/td><td.*?MN<\\/td><td .*?>(.*?)<\\/td><td.*?>(.*?)<\\/td><td.*?>(.*?)<\\/td>.*");
        // MosmixS get's updated every hour, but all_stations -> 38 MB zipped & 650 MB unzipped
        public static Uri MosmixSLatestUri = new Uri("https://opendata.dwd.de/weather/local_forecasts/mos/MOSMIX_S/all_stations/kml/MOSMIX_S_LATEST_240.kmz");
        // MosmixL get's updated every 6 hours, but one_station -> 18 KB zipped & 337 KB unzipped
        public static Uri MosmixLUri = new Uri("https://opendata.dwd.de/weather/local_forecasts/mos/MOSMIX_L/single_stations/");
        public static Uri WarnwetterUri = new Uri("https://app-prod-ws.warnwetter.de/v30/"); // Use to get forcast Data, uses Momix station_id
        public static Uri WarnwetterAWSUri = new Uri("https://s3.eu-central-1.amazonaws.com/app-prod-static.warnwetter.de/v16/"); // Use to get current measurements, uses Momix station_id
        // https://s3.eu-central-1.amazonaws.com/app-prod-static.warnwetter.de/v16/current_measurement_M552.json
        // https://s3.eu-central-1.amazonaws.com/app-prod-static.warnwetter.de/v16/forecast_mosmix_M552.json
        public static Uri PoiUri = new Uri("https://opendata.dwd.de/weather/weather_reports/poi/"); // uses Momix station_id
        // https://www.dwd.de/DE/leistungen/opendata/help/schluessel_datenformate/csv/poi_present_weather_zuordnung_pdf.pdf?__blob=publicationFile&v=5

        // Current Information
        // First use WarnwetterAWSUri with currrent_measurement_{stationId}.json
        // complete with PoiUri, then MosmixLUri

        // Forcast Information
        // First use WarnwetterUri with stationOverviewExtended?stationIds={stationId}
        // complete with MosmixLUri

        // History Information use HistoryBaseUri


        public static Uri GeoLocationAPI = new Uri("http://www.geoplugin.net/json.gp");


    }

    public static class JsonCasts
    {
        public class GeoItem
        {
            public string geoplugin_latitude { get; set; }
            public string geoplugin_longitude { get; set; }
        }

        public class CurrentMeasurement
        {
            public int icon { get; set; }
            public float temperature { get; set; }
            public float humidity { get; set; }
            public float precipitation { get; set; }
            public float winddirection { get; set; }
            public float meanwind { get; set; }
            public float maxwind { get; set; }
        }
    }

    public class DWDWeather
    {
        private class CustomeWebClient : WebClient
        {
            public int Timeout = 500;
            protected override WebRequest GetWebRequest(Uri uri)
            {
                WebRequest w = base.GetWebRequest(uri);
                w.Timeout = Timeout;
                return w;
            }
        }
        #region Properties
        public class Item
        {
            public WeatherIcon Icon = WeatherIcon.None;
            public float Temperature = float.NaN;
            public float Humidity = float.NaN;
            public float Precipitation = float.NaN;
            public float WindDirection = float.NaN;
            public float Wind = float.NaN;
            public float MaxWind = float.NaN;
        }

        public class ItemExtended : Item
        {
            public float Pressure = float.NaN;
            public float Dewpoint = float.NaN;
            public float Sunshine = float.NaN;
            public float CloudCover = float.NaN;
        }

        public class ItemTimed : Item
        {
            public DateTime Time;
        }

        private string name;
        private string stationIDMosmix;
        private string[,] stationIDHistory = new string[DWDSettings.Resolutions.Length, DWDSettings.DataTypes.Length];
        private bool geoLocationSet = false;
        private float latitude;
        private float longitude;

        private KeyValuePair<DateTime, byte[]> cacheMosmix;
        private KeyValuePair<DateTime, byte[]> cachePoi;
        private KeyValuePair<DateTime, JsonCasts.CurrentMeasurement> cacheAwsCurrent;
        private KeyValuePair<DateTime, byte[]> cacheAwsStation;
        private KeyValuePair<DateTime, byte[]>[,] cacheHistory = new KeyValuePair<DateTime, byte[]>[DWDSettings.Resolutions.Length, DWDSettings.DataTypes.Length];

        private Item _now;
        private Item _dailyHigh;
        private Item _dailyLow;

        public Item Now { get { return _now; } }
        public Item DailyHigh { get { return _dailyHigh; } }
        public Item DailyLow { get { return _dailyLow; } }
        #endregion

        #region Constructors
        public DWDWeather()
        {
            Init();
#if DEBUG
            System.Windows.Forms.MessageBox.Show(
                $"{stationIDMosmix} {name}",
                "StationIDMosmix",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
#endif
        }

        //public DWDWeather(string City)
        //{
        //}

        public DWDWeather(float Latitude, float Longitude)
        {
            latitude = Latitude;
            longitude = Longitude;
            geoLocationSet = true;
            Init();
        }
        #endregion

        #region private

        #region Mosmix
        private string GetMosmixStationFromGeoPosition(out string Name)
        {
            Name = default; 
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                   | SecurityProtocolType.Tls11
                   | SecurityProtocolType.Tls12
                   | SecurityProtocolType.Ssl3;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(DWDSettings.MosmixStaionsUri);
            request.Method = "GET";
            request.Timeout = 1000;
            Stack<KeyValuePair<string, string>> matching = new Stack<KeyValuePair<string, string>>();
            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        return null;
                    }
                    StreamReader reader = new StreamReader(stream, Encoding.UTF8);
                    string line;
                    float latitudeDelta = float.MaxValue;
                    float longitudeDelta = float.MaxValue;
                    while ((line = reader.ReadLine()) != null)
                    {
                        //Groups 1=Name  2=StaionID  3=Latitude  4=Longitude
                        Match parsed = DWDSettings.MosmixStationRegex.Match(line);
                        if (!parsed.Success)
                        {
                            continue;
                        }
                        float lat = float.Parse(parsed.Groups[3].Value, CultureInfo.InvariantCulture);
                        float lon = float.Parse(parsed.Groups[4].Value, CultureInfo.InvariantCulture);
                        float deltaLat = (latitude - lat) * (latitude - lat);
                        float deltaLon = (longitude - lon) * (longitude - lon);
                        if (deltaLat + deltaLon < latitudeDelta + longitudeDelta)
                        {
                            matching.Push(new KeyValuePair<string, string>(parsed.Groups[1].Value, parsed.Groups[2].Value));
                            latitudeDelta = deltaLat;
                            longitudeDelta = deltaLon;
                        }
                    }
                }
            }
            catch (WebException ex)
            {
#if DEBUG
                System.Windows.Forms.MessageBox.Show(
                    $"{ex.Message}\n{ex.StackTrace}",
                    "Unhandled WebException",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
#endif
                return null;
            }

            while (matching.Count > 0)
            {
                KeyValuePair<string, string> item = matching.Pop();
                if (UriIsOK(new Uri(DWDSettings.PoiUri, GetPoiName(item.Value))) && UriIsOK(new Uri(DWDSettings.MosmixLUri, item.Value)))
                {
#if DEBUG
                    Console.WriteLine("Finished Get Station ID");
#endif
                    Name = item.Key;
                    return item.Value;
                }
            }
#if DEBUG
            System.Windows.Forms.MessageBox.Show(
                $"URL not OK {latitude}, {longitude}",
                "URL not OK",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
#endif
            return null;
        }

        private bool CacheMosmix(Uri uri)
        {
            KeyValuePair<DateTime, byte[]> cache = CacheData(uri, cacheMosmix, new TimeSpan(1, 0, 0));
            if (cache.Equals(default(KeyValuePair<DateTime, byte[]>)))
            {
                return false;
            }
            cacheMosmix = cache;
            return true;
        }

        private WeatherIcon GetMosmixIcon(int weather, float cloudCover)
        {
            WeatherIcon icon = WeatherIcon.None;
            switch (cloudCover)
            {
                case float v when v < 30:
                    icon = WeatherIcon.Wolkenlos;
                    break;
                case float v when v < 70:
                    icon = WeatherIcon.Bewoelkt_leicht;
                    break;
                case float v when v < 95:
                    icon = WeatherIcon.Bewoelkt_schwer;
                    break;
                case float v when v >= 95:
                    icon = WeatherIcon.Bedeckt;
                    break;
                default:
                    break;
            }
            switch (weather)
            {
                case 95: // Gewitter
                    //Fehlt: Gewitter_Hagel_leicht, Gewitter_Hagel_schwer, Gewitter_leicht, Gewitter_mittel, Gewitter_schwer
                    icon = WeatherIcon.Gewitter_mittel;
                    break;
                case 57: icon = WeatherIcon.Regen_gefrierend_schwer; break;
                case 56: icon = WeatherIcon.Regen_gefrierend_leicht; break;
                case 67: icon = WeatherIcon.Regen_gefrierend_schwer; break;
                case 66: icon = WeatherIcon.Regen_gefrierend_leicht; break;
                case 86: icon = WeatherIcon.Schneeschauer_schwer; break;
                case 85: icon = WeatherIcon.Schneeschauer_leicht; break;
                case 84: icon = WeatherIcon.Schneeregenschauer_schwer; break;
                case 83: icon = WeatherIcon.Schneeregenschauer_leicht; break;
                case 82: icon = WeatherIcon.Regenschauer_schwer; break;
                case 81: case 80: icon = WeatherIcon.Regenschauer_leicht; break;
                case 75: icon = WeatherIcon.Schneefall_schwer; break;
                case 73: icon = WeatherIcon.Schneefall_mittel; break;
                case 71: icon = WeatherIcon.Schneefall_leicht; break;
                case 69: icon = WeatherIcon.Schneeregen_schwer; break;
                case 68: icon = WeatherIcon.Schneeregen_leicht; break;
                case 55: case 65: icon = WeatherIcon.Regen_schwer; break;
                case 53: case 63: icon = WeatherIcon.Regen_mittel; break;
                case 51: case 61: icon = WeatherIcon.Regen_leicht; break;
                case 49: icon = WeatherIcon.Nebel_gefrierend; break;
                case 45: icon = WeatherIcon.Nebel; break;
                default:
                    break;
            }
            // Fehlt Grauperlschauer, Hagel_leicht, Hagel_schwer, Glatteis
            // Hagel ist schwer zu Vorhersagen: https://www.dwd.de/DE/klimaumwelt/klimaforschung/spez_themen/hagel/hagel_node.html
            return icon;
        }

        private Item GetMosmixDataNow()
        {
            bool success = CacheMosmix(new Uri(DWDSettings.MosmixLUri, $"{stationIDMosmix}/kml/MOSMIX_L_LATEST_{stationIDMosmix}.kmz"));
            if (!success)
            {
                return null;
            }

            XDocument doc;
            using (MemoryStream response = new MemoryStream(cacheMosmix.Value))
            using (ZipArchive archive = new ZipArchive(response, ZipArchiveMode.Read))
            {
                if (archive.Entries.Count <= 0)
                {
                    return null;
                }
                using (StreamReader stream = new StreamReader(archive.Entries[0].Open()))
                {
                    doc = XDocument.Load(stream);
                }
            }

            XNamespace kml = doc.Root.GetNamespaceOfPrefix("kml");
            XNamespace dwd = doc.Root.GetNamespaceOfPrefix("dwd");
            XElement document = doc.Root.Element(kml + "Document");
            XElement defintion = document.Element(kml + "ExtendedData").Element(dwd + "ProductDefinition");
            string defaultSign = defintion.Element(dwd + "FormatCfg").Element(dwd + "DefaultUndefSign").Value;
            IEnumerable<XElement> timeStemps = defintion.Element(dwd + "ForecastTimeSteps").Elements(dwd + "TimeStep");
            DateTime now = DateTime.Now;
            int selectedColumn = 0;
            foreach (XElement element in timeStemps)
            {
                DateTime time = DateTime.Parse(element.Value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
                if (time.Hour == now.Hour && time.Year == now.Year && time.DayOfYear == now.DayOfYear)
                {
                    break;
                }
                selectedColumn++;
            }
            if (selectedColumn >= timeStemps.Count())
            {
                return null;
            }
            float temperature = float.NaN;
            float dewpoint = float.NaN;
            float windDirection = float.NaN;
            float wind = float.NaN;
            float windMax = float.NaN;
            float precipitation = float.NaN;
            float cloudCover = float.NaN;
            float weather = float.NaN;
            IEnumerable<XElement> data = document.Element(kml + "Placemark").Element(kml + "ExtendedData").Elements(dwd + "Forecast");
            foreach (XElement element in data)
            {
                // see https://opendata.dwd.de/weather/lib/MetElementDefinition.xml
                string[] values;
                string value;
                string second;
                float start;
                float delta;
                switch (element.Attribute(dwd + "elementName").Value)
                {
                    case "TTT": // Temperature 2m above surface
                        values = element.Value.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        value = values[selectedColumn];
                        second = values[selectedColumn + 1];
                        if (value != defaultSign && second != defaultSign)
                        {
                            start = float.Parse(value, CultureInfo.InvariantCulture);
                            delta = float.Parse(second, CultureInfo.InvariantCulture) - start;
                            temperature = delta * now.Minute / 60 + start - 273.15f;
                        }
                        break;
                    case "Td": // Dewpoint 2m above surface
                        values = element.Value.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        value = values[selectedColumn];
                        second = values[selectedColumn + 1];
                        if (value != defaultSign)
                        {
                            start = float.Parse(value, CultureInfo.InvariantCulture);
                            delta = float.Parse(second, CultureInfo.InvariantCulture) - start;
                            dewpoint = delta * now.Minute / 60 + start - 273.15f;
                        }
                        break;
                    case "DD": // Wind direction
                        values = element.Value.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        value = values[selectedColumn];
                        second = values[selectedColumn + 1];
                        if (value != defaultSign && second != defaultSign)
                        {
                            start = float.Parse(value, CultureInfo.InvariantCulture);
                            delta = float.Parse(second, CultureInfo.InvariantCulture) - start;
                            windDirection = delta * now.Minute / 60 + start;
                        }
                        break;
                    case "FF": // Wind speed
                        values = element.Value.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        value = values[selectedColumn];
                        second = values[selectedColumn + 1];
                        if (value != defaultSign && second != defaultSign)
                        {
                            start = float.Parse(value, CultureInfo.InvariantCulture);
                            delta = float.Parse(second, CultureInfo.InvariantCulture) - start;
                            wind = (delta * now.Minute / 60 + start) * 3.6f;
                        }
                        break;
                    case "FX1": // Maximum wind gust within the last hour
                        values = element.Value.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        value = values[selectedColumn];
                        second = values[selectedColumn + 1];
                        if (value != defaultSign && second != defaultSign)
                        {
                            start = float.Parse(value, CultureInfo.InvariantCulture);
                            delta = float.Parse(second, CultureInfo.InvariantCulture) - start;
                            windMax = (delta * now.Minute / 60 + start) * 3.6f;
                        }
                        break;
                    case "RR1c": // Total precipitation during the last hour consistent with significant weather
                        values = element.Value.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        value = values[selectedColumn];
                        second = values[selectedColumn + 1];
                        if (value != defaultSign && second != defaultSign)
                        {
                            start = float.Parse(value, CultureInfo.InvariantCulture);
                            delta = float.Parse(second, CultureInfo.InvariantCulture) - start;
                            precipitation = delta * now.Minute / 60 + start;
                        }
                        break;
                    case "N": // Total cloud cover
                        values = element.Value.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        value = values[selectedColumn];
                        second = values[selectedColumn + 1];
                        if (value != defaultSign && second != defaultSign)
                        {
                            start = float.Parse(value, CultureInfo.InvariantCulture);
                            delta = float.Parse(second, CultureInfo.InvariantCulture) - start;
                            cloudCover = delta * now.Minute / 60 + start;
                        }
                        break;
                    case "ww": // Significant Weather
                        value = element.Value.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[selectedColumn];
                        if (value != defaultSign)
                        {
                            weather = float.Parse(value, CultureInfo.InvariantCulture);
                        }
                        break;

                    default: break;
                }
            }

            return new Item
            {
                Temperature = temperature,
                Humidity = (float)CalculateHumidity(dewpoint, temperature),
                Wind = wind,
                WindDirection = windDirection,
                MaxWind = windMax,
                Precipitation = precipitation,
                Icon = GetMosmixIcon((int)weather, cloudCover)
            };
        }

        private ItemTimed[] GetMosmixData()
        {
            bool success = CacheMosmix(new Uri(DWDSettings.MosmixLUri, $"{stationIDMosmix}/kml/MOSMIX_L_LATEST_{stationIDMosmix}.kmz"));
            if (!success)
            {
                return null;
            }

            XDocument doc;
            using (MemoryStream response = new MemoryStream(cacheMosmix.Value))
            using (ZipArchive archive = new ZipArchive(response, ZipArchiveMode.Read))
            {
                if (archive.Entries.Count <= 0)
                {
                    return null;
                }
                using (StreamReader stream = new StreamReader(archive.Entries[0].Open()))
                {
                    doc = XDocument.Load(stream);
                }
            }
            XNamespace kml = doc.Root.GetNamespaceOfPrefix("kml");
            XNamespace dwd = doc.Root.GetNamespaceOfPrefix("dwd");
            XElement document = doc.Root.Element(kml + "Document");
            XElement defintion = document.Element(kml + "ExtendedData").Element(dwd + "ProductDefinition");
            string defaultSign = defintion.Element(dwd + "FormatCfg").Element(dwd + "DefaultUndefSign").Value;
            IEnumerable<XElement> timeStemps = defintion.Element(dwd + "ForecastTimeSteps").Elements(dwd + "TimeStep");
            IEnumerable<XElement> data = document.Element(kml + "Placemark").Element(kml + "ExtendedData").Elements(dwd + "Forecast");

            int dataLength = timeStemps.Count();
            ItemTimed[] items = new ItemTimed[dataLength];
            int index = 0;
            foreach (XElement element in timeStemps)
            {
                items[index] = new ItemTimed() { Time=DateTime.Parse(element.Value) };
                index++;
            }
            if (index >= timeStemps.Count())
            {
                return null;
            }
            float[] dewpoints = new float[dataLength];
            float[] cloudCovers = new float[dataLength];
            float[] weathers = new float[dataLength];

            foreach (XElement element in data)
            {
                // see https://opendata.dwd.de/weather/lib/MetElementDefinition.xml
                string[] values;
                switch (element.Attribute(dwd + "elementName").Value)
                {
                    case "TTT": // Temperature 2m above surface
                        values = element.Value.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 0; i < dataLength; i++)
                        {
                            if (values[i] == defaultSign)
                            {
                                continue;
                            }
                            items[i].Temperature = float.Parse(values[i], CultureInfo.InvariantCulture) - 273.15f;
                        }
                        break;
                    case "Td": // Dewpoint 2m above surface
                        values = element.Value.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 0; i < dataLength; i++)
                        {
                            if (values[i] == defaultSign)
                            {
                                continue;
                            }
                            dewpoints[i] = float.Parse(values[i], CultureInfo.InvariantCulture) - 273.15f;
                        }
                        break;
                    case "DD": // Wind direction
                        values = element.Value.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 0; i < dataLength; i++)
                        {
                            if (values[i] == defaultSign)
                            {
                                continue;
                            }
                            items[i].WindDirection = float.Parse(values[i], CultureInfo.InvariantCulture);
                        }
                        break;
                    case "FF": // Wind speed
                        values = element.Value.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 0; i < dataLength; i++)
                        {
                            if (values[i] == defaultSign)
                            {
                                continue;
                            }
                            items[i].Wind = float.Parse(values[i], CultureInfo.InvariantCulture) * 3.6f;
                        }
                        break;
                    case "FX1": // Maximum wind gust within the last hour
                        values = element.Value.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 0; i < dataLength; i++)
                        {
                            if (values[i] == defaultSign)
                            {
                                continue;
                            }
                            items[i].MaxWind = float.Parse(values[i], CultureInfo.InvariantCulture) * 3.6f;
                        }
                        break;
                    case "RR1c": // Total precipitation during the last hour consistent with significant weather
                        values = element.Value.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 0; i < dataLength; i++)
                        {
                            if (values[i] == defaultSign)
                            {
                                continue;
                            }
                            items[i].Precipitation = float.Parse(values[i], CultureInfo.InvariantCulture);
                        }
                        break;
                    case "N": // Total cloud cover
                        values = element.Value.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 0; i < dataLength; i++)
                        {
                            if (values[i] == defaultSign)
                            {
                                continue;
                            }
                            cloudCovers[i] = float.Parse(values[i], CultureInfo.InvariantCulture);
                        }
                        break;
                    case "ww": // Significant Weather
                        values = element.Value.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 0; i < dataLength; i++)
                        {
                            if (values[i] == defaultSign)
                            {
                                continue;
                            }
                            weathers[i] = float.Parse(values[i], CultureInfo.InvariantCulture);
                        }
                        break;

                    default: break;
                }
            }

            for (int i = 0; i < dataLength; i++)
            {
                items[i].Humidity = (float)CalculateHumidity(dewpoints[i], items[i].Temperature);
                items[i].Icon = GetMosmixIcon((int)weathers[i], cloudCovers[i]);
            }

            return items;
        }
        #endregion

        #region Poi
        private string GetPoiName(string stationID)
        {
            while (stationID.Length < 5)
            {
                stationID += "_";
            }
            return $"{stationID}-BEOB.csv";
        }

        private bool CachePoi(Uri uri)
        {
            KeyValuePair<DateTime, byte[]> cache = CacheData(uri, cachePoi, new TimeSpan(0, 15, 0));
            if (cache.Equals(default(KeyValuePair<DateTime, byte[]>)))
            {
                return false;
            }
            cachePoi = cache;
            return true;
        }
        private ItemTimed[] GetPoisCSV()
        {
            bool success = CachePoi(new Uri(DWDSettings.PoiUri, GetPoiName(stationIDMosmix)));
            if (!success)
            {
                return null;
            }

            List<ItemTimed> values = new List<ItemTimed>();

            using (MemoryStream response = new MemoryStream(cachePoi.Value))
            using (TextFieldParser parser = new TextFieldParser(response, Encoding.UTF8))
            {
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(";");


                if (parser.EndOfData)
                {
                    return null;
                }
                string[] fields = parser.ReadFields();

                int IndexTemperatur = 0;
                int IndexWindDirection = 0;
                int IndexWind = 0;
                int IndexWindMax = 0;
                int IndexIcon = 0;
                int IndexHumidity = 0;
                int IndexPrecipitation = 0;
                for (int i = 2; i < fields.Length; i++)
                {
                    switch (fields[i])
                    {
                        case "dry_bulb_temperature_at_2_meter_above_ground":
                            IndexTemperatur = i;
                            break;
                        case "mean_wind_direction_during_last_10 min_at_10_meters_above_ground":
                            IndexWindDirection = i;
                            break;
                        case "maximum_wind_speed_as_10_minutes_mean_during_last_hour":
                            IndexWindMax = i;
                            break;
                        case "mean_wind_speed_during last_10_min_at_10_meters_above_ground":
                            IndexWind = i;
                            break;
                        case "present_weather":
                            IndexIcon = i;
                            break;
                        case "relative_humidity":
                            IndexHumidity = i;
                            break;
                        case "precipitation_amount_last_hour":
                            IndexPrecipitation = i;
                            break;
                        default:
                            break;
                    }
                }

                CultureInfo culture = CultureInfo.GetCultureInfo("de-DE");
                while (!parser.EndOfData)
                {
                    fields = parser.ReadFields();
                    ItemTimed item = new ItemTimed();
                    bool succes = DateTime.TryParseExact($"{fields[0]} {fields[1]}", "dd.MM.yy HH:mm", culture, DateTimeStyles.AssumeUniversal, out item.Time);
                    if (!succes)
                    {
                        continue;
                    }
                    if (!float.TryParse(fields[IndexTemperatur], NumberStyles.Number, culture, out item.Temperature))
                    {
                        item.Temperature = float.NaN;
                    }
                    if (!float.TryParse(fields[IndexHumidity], NumberStyles.Number, culture, out item.Humidity))
                    {
                        item.Humidity = float.NaN;
                    }
                    if (!int.TryParse(fields[IndexIcon], NumberStyles.Number, culture, out int icon))
                    {
                        item.Icon = WeatherIcon.None;
                    }
                    else
                    {
                        item.Icon = ParsePoiIcon(icon);
                    }
                    if (!float.TryParse(fields[IndexWind], NumberStyles.Number, culture, out item.Wind))
                    {
                        item.Wind = float.NaN;
                    }
                    if (!float.TryParse(fields[IndexWindMax], NumberStyles.Number, culture, out item.MaxWind))
                    {
                        item.MaxWind = float.NaN;
                    }
                    if (!float.TryParse(fields[IndexWindDirection], NumberStyles.Number, culture, out item.WindDirection))
                    {
                        item.WindDirection = float.NaN;
                    }
                    if (!float.TryParse(fields[IndexPrecipitation], NumberStyles.Number, culture, out item.Precipitation))
                    {
                        item.Precipitation = float.NaN;
                    }
                    values.Add(item);
                }
            }

            return values.ToArray();
        }

        private WeatherIcon ParsePoiIcon(int icon)
        {
            switch (icon)
            {
                case 1: return WeatherIcon.Wolkenlos;
                case 2: return WeatherIcon.Bewoelkt_leicht;
                case 3: return WeatherIcon.Bewoelkt_schwer;
                case 4: return WeatherIcon.Bedeckt;
                case 5: return WeatherIcon.Nebel;
                case 6: return WeatherIcon.Nebel_gefrierend;
                case 7: return WeatherIcon.Regen_leicht;
                case 8: return WeatherIcon.Regen_mittel;
                case 9: return WeatherIcon.Regen_schwer;
                case 10: return WeatherIcon.Regen_gefrierend_leicht;
                case 11: return WeatherIcon.Regen_gefrierend_schwer;
                case 12: return WeatherIcon.Schneeregen_leicht;
                case 13: return WeatherIcon.Schneeregen_schwer;
                case 14: return WeatherIcon.Schneefall_leicht;
                case 15: return WeatherIcon.Schneefall_mittel;
                case 16: return WeatherIcon.Schneefall_schwer;
                case 17: return WeatherIcon.Hagel_leicht;
                case 18: return WeatherIcon.Regenschauer_leicht;
                case 19: return WeatherIcon.Regenschauer_schwer;
                case 20: return WeatherIcon.Schneeregenschauer_leicht;
                case 21: return WeatherIcon.Schneeregenschauer_schwer;
                case 22: return WeatherIcon.Schneeschauer_leicht;
                case 23: return WeatherIcon.Schneeschauer_schwer;
                case 24: return WeatherIcon.Graupelschauer;
                case 25: return WeatherIcon.Hagel_schwer;
                case 26: return WeatherIcon.Gewitter_leicht;
                case 27: return WeatherIcon.Gewitter_mittel;
                case 28: return WeatherIcon.Gewitter_schwer;
                case 29: return WeatherIcon.Gewitter_Hagel_leicht;
                case 30: return WeatherIcon.Gewitter_Hagel_schwer;
                case 31: return WeatherIcon.Sturm;
                default: return WeatherIcon.None;
            }
        }
        #endregion

        #region History
        private Uri CombineHistoryUri(Resolution Resolution, DataTyp DataTyp)
        {
            if (Resolution == Resolution.Minutes10 || Resolution == Resolution.Hourly)
            {
                return new Uri(DWDSettings.HistroyBaseUri, $"{DWDSettings.Resolutions[(int)Resolution]}/{DWDSettings.DataTypes[(int)DataTyp]}");
            }
            return new Uri(DWDSettings.HistroyBaseUri, $"{DWDSettings.Resolutions[(int)Resolution]}/kl");
        }

        private string GetHistoryStationFromGeoPosition(Resolution Resolution, DataTyp DataTyp, out string Name)
        {
            Name = null;
            string filepath = "{0}_Beschreibung_Stationen.txt";
            switch (Resolution)
            {
                case Resolution.Minutes10:
                    filepath = string.Format(filepath, $"{DWDSettings.DataTypes[(int)DataTyp]}/now/{DWDSettings.ResolutionsStation[(int)Resolution]}_now_{DWDSettings.DataTypesStations[(int)DataTyp]}");
                    break;
                case Resolution.Hourly:
                    filepath = string.Format(filepath, $"{DWDSettings.DataTypes[(int)DataTyp]}/recent/{DWDSettings.DataTypesStations[(int)DataTyp].ToUpper()}_{DWDSettings.ResolutionsStation[(int)Resolution]}");
                    break;
                case Resolution.Daily:
                case Resolution.Monthly:
                    filepath = string.Format(filepath, $"recent/KL_{DWDSettings.ResolutionsStation[(int)Resolution]}");
                    break;
            }
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(CombineHistoryUri(Resolution, DataTyp), filepath));
            request.Method = "GET";
            request.Timeout = 500;
            try
            {
                string stationID = null;
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        return null;
                    }
                    StreamReader reader = new StreamReader(stream, Encoding.UTF8);
                    reader.ReadLine(); // Skip first 2 lines
                    reader.ReadLine(); // because these are table header

                    string line;
                    float latitudeDelta = float.MaxValue;
                    float longitudeDelta = float.MaxValue;
                    while ((line = reader.ReadLine()) != null)
                    {
                        //Groups 1=StationId  2=Latitude  3=Longitude  4=Name  
                        Match parsed = DWDSettings.HistoryStationRegex.Match(line);
                        float lat = float.Parse(parsed.Groups[2].Value, CultureInfo.InvariantCulture);
                        float lon = float.Parse(parsed.Groups[3].Value, CultureInfo.InvariantCulture);
                        float deltaLat = (latitude - lat) * (latitude - lat);
                        float deltaLon = (longitude - lon) * (longitude - lon);
                        if (deltaLat + deltaLon < latitudeDelta + longitudeDelta)
                        {
                            stationID = parsed.Groups[1].Value;
                            Name = parsed.Groups[4].Value;
                            latitudeDelta = deltaLat;
                            longitudeDelta = deltaLon;
                        }
                    }
                }
                return stationID;
            }
            catch (WebException)
            {
                return null;
            }
        }
        #endregion

        #region AWS
        private JsonCasts.CurrentMeasurement CachAwsCurrent()
        {
            if (cacheAwsCurrent.Value != default && cacheAwsCurrent.Key != default && cacheAwsCurrent.Key > DateTime.Now)
            {
                return cacheAwsCurrent.Value;
            }
            JsonCasts.CurrentMeasurement item = GetJson<JsonCasts.CurrentMeasurement>(new Uri(DWDSettings.WarnwetterAWSUri, $"current_measurement_{stationIDMosmix}.json"));
            cacheAwsCurrent = new KeyValuePair<DateTime, JsonCasts.CurrentMeasurement>(DateTime.Now.AddMinutes(10), item);
            return cacheAwsCurrent.Value;
        }
        #endregion

        #region General
        private bool SetGeoFromIP()
        {
            JsonCasts.GeoItem item = GetJson<JsonCasts.GeoItem>(DWDSettings.GeoLocationAPI);
            if (item == default(JsonCasts.GeoItem))
            {
                return false;
            }
            latitude = float.Parse(item.geoplugin_latitude, CultureInfo.InvariantCulture);
            longitude = float.Parse(item.geoplugin_longitude, CultureInfo.InvariantCulture);
            return true;
        }
        private T GetJson<T>(Uri uri)
        {
            HttpWebRequest request = WebRequest.CreateHttp(uri);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.Method = "GET";
            request.Timeout = 500;
            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (StreamReader sr = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                using (JsonTextReader jsonIn = new JsonTextReader(sr))
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        return default;
                    }

                    T data = new JsonSerializer().Deserialize<T>(jsonIn);
                    return data;
                }
            }
            catch (WebException ex)
            {
#if DEBUG
                System.Windows.Forms.MessageBox.Show(
                    $"{ex.Message}\n{ex.StackTrace}",
                    "Unhandled WebException",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
#endif
                return default;
            }
        }
        private bool UriIsOK(Uri uri)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.Method = "HEAD";
            request.Timeout = 500;
            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    return response.StatusCode == HttpStatusCode.OK;
                }
            }
            catch (WebException)
            {
                return false;
            }
        }

        private KeyValuePair<DateTime, byte[]> CacheData(Uri uri, KeyValuePair<DateTime, byte[]> current, TimeSpan expireTime)
        {
            if (current.Key != default && current.Key > DateTime.Now)
            {
                return current;
            }

            try
            {
                using (CustomeWebClient client = new CustomeWebClient{Timeout = 500})
                {
                    return new KeyValuePair<DateTime, byte[]>(DateTime.Now + expireTime, client.DownloadData(uri));
                }
            }
            catch (WebException ex)
            {
#if DEBUG
                System.Windows.Forms.MessageBox.Show(
                    $"{ex.Message}\n{ex.StackTrace}",
                    "Unhandled WebException",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
#endif
                return default;
            }
        }

        private double CalculateHumidity(double dewpoint, double temperature)
        {
            // Humidity based on https://www.dwd.de/DE/leistungen/met_verfahren_mosmix/faq/relative_feuchte.html
            double rh_c2 = 17.5043;
            double rh_c3 = 241.2;
            return 100 * Math.Exp((rh_c2 * dewpoint / (rh_c3 + dewpoint)) - (rh_c2 * temperature / (rh_c3 + temperature)));
        }

        private bool SetGeoPosition()
        {
            using (GeoCoordinateWatcher watcher = new GeoCoordinateWatcher(GeoPositionAccuracy.Default))
            {
                GeoCoordinate coord = watcher.Position.Location;

                if (!coord.IsUnknown)
                {
                    latitude = (float)coord.Latitude;
                    longitude = (float)coord.Longitude;
                }
                else
                {
                    bool success = SetGeoFromIP();
                    if (!success)
                    {
                        return false;
                    }
                }
                return true;
            }
        }
        #endregion
        #endregion

        #region public
        public bool Init()
        {
            if (!geoLocationSet && !SetGeoPosition())
            {
                return false;
            }
                
            stationIDMosmix = GetMosmixStationFromGeoPosition(out name);
            return stationIDHistory != default;
        }
        public async Task<bool> UpdateNow()
        {
            if (stationIDMosmix == default && !await Task.Run(delegate { return Init(); }))
            {
                return false;
            }

            Task<Item>[] tasks = new Task<Item>[] {
                Task.Run(() => {
                    JsonCasts.CurrentMeasurement item = CachAwsCurrent();
                    if (item == default(JsonCasts.CurrentMeasurement)) 
                    {
                        return null;
                    }
                    return new Item{
                        Icon = ParsePoiIcon(item.icon),
                        Temperature = item.temperature == 32767 ? float.NaN : item.temperature / 10,
                        Wind = item.meanwind == 32767 ? float.NaN : item.meanwind / 10,
                        WindDirection = item.winddirection== 32767 ? float.NaN : item.winddirection / 10,
                        Precipitation = item.precipitation == 32767 ? float.NaN : item.precipitation / 10,
                        MaxWind = item.maxwind == 32767 ? float.NaN : item.maxwind / 10,
                        Humidity = item.humidity == 32767 ? float.NaN : item.humidity / 10
                    };
                }),
                Task.Run(() => {
                    ItemTimed[] item = GetPoisCSV();
                    if (item.Length <= 0)
                    {
                        return null;
                    }
                    return (Item)item[0];
                }),
                Task.Run(() =>
                {
                    return GetMosmixDataNow();
                }),
            };

            _now = new Item();
            bool success = false; 
            
            foreach (Item item in await Task.WhenAll(tasks))
            {
                success |= (item != null);
                if (item == null)
                {
                    continue;
                }
                _now.Temperature = _now.Temperature.Equals(float.NaN) ? item.Temperature : _now.Temperature;
                _now.Humidity = _now.Humidity.Equals(float.NaN) ? item.Humidity : _now.Humidity;
                _now.Precipitation = _now.Precipitation.Equals(float.NaN) ? item.Precipitation : _now.Precipitation;
                _now.WindDirection = _now.WindDirection.Equals(float.NaN) ? item.WindDirection : _now.WindDirection;
                _now.Wind = _now.Wind.Equals(float.NaN) ? item.Wind : _now.Wind;
                _now.MaxWind = _now.MaxWind.Equals(float.NaN) ? item.MaxWind : _now.MaxWind;
                _now.Icon = _now.Icon == WeatherIcon.None ? item.Icon : _now.Icon;
            }
            return success;
        }

        public async Task<bool> UpdateHighLow()
        {
            if (stationIDMosmix == default && !await Task.Run(delegate { return Init(); }))
            {
                return false;
            }

            bool success = false;
            if (_now == null)
            {
                success = await UpdateNow();
                if (!success)
                {
                    return false;
                }
            }

            _dailyHigh = new Item
            {
                Icon = WeatherIcon.None,
                Temperature = _now.Temperature,
                Precipitation = _now.Precipitation,
                Humidity = _now.Humidity,
                Wind = _now.Wind,
                MaxWind = _now.MaxWind,
                WindDirection = _now.WindDirection,
            };
            _dailyLow = new Item
            {
                Icon = WeatherIcon.None,
                Temperature = _now.Temperature,
                Precipitation = _now.Precipitation,
                Humidity = _now.Humidity,
                Wind = _now.Wind,
                MaxWind = _now.MaxWind,
                WindDirection = _now.WindDirection,
            };
            Task<Item[]>[] tasks = new Task<Item[]>[] {
                Task.Run(() => {
                    ItemTimed[] items = GetPoisCSV();
                    if (items == null || items.Length <= 0)
                    {
                        return new ItemTimed[0];
                    }
                    DateTime now = DateTime.Now;
                    List<ItemTimed> results = new List<ItemTimed>();
                    int fittedLength;
                    for (fittedLength = 0; fittedLength < items.Length; fittedLength++)
                    {
                        if (items[fittedLength].Time.DayOfYear != now.DayOfYear)
                        {
                            break;
                        }
                    }
                    ItemTimed[] fitted = new ItemTimed[fittedLength];
                    Array.Copy(items, fitted, fittedLength);
                    return (Item[])fitted;
                }),
                Task.Run(() =>
                {   
                    ItemTimed[] items = GetMosmixData();
                    if (items == null || items.Length <= 0)
                    {
                        return new ItemTimed[0];
                    }
                    DateTime now = DateTime.Now;
                    int fittedStart = 0;
                    int fittedLength;
                    for (fittedLength = 0; fittedLength < items.Length; fittedLength++)
                    {
                        if (items[fittedLength].Time.DayOfYear != now.DayOfYear)
                        {
                            if (fittedStart != fittedLength)
                            {
                                break;
                            }
                            fittedStart++;
                        }
                    }
                    ItemTimed[] fitted = new ItemTimed[fittedLength - fittedStart];
                    Array.Copy(items, fittedStart, fitted, 0, fittedLength);
                    return (Item[])fitted;
                }),
            };


            success = false;
            foreach (Item[] items in await Task.WhenAll(tasks))
            foreach (Item item in items)
            {
                success |= (item != null);
                if (item == null)
                {
                    continue;
                }
                _dailyHigh.Temperature = item.Temperature > _dailyHigh.Temperature ? item.Temperature : _dailyHigh.Temperature;
                _dailyHigh.Precipitation = item.Precipitation > _dailyHigh.Precipitation ? item.Precipitation : _dailyHigh.Precipitation;
                _dailyHigh.Humidity = item.Humidity > _dailyHigh.Humidity ? item.Humidity : _dailyHigh.Humidity;
                _dailyHigh.Wind = item.Wind > _dailyHigh.Wind ? item.Wind : _dailyHigh.Wind;
                _dailyHigh.MaxWind = item.MaxWind > _dailyHigh.MaxWind ? item.MaxWind : _dailyHigh.MaxWind;
                _dailyHigh.WindDirection = item.WindDirection > _dailyHigh.WindDirection ? item.WindDirection : _dailyHigh.WindDirection;

                _dailyLow.Temperature = item.Temperature < _dailyLow.Temperature ? item.Temperature : _dailyLow.Temperature;
                _dailyLow.Precipitation = item.Precipitation < _dailyLow.Precipitation ? item.Precipitation : _dailyLow.Precipitation;
                _dailyLow.Humidity = item.Humidity < _dailyLow.Humidity ? item.Humidity : _dailyLow.Humidity;
                _dailyLow.Wind = item.Wind < _dailyLow.Wind ? item.Wind : _dailyLow.Wind;
                _dailyLow.MaxWind = item.MaxWind < _dailyLow.MaxWind ? item.MaxWind : _dailyLow.MaxWind;
                _dailyLow.WindDirection = item.WindDirection < _dailyLow.WindDirection ? item.WindDirection : _dailyLow.WindDirection;
            }
            return success;
        }

        #endregion
    }
}