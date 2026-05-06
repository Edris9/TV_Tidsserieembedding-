using System.Text;

namespace TvTidsserieembedding.Infrastructure.TrafikverketApi;

public class TrafikverketClient
{
    private readonly HttpClient _http;
    private const string ApiUrl = "https://api.trafikinfo.trafikverket.se/v2/data.json";
    private const string ApiKey = "demokey";

    public TrafikverketClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<string> GetWeatherDataAsync()
    {
        var body = $@"<REQUEST>
          <LOGIN authenticationkey=""{ApiKey}""/>
          <QUERY objecttype=""WeatherMeasurepoint"" namespace=""road.weatherinfo"" schemaversion=""2.1"" limit=""10"">
            <FILTER></FILTER>
          </QUERY>
        </REQUEST>";

        var content = new StringContent(body, Encoding.UTF8, "application/xml");
        var response = await _http.PostAsync(ApiUrl, content);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
}
