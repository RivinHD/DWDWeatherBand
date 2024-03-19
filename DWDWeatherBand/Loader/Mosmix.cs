using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using static DWDWeatherBand.DWDWeather;

namespace DWDWeatherBand.Loader
{
    internal class Mosmix : ILoader
    {
        public CacheItem[] Parse(byte[] data)
        {
            XDocument doc;
            using (MemoryStream response = new MemoryStream(data))
            using (BufferedStream bufferedResponse = new BufferedStream(response))
            using (ZipArchive archive = new ZipArchive(bufferedResponse, ZipArchiveMode.Read))
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
            IEnumerable<XElement> Xdata = document.Element(kml + "Placemark").Element(kml + "ExtendedData").Elements(dwd + "Forecast");

            int dataLength = timeStemps.Count();

            CacheItem[] items = new CacheItem[dataLength];
            int index = 0;
            foreach (XElement element in timeStemps)
            {
                items[index] = new CacheItem() { Time = DateTime.Parse(element.Value) };
                index++;
            }

            foreach (XElement element in Xdata)
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
                            items[i].Dewpoint = float.Parse(values[i], CultureInfo.InvariantCulture) - 273.15f;
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
                            items[i].CloudCover = float.Parse(values[i], CultureInfo.InvariantCulture);
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
                            items[i].Weather = float.Parse(values[i], CultureInfo.InvariantCulture);
                        }
                        break;
                    case "R101": // Probability of precipitation > 0.1 mm during the last hour
                        values = element.Value.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 0; i < dataLength; i++)
                        {
                            if (values[i] == defaultSign)
                            {
                                continue;
                            }
                            items[i].PrecipitationProbability = float.Parse(values[i], CultureInfo.InvariantCulture);
                        }
                        break;

                    default: break;
                }
            }

            for (int i = 0; i < dataLength; i++)
            {
                items[i].Humidity = (float)CalculateHumidity(items[i].Dewpoint, items[i].Temperature);
                items[i].Icon = GetMosmixIcon((int)items[i].Weather, items[i].CloudCover);
            }

            return items;
        }

        private static double CalculateHumidity(double dewpoint, double temperature)
        {
            // Humidity based on https://www.dwd.de/DE/leistungen/met_verfahren_mosmix/faq/relative_feuchte.html
            double rh_c2 = 17.5043;
            double rh_c3 = 241.2;
            return 100 * Math.Exp((rh_c2 * dewpoint / (rh_c3 + dewpoint)) - (rh_c2 * temperature / (rh_c3 + temperature)));
        }

        public static WeatherIcon GetMosmixIcon(int weather, float cloudCover)
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
            // Hagel ist schwer Vorhersagen: https://www.dwd.de/DE/klimaumwelt/klimaforschung/spez_themen/hagel/hagel_node.html
            return icon;
        }
    }
}
