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
        Status.Text = "Loading weather...";

        try
        {
            var location = await GetLocationAsync();
            if (location == null)
            {
                Status.Text = "Unable to retrieve location data.";
                return;
            }

            var weatherData = await GetWeatherDataAsync(location.Value.Item1, location.Value.Item2);
            if (weatherData != null)
            {
                MaxTemp.Text = $"Max Temp: {weatherData.MaxTemp}°F";
                MinTemp.Text = $"Min Temp: {weatherData.MinTemp}˚F";
                Precipitation.Text = $"Precipitation: {weatherData.Precipitation}mm";
                Temperature.Text = $"The temperature is around {weatherData.Temperature}°F";
                Humidity.Text = $"While the humidity is: {weatherData.Humidity}%";
                WindSpeed.Text = $"The Wind speed is: {weatherData.WindSpeed} m/s";
                Condition.Text = $"{weatherData.WeatherCondition}";
            }
            else
            {
                Status.Text = "Error fetching weather data.";
            }
        }
        catch (HttpRequestException exception)
        {
            Status.Text = $"Error fetching weather data: {exception.Message}";
            Console.WriteLine(Status.Text);
            SemanticScreenReader.Announce(Status.Text);
        }
        finally
        {
            if (Status.Text == "Loading weather...") Status.Text = "";
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
                return (latitude, longitude);
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Error retrieving location: {exception.Message}");
        }

        return null;
    }

    private async Task<WeatherData?> GetWeatherDataAsync(double latitude, double longitude)
    {
        const string openMeteoUrl = "https://api.open-meteo.com/v1/forecast";
        try
        {
            var weatherUrl =
                $"{openMeteoUrl}?latitude={latitude}&longitude={longitude}&hourly=temperature_2m,relative_humidity_2m,weathercode,windspeed_10m&daily=temperature_2m_max,temperature_2m_min,precipitation_sum,weathercode";
            var weatherResponse = await _httpClient.GetStringAsync(weatherUrl);
            var weatherData = JObject.Parse(weatherResponse);

            if (weatherData["hourly"]?["temperature_2m"] is not JArray temperatureArray ||
                weatherData["hourly"]?["relative_humidity_2m"] is not JArray humidityArray ||
                weatherData["hourly"]?["weathercode"] is not JArray weatherCodeArray ||
                weatherData["hourly"]?["windspeed_10m"] is not JArray windSpeedArray ||
                temperatureArray.Count == 0 || humidityArray.Count == 0 || weatherCodeArray.Count == 0 ||
                windSpeedArray.Count == 0)
                return null;

            var temperatureCelsius = temperatureArray[0].Value<double>();
            var temperatureFahrenheit = temperatureCelsius * 9 / 5 + 32;
            var humidity = humidityArray[0].Value<double>();
            var weatherConditionCode = weatherCodeArray[0].ToString();
            var windSpeedMetersASecond = windSpeedArray[0].Value<double>();
            var (weatherCondition, iconUrl) = GetWeatherConditionDescription(weatherConditionCode);

            WeatherIcon.Source = iconUrl;

            if (weatherData["daily"]?["temperature_2m_max"] is not JArray maxTempArray ||
                weatherData["daily"]?["temperature_2m_min"] is not JArray minTempArray ||
                weatherData["daily"]?["precipitation_sum"] is not JArray precipitationArray ||
                maxTempArray.Count == 0 || minTempArray.Count == 0 || precipitationArray.Count == 0)
                return null;

            var maxTemperature = maxTempArray[0].Value<double>() * 9 / 5 + 32;
            var minTemperature = minTempArray[0].Value<double>() * 9 / 5 + 32;
            var totalPrecipitation = precipitationArray[0].Value<double>();

            return new WeatherData
            {
                Temperature = temperatureFahrenheit,
                Humidity = humidity,
                WeatherCondition = weatherCondition,
                WindSpeed = windSpeedMetersASecond,
                MaxTemp = maxTemperature,
                MinTemp = minTemperature,
                Precipitation = totalPrecipitation
            };
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Error fetching weather data: {exception.Message}");
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
            "11" => ("Hold on to your hats! Squalls today...", "https://openweathermap.org/img/wn/50d@2x.png"),
            "12" => ("Today brings a tornado", "https://openweathermap.org/img/wn/50d@2x.png"),
            "13" => ("Today brings a tropical storm", "https://openweathermap.org/img/wn/50d@2x.png"),
            "14" => ("Today brings a hurricane", "https://openweathermap.org/img/wn/50d@2x.png"),
            "15" => ("Today brings a cold front", "https://openweathermap.org/img/wn/50d@2x.png"),
            "16" => ("Today brings a warm front", "https://openweathermap.org/img/wn/50d@2x.png"),
            "17" => ("Today brings a stationary front", "https://openweathermap.org/img/wn/50d@2x.png"),
            "18" => ("Today brings a occluded front", "https://openweathermap.org/img/wn/50d@2x.png"),
            "19" => ("Unknown weather condition weather phenomenon", "https://openweathermap.org/img/wn/50d@2x.png"),
            "20" => ("Unknown weather condition", ""),
            "21" => ("Unknown weather condition", ""),
            "22" => ("Unknown weather condition", ""),
            "23" => ("Unknown weather condition", ""),
            "24" => ("Unknown weather condition", ""),
            "25" => ("Unknown weather condition", ""),
            "26" => ("Unknown weather condition", ""),
            "27" => ("Unknown weather condition", ""),
            "28" => ("Unknown weather condition", ""),
            "29" => ("Unknown weather condition", ""),
            "30" => ("Unknown weather condition", ""),
            "31" => ("Unknown weather condition", ""),
            "32" => ("Unknown weather condition", ""),
            "33" => ("Unknown weather condition", ""),
            "34" => ("Unknown weather condition", ""),
            "35" => ("Unknown weather condition", ""),
            "36" => ("Unknown weather condition", ""),
            "37" => ("Unknown weather condition", ""),
            "38" => ("Unknown weather condition", ""),
            "39" => ("Unknown weather condition", ""),
            "40" => ("Unknown weather condition", ""),
            "41" => ("Unknown weather condition", ""),
            "42" => ("Unknown weather condition", ""),
            "43" => ("Unknown weather condition", ""),
            "44" => ("Unknown weather condition", ""),
            "45" => ("It's fogy today", "https://openweathermap.org/img/wn/50d@2x.png"),
            "46" => ("Unknown weather condition", ""),
            "47" => ("Unknown weather condition", ""),
            "48" => ("Depositing rime fog", "https://openweathermap.org/img/wn/50d@2x.png"),
            "49" => ("Unknown weather condition", ""),
            "50" => ("Unknown weather condition", ""),
            "51" => ("It's a light drizzle today", "https://openweathermap.org/img/wn/09d@2x.png"),
            "52" => ("It's a moderate drizzle today", "https://openweathermap.org/img/wn/09d@2x.png"),
            "53" => ("It's a dense drizzle today", "https://openweathermap.org/img/wn/09d@2x.png"),
            "54" => ("Unknown weather condition", ""),
            "55" => ("Some light rain today", "https://openweathermap.org/img/wn/10d@2x.png"),
            "56" => ("Some moderate rain today", "https://openweathermap.org/img/wn/10d@2x.png"),
            "57" => ("Some heavy rain today", "https://openweathermap.org/img/wn/10d@2x.png"),
            "58" => ("Unknown weather condition", ""),
            "59" => ("Unknown weather condition", ""),
            "60" => ("Unknown weather condition", ""),
            "61" => ("Nice, easy light snow", "https://openweathermap.org/img/wn/13d@2x.png"),
            "62" => ("Today brings moderate snow", "https://openweathermap.org/img/wn/13d@2x.png"),
            "63" => ("Get your shovel ready! Today brings heavy snow", "https://openweathermap.org/img/wn/13d@2x.png"),
            "64" => ("Unknown weather condition", ""),
            "65" => ("Unknown weather condition", ""),
            "66" => ("Unknown weather condition", ""),
            "67" => ("Unknown weather condition", ""),
            "68" => ("Unknown weather condition", ""),
            "69" => ("Unknown weather condition", ""),
            "70" => ("Unknown weather condition", ""),
            "71" => ("Light rain showers", "https://openweathermap.org/img/wn/09d@2x.png"),
            "72" => ("Moderate rain showers", "https://openweathermap.org/img/wn/09d@2x.png"),
            "73" => ("Heavy rain showers", "https://openweathermap.org/img/wn/09d@2x.png"),
            "74" => ("Unknown weather condition", ""),
            "75" => ("Unknown weather condition", ""),
            "76" => ("Unknown weather condition", ""),
            "77" => ("Unknown weather condition", ""),
            "78" => ("Unknown weather condition", ""),
            "79" => ("Unknown weather condition", ""),
            "80" => ("Some rain showers", "https://openweathermap.org/img/wn/09d@2x.png"),
            "81" => ("Some rain showers with thunder", "https://openweathermap.org/img/wn/11d@2x.png"),
            "82" => ("Heavy rain showers with thunder", "https://openweathermap.org/img/wn/11d@2x.png"),
            "83" => ("Unknown weather condition", ""),
            "84" => ("Unknown weather condition", ""),
            "85" => ("Unknown weather condition", ""),
            "86" => ("Unknown weather condition", ""),
            "87" => ("Unknown weather condition", ""),
            "88" => ("Unknown weather condition", ""),
            "89" => ("Unknown weather condition", ""),
            "90" => ("Unknown weather condition", ""),
            "91" => ("Unknown weather condition", ""),
            "92" => ("Unknown weather condition", ""),
            "93" => ("Unknown weather condition", ""),
            "94" => ("Unknown weather condition", ""),
            "95" => ("BOOM! Thunderstorms", "https://openweathermap.org/img/wn/11d@2x.png"),
            "96" => ("Ouch! Thunderstorms with hail", "https://openweathermap.org/img/wn/11d@2x.png"),
            "97" => ("Unknown weather condition", ""),
            "98" => ("Unknown weather condition", ""),
            "99" => ("Hide... Thunderstorms with heavy hail", "https://openweathermap.org/img/wn/11d@2x.png"),
            _ => ("Unknown weather condition", "")
        };
    }
}

public class WeatherData
{
    public double Temperature { get; set; }
    public double Humidity { get; set; }
    public string? WeatherCondition { get; set; }
    public double WindSpeed { get; set; }
    public double MaxTemp { get; set; }
    public double MinTemp { get; set; }
    public double Precipitation { get; set; }
}