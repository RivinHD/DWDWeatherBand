using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using HtmlAgilityPack;
using Newtonsoft.Json;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;
using TimeSpan = System.TimeSpan;

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
        // Current Information
        // First use WarnwetterAWSUri with currrent_measurement_{stationId}.json
        // complete with PoiUri, then MosmixLUri

        // Forcast Information
        // First use WarnwetterUri with stationOverviewExtended?stationIds={stationId}
        // complete with MosmixLUri

        // History Information use HistoryBaseUri

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
        public static Regex MosmixStationRegex = new Regex("<tr><td>(.*?)<\\/td><td.*?<\\/td><td.*?MN<\\/td><td .*?>(.*?)<\\/td><td.*?>(.*?)<\\/td><td.*?>(.*?)<\\/td>");
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

    public static class DWDWeather
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

        private static Loader.ILoader LoaderMosmix = new Loader.Mosmix();
        private static Loader.ILoader LoaderPoi = new Loader.Poi();

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

            public static Item Copy(Item item)
            {
                return new Item
                {
                    Icon = item.Icon,
                    Temperature = item.Temperature,
                    Precipitation = item.Precipitation,
                    Humidity = item.Humidity,
                    Wind = item.Wind,
                    MaxWind = item.MaxWind,
                    WindDirection = item.WindDirection,
                };
            }
        }

        public class ItemTimed : Item
        {
            public DateTime Time;
        }

        public class ItemTimedForcast : ItemTimed
        {
            public float PrecipitationProbability = float.NaN;
        }

        public class CacheItem : ItemTimedForcast
        {
            public float Dewpoint = float.NaN;
            public float CloudCover = float.NaN;
            public float Weather = float.NaN;
        }

        private static string name;
        private static string stationIDMosmix;
        private static string[,] stationIDHistory = new string[DWDSettings.Resolutions.Length, DWDSettings.DataTypes.Length];
        private static bool geoLocationSet = false;
        private static double latitude;
        private static double longitude;

        private static KeyValuePair<DateTime, CacheItem[]> cacheMosmix;
        private static KeyValuePair<DateTime, CacheItem[]> cachePoi;
        private static KeyValuePair<DateTime, JsonCasts.CurrentMeasurement> cacheAwsCurrent;
        private static KeyValuePair<DateTime, CacheItem[]> cacheAwsStation;
        private static KeyValuePair<DateTime, CacheItem[]>[,] cacheHistory = new KeyValuePair<DateTime, CacheItem[]>[DWDSettings.Resolutions.Length, DWDSettings.DataTypes.Length];

        private static Item _now;
        private static Item _dailyHigh;
        private static Item _dailyLow;
        private static Item _absoluteHigh;
        private static Item _absoluteLow;
        private static ItemTimedForcast[] _forcast;
        private static bool _finishedInit = false;

        public static Item Now { get { return _now; } }
        public static Item DailyHigh { get { return _dailyHigh; } }
        public static Item DailyLow { get { return _dailyLow; } }
        public static Item AbsoluteHigh { get { return _absoluteHigh; } }
        public static Item AbsoluteLow { get { return _absoluteLow; } }
        public static ItemTimedForcast[] Forcast { get { return _forcast; } }
        public static bool FinishedInit { get { return _finishedInit; } }
        public static string StationName { get { return name; } }
        #endregion

        #region private

        #region Mosmix
        private static string GetMosmixStationFromGeoPosition(out string Name)
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
            KeyValuePair<string, string> closest = new KeyValuePair<string, string>();
            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        return null;
                    }
                    using (BufferedStream bufferedStream = new BufferedStream(stream))
                    {
                        HtmlDocument html = new HtmlDocument();
                        html.Load(bufferedStream);
                        HtmlNodeCollection selection = html.DocumentNode.SelectNodes("//tr");
                        double latitudeDelta = double.MaxValue;
                        double longitudeDelta = double.MaxValue;
                        foreach (HtmlNode item in selection)
                        {
                            //Groups 1=Name  2=StaionID  3=Latitude  4=Longitude
                            Match parsed = DWDSettings.MosmixStationRegex.Match(Regex.Replace(item.OuterHtml, @"\t|\n|\r", ""));
                            if (!parsed.Success)
                            {
                                continue;
                            }
                            float lat = float.Parse(parsed.Groups[3].Value, CultureInfo.InvariantCulture);
                            float lon = float.Parse(parsed.Groups[4].Value, CultureInfo.InvariantCulture);
                            double deltaLat = (latitude - lat) * (latitude - lat);
                            double deltaLon = (longitude - lon) * (longitude - lon);
                            string value = parsed.Groups[2].Value;
                            if (deltaLat + deltaLon < latitudeDelta + longitudeDelta && UriIsOK(new Uri(DWDSettings.PoiUri, GetPoiName(value))) && UriIsOK(new Uri(DWDSettings.MosmixLUri, value)))
                            {
                                closest = new KeyValuePair<string, string>(parsed.Groups[1].Value, value);
                                latitudeDelta = deltaLat;
                                longitudeDelta = deltaLon;
                            }
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

            if (closest.Value == null)
            {
                return null;
            }

            Console.WriteLine("Finished Get Station ID");
            Name = closest.Key;
            return closest.Value;
        }

        private static bool CacheMosmix(Uri uri)
        {
            KeyValuePair<DateTime, CacheItem[]> cache = CacheData(LoaderMosmix, uri, cacheMosmix, new TimeSpan(1, 0, 0));
            if (cache.Equals(default(KeyValuePair<DateTime, CacheItem[]>)))
            {
                return false;
            }
            cacheMosmix = cache;
            return true;
        }

        private static Item GetMosmixDataNow()
        {
            bool success = CacheMosmix(new Uri(DWDSettings.MosmixLUri, $"{stationIDMosmix}/kml/MOSMIX_L_LATEST_{stationIDMosmix}.kmz"));
            if (!success)
            {
                return null;
            }

            CacheItem[] data = cacheMosmix.Value;

            DateTime now = DateTime.Now;
            int selectedColumn = 0;
            foreach (CacheItem item in data)
            {
                DateTime time = item.Time;
                if (time.Hour == now.Hour && time.Year == now.Year && time.DayOfYear == now.DayOfYear)
                {
                    break;
                }
                selectedColumn++;
            }
            if (selectedColumn >= data.Count())
            {
                return null;
            }

            CacheItem low = data[selectedColumn];
            CacheItem high = data[selectedColumn +1];

            float temperature = high.Temperature * now.Minute / 60 + low.Temperature - 273.15f;
            float dewpoint = high.Dewpoint * now.Minute / 60 + low.Dewpoint - 273.15f;
            float cloudCover = high.CloudCover * now.Minute / 60 + low.CloudCover;
            return new Item
            {
                Temperature = temperature,
                Humidity = (float)CalculateHumidity(dewpoint, temperature),
                Wind = (high.Wind * now.Minute / 60 + low.Wind) * 3.6f,
                WindDirection = high.WindDirection * now.Minute / 60 + low.WindDirection,
                MaxWind = (high.MaxWind * now.Minute / 60 + low.MaxWind) * 3.6f,
                Precipitation = high.Precipitation * now.Minute / 60 + low.Precipitation,
                Icon = Loader.Mosmix.GetMosmixIcon((int)low.Weather, cloudCover)
            };
        }

        private static ItemTimedForcast[] GetMosmixData()
        {
            bool success = CacheMosmix(new Uri(DWDSettings.MosmixLUri, $"{stationIDMosmix}/kml/MOSMIX_L_LATEST_{stationIDMosmix}.kmz"));
            if (!success)
            {
                return null;
            }

            return cacheMosmix.Value;
        }
        #endregion

        #region Poi
        private static string GetPoiName(string stationID)
        {
            while (stationID.Length < 5)
            {
                stationID += "_";
            }
            return $"{stationID}-BEOB.csv";
        }

        private static bool CachePoi(Uri uri)
        {
            KeyValuePair<DateTime, CacheItem[]> cache = CacheData(LoaderPoi, uri, cachePoi, new TimeSpan(0, 15, 0));
            if (cache.Equals(default(KeyValuePair<DateTime, CacheItem[]>)))
            {
                return false;
            }
            cachePoi = cache;
            return true;
        }
        private static ItemTimed[] GetPoisCSV()
        {
            bool success = CachePoi(new Uri(DWDSettings.PoiUri, GetPoiName(stationIDMosmix)));
            if (!success)
            {
                return null;
            }

            return cachePoi.Value;
        }
        #endregion

        #region History
        private static Uri CombineHistoryUri(Resolution Resolution, DataTyp DataTyp)
        {
            if (Resolution == Resolution.Minutes10 || Resolution == Resolution.Hourly)
            {
                return new Uri(DWDSettings.HistroyBaseUri, $"{DWDSettings.Resolutions[(int)Resolution]}/{DWDSettings.DataTypes[(int)DataTyp]}");
            }
            return new Uri(DWDSettings.HistroyBaseUri, $"{DWDSettings.Resolutions[(int)Resolution]}/kl");
        }

        private static string GetHistoryStationFromGeoPosition(Resolution Resolution, DataTyp DataTyp, out string Name)
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

                    using (BufferedStream bufferedStream = new BufferedStream(stream))
                    using (StreamReader reader = new StreamReader(bufferedStream, Encoding.UTF8))
                    {
                        reader.ReadLine(); // Skip first 2 lines
                        reader.ReadLine(); // because these are table header

                        string line;
                        double latitudeDelta = float.MaxValue;
                        double longitudeDelta = float.MaxValue;
                        while ((line = reader.ReadLine()) != null)
                        {
                            //Groups 1=StationId  2=Latitude  3=Longitude  4=Name  
                            Match parsed = DWDSettings.HistoryStationRegex.Match(line);
                            float lat = float.Parse(parsed.Groups[2].Value, CultureInfo.InvariantCulture);
                            float lon = float.Parse(parsed.Groups[3].Value, CultureInfo.InvariantCulture);
                            double deltaLat = (latitude - lat) * (latitude - lat);
                            double deltaLon = (longitude - lon) * (longitude - lon);
                            if (deltaLat + deltaLon < latitudeDelta + longitudeDelta)
                            {
                                stationID = parsed.Groups[1].Value;
                                Name = parsed.Groups[4].Value;
                                latitudeDelta = deltaLat;
                                longitudeDelta = deltaLon;
                            }
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
        private static JsonCasts.CurrentMeasurement CachAwsCurrent()
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
        private static bool SetGeoFromIP()
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
        private static T GetJson<T>(Uri uri)
        {
            HttpWebRequest request = WebRequest.CreateHttp(uri);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.Method = "GET";
            request.Timeout = 500;
            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (BufferedStream bufferedResponse = new BufferedStream(response.GetResponseStream()))
                using (StreamReader sr = new StreamReader(bufferedResponse, Encoding.UTF8))
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
        private static bool UriIsOK(Uri uri)
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

        private static KeyValuePair<DateTime, CacheItem[]> CacheData(Loader.ILoader loader, Uri uri, KeyValuePair<DateTime, CacheItem[]> current, TimeSpan expireTime)
        {
            if (current.Key != default && current.Key > DateTime.Now)
            {
                return current;
            }

            try
            {
                using (CustomeWebClient client = new CustomeWebClient{Timeout = 500})
                {
                    return new KeyValuePair<DateTime, CacheItem[]>(DateTime.Now + expireTime, loader.Parse(client.DownloadData(uri)));
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

        private static double CalculateHumidity(double dewpoint, double temperature)
        {
            // Humidity based on https://www.dwd.de/DE/leistungen/met_verfahren_mosmix/faq/relative_feuchte.html
            double rh_c2 = 17.5043;
            double rh_c3 = 241.2;
            return 100 * Math.Exp((rh_c2 * dewpoint / (rh_c3 + dewpoint)) - (rh_c2 * temperature / (rh_c3 + temperature)));
        }

        private static bool SetGeoPosition()
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
        public static bool Init(double Latitude, double Longitude)
        {
            latitude = Latitude;
            longitude = Longitude;
            geoLocationSet = true;
            return Init();
        }

        public static bool Init()
        {

            if (!geoLocationSet && !SetGeoPosition())
            {
                return false;
            }
                
            stationIDMosmix = GetMosmixStationFromGeoPosition(out name);
            _finishedInit = stationIDMosmix != default;
#if DEBUG
            System.Windows.Forms.MessageBox.Show(
                $"{stationIDMosmix} {name}",
                "StationIDMosmix",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
#endif
            return _finishedInit;
        }
        public static async Task<bool> UpdateNow()
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
                        Icon = Loader.Poi.ParsePoiIcon(item.icon),
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
                    if (item ==null || item.Length <= 0)
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

        public static async Task<bool> UpdateHighLow()
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

            _dailyHigh = Item.Copy(_now);
            _dailyHigh.Icon = WeatherIcon.None;
            _dailyLow = Item.Copy(_now);
            _dailyHigh.Icon = WeatherIcon.None;
            _absoluteHigh = Item.Copy(_now);
            _absoluteHigh.Icon = WeatherIcon.None;
            _absoluteLow = Item.Copy(_now);
            _absoluteLow.Icon = WeatherIcon.None;

            Task<ItemTimed[]>[] tasks = new Task<ItemTimed[]>[] {
                Task.Run(() => {
                    ItemTimed[] items = GetPoisCSV();
                    if (items == null || items.Length <= 0)
                    {
                        return new ItemTimed[0];
                    }
                    return items;
                }),
                Task.Run(() =>
                {   
                    ItemTimed[] items = GetMosmixData();
                    if (items == null || items.Length <= 0)
                    {
                        return new ItemTimed[0];
                    }
                    return items;
                }),
            };

            int currentDay = DateTime.Now.DayOfYear;
            success = false;
            foreach (ItemTimed[] items in await Task.WhenAll(tasks))
            foreach (ItemTimed item in items)
            {
                success |= (item != null);
                if (item == null)
                {
                    continue;
                }

                if (currentDay == item.Time.DayOfYear)
                {
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

                _absoluteHigh.Temperature = item.Temperature > _absoluteHigh.Temperature ? item.Temperature : _absoluteHigh.Temperature;
                _absoluteHigh.Precipitation = item.Precipitation > _absoluteHigh.Precipitation ? item.Precipitation : _absoluteHigh.Precipitation;
                _absoluteHigh.Humidity = item.Humidity > _absoluteHigh.Humidity ? item.Humidity : _absoluteHigh.Humidity;
                _absoluteHigh.Wind = item.Wind > _absoluteHigh.Wind ? item.Wind : _absoluteHigh.Wind;
                _absoluteHigh.MaxWind = item.MaxWind > _absoluteHigh.MaxWind ? item.MaxWind : _absoluteHigh.MaxWind;
                _absoluteHigh.WindDirection = item.WindDirection > _absoluteHigh.WindDirection ? item.WindDirection : _absoluteHigh.WindDirection;

                _absoluteLow.Temperature = item.Temperature < _absoluteLow.Temperature ? item.Temperature : _absoluteLow.Temperature;
                _absoluteLow.Precipitation = item.Precipitation < _absoluteLow.Precipitation ? item.Precipitation : _absoluteLow.Precipitation;
                _absoluteLow.Humidity = item.Humidity < _absoluteLow.Humidity ? item.Humidity : _absoluteLow.Humidity;
                _absoluteLow.Wind = item.Wind < _absoluteLow.Wind ? item.Wind : _absoluteLow.Wind;
                _absoluteLow.MaxWind = item.MaxWind < _absoluteLow.MaxWind ? item.MaxWind : _absoluteLow.MaxWind;
                _absoluteLow.WindDirection = item.WindDirection < _absoluteLow.WindDirection ? item.WindDirection : _absoluteLow.WindDirection;
            }
            return success;
        }
        public static async Task<bool> UpdateForcast()
        {
            if (stationIDMosmix == default && !await Task.Run(delegate { return Init(); }))
            {
                return false;
            }

            Task<ItemTimedForcast[]> task = Task.Run(() =>
            {
                ItemTimedForcast[] _items = GetMosmixData();
                if (_items == null || _items.Length <= 0)
                {
                    return new ItemTimedForcast[0];
                }
                return _items;
            });

            _forcast = await task;
            return true;
        }
        #endregion
    }
}