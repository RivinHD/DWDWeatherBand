namespace DWDWeatherBand.Loader
{
    public interface ILoader
    {
        DWDWeather.CacheItem[] Parse(byte[] data);
    }
}
