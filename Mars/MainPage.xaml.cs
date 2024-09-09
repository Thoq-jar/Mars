// ReSharper disable PropertyCanBeMadeInitOnly.Global

using Newtonsoft.Json.Linq;

namespace Mars;

public partial class MainPage
{
    private readonly HttpClient _httpClient;

    public MainPage()
    {
        InitializeComponent();
        _httpClient = new HttpClient();
        _ = GetWeather();
    }

    private async Task GetWeather()
    {
        try
        {
            var location = await GetLocationAsync();
            if (location == null)
            {
                ErrorText.Text = "Unable to retrieve location data.";
                return;
            }

            var weatherData = await GetWeatherDataAsync(location.Value.Item1, location.Value.Item2);
            if (weatherData != null)
            {
                Temperature.Text = $"The temperature is around {weatherData.Temperature}°F";
                Humidity.Text = $"While the humidity is: {weatherData.Humidity}%";
                WindSpeed.Text = $"The Wind speed is: {weatherData.WindSpeed} m/s";
                Condition.Text = $"{weatherData.WeatherCondition}";
            }
            else
            {
                ErrorText.Text = "Error fetching weather data.";
            }
        }
        catch (HttpRequestException ex)
        {
            ErrorText.Text = $"Error fetching weather data: {ex.Message}";
            Console.WriteLine(ErrorText.Text);
            SemanticScreenReader.Announce(ErrorText.Text);
        }
    }

