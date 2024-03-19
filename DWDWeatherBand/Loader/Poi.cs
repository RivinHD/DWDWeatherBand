using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.VisualBasic.FileIO;
using static DWDWeatherBand.DWDWeather;

namespace DWDWeatherBand.Loader
{
    internal class Poi : ILoader
    {
        public CacheItem[] Parse(byte[] data)
        {
            List<CacheItem> values = new List<CacheItem>();

            using (MemoryStream response = new MemoryStream(data))
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
                    CacheItem item = new CacheItem();
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

        public static WeatherIcon ParsePoiIcon(int icon)
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
    }
}