    private async Task<(double, double)?> GetLocationAsync()
    {
        const string ipApiUrl = "https://ipinfo.io/json";
        try
        {
            var ipResponse = await _httpClient.GetStringAsync(ipApiUrl);
            var locationData = JObject.Parse(ipResponse);
            var coordinates = locationData["loc"]?.ToString().Split(',');
            if (coordinates is { Length: 2 } &&
                double.TryParse(coordinates[0], out var latitude) &&
                double.TryParse(coordinates[1], out var longitude))
            {
                return (latitude, longitude);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving location: {ex.Message}");
        }

        return null;
    }

    private async Task<WeatherData?> GetWeatherDataAsync(double latitude, double longitude)
    {
        const string openMeteoUrl = "https://api.open-meteo.com/v1/forecast";
        try
        {
            var weatherUrl =
                $"{openMeteoUrl}?latitude={latitude}&longitude={longitude}&hourly=temperature_2m,relative_humidity_2m,weathercode,windspeed_10m";
            var weatherResponse = await _httpClient.GetStringAsync(weatherUrl);
            var weatherData = JObject.Parse(weatherResponse);

            if (weatherData["hourly"]?["temperature_2m"] is not JArray temperatureArray ||
                weatherData["hourly"]?["relative_humidity_2m"] is not JArray humidityArray ||
                weatherData["hourly"]?["weathercode"] is not JArray weatherCodeArray ||
                weatherData["hourly"]?["windspeed_10m"] is not JArray windSpeedArray ||
                temperatureArray.Count == 0 || humidityArray.Count == 0 || weatherCodeArray.Count == 0 ||
                windSpeedArray.Count == 0)
                return null;

            var temperatureC = temperatureArray[0].Value<double>();
            var temperatureF = temperatureC * 9 / 5 + 32;
            var humidity = humidityArray[0].Value<double>();
            var weatherConditionCode = weatherCodeArray[0].ToString();
            var windSpeed = windSpeedArray[0].Value<double>();

            var (weatherCondition, iconUrl) = GetWeatherConditionDescription(weatherConditionCode);

            WeatherIcon.Source = iconUrl;

            return new WeatherData
            {
                Temperature = temperatureF,
                Humidity = humidity,
                WeatherCondition = weatherCondition,
                WindSpeed = windSpeed
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching weather data: {ex.Message}");
            return null;
        }
    }

private static (string description, string iconUrl) GetWeatherConditionDescription(string weatherCode)
{
    return weatherCode switch
    {
        "0" => ("Clear sky's today", "https://openweathermap.org/img/wn/01d@2x.png"),
        "1" => ("It's mainly clear today", "https://openweathermap.org/img/wn/02d@2x.png"),
        "2" => ("It's partly cloudy today", "https://openweathermap.org/img/wn/03d@2x.png"),
        "3" => ("Overcast", "https://openweathermap.org/img/wn/04d@2x.png"),
        "01" => ("It's a nice clear sky at night", "https://openweathermap.org/img/wn/01n@2x.png"),
        "02" => ("It's a mainly clear at night", "https://openweathermap.org/img/wn/02n@2x.png"),
        "03" => ("It's partly cloudy at night", "https://openweathermap.org/img/wn/03n@2x.png"),
        "04" => ("Overcast at night", "https://openweathermap.org/img/wn/04n@2x.png"),
        "05" => ("It's misting right now", "https://openweathermap.org/img/wn/50d@2x.png"),
        "06" => ("Smokey today", "https://openweathermap.org/img/wn/50d@2x.png"),
        "07" => ("Hazy today", "https://openweathermap.org/img/wn/50d@2x.png"),
        "08" => ("Dusty today", "https://openweathermap.org/img/wn/50d@2x.png"),
        "09" => ("Sandy today", "https://openweathermap.org/img/wn/50d@2x.png"),
        "10" => ("Ashy today", "https://openweathermap.org/img/wn/50d@2x.png"),
        "11" => ("Squalls", "https://openweathermap.org/img/wn/50d@2x.png"),
        "12" => ("Today brings a tornado", "https://openweathermap.org/img/wn/50d@2x.png"),
        "13" => ("Today brings a tropical storm", "https://openweathermap.org/img/wn/50d@2x.png"),
        "14" => ("Today brings a hurricane", "https://openweathermap.org/img/wn/50d@2x.png"),
        "15" => ("Today brings a cold front", "https://openweathermap.org/img/wn/50d@2x.png"),
        "16" => ("Today brings a warm front", "https://openweathermap.org/img/wn/50d@2x.png"),
        "17" => ("Today brings a stationary front", "https://openweathermap.org/img/wn/50d@2x.png"),
        "18" => ("Today brings a occluded front", "https://openweathermap.org/img/wn/50d@2x.png"),
        "19" => ("Unknown weather phenomenon", "https://openweathermap.org/img/wn/50d@2x.png"),
        "20" => ("Unknown", ""),
        "21" => ("Unknown", ""),
        "22" => ("Unknown", ""),
        "23" => ("Unknown", ""),
        "24" => ("Unknown", ""),
        "25" => ("Unknown", ""),
        "26" => ("Unknown", ""),
        "27" => ("Unknown", ""),
        "28" => ("Unknown", ""),
        "29" => ("Unknown", ""),
        "30" => ("Unknown", ""),
        "31" => ("Unknown", ""),
        "32" => ("Unknown", ""),
        "33" => ("Unknown", ""),
        "34" => ("Unknown", ""),
        "35" => ("Unknown", ""),
        "36" => ("Unknown", ""),
        "37" => ("Unknown", ""),
        "38" => ("Unknown", ""),
        "39" => ("Unknown", ""),
        "40" => ("Unknown", ""),
        "41" => ("Unknown", ""),
        "42" => ("Unknown", ""),
        "43" => ("Unknown", ""),
        "44" => ("Unknown", ""),
        "45" => ("It's fogy today", "https://openweathermap.org/img/wn/50d@2x.png"),
        "46" => ("Unknown", ""),
        "47" => ("Unknown", ""),
        "48" => ("Depositing rime fog", "https://openweathermap.org/img/wn/50d@2x.png"),
        "49" => ("Unknown", ""),
        "50" => ("Unknown", ""),
        "51" => ("It's a light drizzle today", "https://openweathermap.org/img/wn/09d@2x.png"),
        "52" => ("It's a moderate drizzle today", "https://openweathermap.org/img/wn/09d@2x.png"),
        "53" => ("It's a dense drizzle today", "https://openweathermap.org/img/wn/09d@2x.png"),
        "54" => ("Unknown", ""),
        "55" => ("Some light rain today", "https://openweathermap.org/img/wn/10d@2x.png"),
        "56" => ("Some moderate rain today", "https://openweathermap.org/img/wn/10d@2x.png"),
        "57" => ("Some heavy rain today", "https://openweathermap.org/img/wn/10d@2x.png"),
        "58" => ("Unknown", ""),
        "59" => ("Unknown", ""),
        "60" => ("Unknown", ""),
        "61" => ("Nice, easy light snow", "https://openweathermap.org/img/wn/13d@2x.png"),
        "62" => ("Today brings moderate snow", "https://openweathermap.org/img/wn/13d@2x.png"),
        "63" => ("Get your shovel ready! Today brings heavy snow", "https://openweathermap.org/img/wn/13d@2x.png"),
        "64" => ("Unknown", ""),
        "65" => ("Unknown", ""),
        "66" => ("Unknown", ""),
        "67" => ("Unknown", ""),
        "68" => ("Unknown", ""),
        "69" => ("Unknown", ""),
        "70" => ("Unknown", ""),
        "71" => ("Light rain showers", "https://openweathermap.org/img/wn/09d@2x.png"),
        "72" => ("Moderate rain showers", "https://openweathermap.org/img/wn/09d@2x.png"),
        "73" => ("Heavy rain showers", "https://openweathermap.org/img/wn/09d@2x.png"),
        "74" => ("Unknown", ""),
        "75" => ("Unknown", ""),
        "76" => ("Unknown", ""),
        "77" => ("Unknown", ""),
        "78" => ("Unknown", ""),
        "79" => ("Unknown", ""),
        "80" => ("Some rain showers", "https://openweathermap.org/img/wn/09d@2x.png"),
        "81" => ("some rain showers with thunder", "https://openweathermap.org/img/wn/11d@2x.png"),
        "82" => ("Heavy rain showers with thunder", "https://openweathermap.org/img/wn/11d@2x.png"),
        "83" => ("Unknown", ""),
        "84" => ("Unknown", ""),
        "85" => ("Unknown", ""),
        "86" => ("Unknown", ""),
        "87" => ("Unknown", ""),
        "88" => ("Unknown", ""),
        "89" => ("Unknown", ""),
        "90" => ("Unknown", ""),
        "91" => ("Unknown", ""),
        "92" => ("Unknown", ""),
        "93" => ("Unknown", ""),
        "94" => ("Unknown", ""),
        "95" => ("BOOM! Thunderstorms", "https://openweathermap.org/img/wn/11d@2x.png"),
        "96" => ("Ouch! Thunderstorms with hail", "https://openweathermap.org/img/wn/11d@2x.png"),
        "97" => ("Unknown", ""),
        "98" => ("Unknown", ""),
        "99" => ("Hide... Thunderstorms with heavy hail", "https://openweathermap.org/img/wn/11d@2x.png"),
        _ => ("Unknown", "")
    };
}}

public class WeatherData
{
    public double Temperature { get; set; }
    public double Humidity { get; set; }
    public string? WeatherCondition { get; set; }
    public double WindSpeed { get; set; }
}