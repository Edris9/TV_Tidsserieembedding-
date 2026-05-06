using System.Globalization;
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

    /// <summary>
    /// Hämtar vädermätstationer. <paramref name="stationLimit"/> är QUERY <c>limit</c>.
    /// Om <paramref name="sampleFrom"/> och <paramref name="sampleTo"/> anges filtreras
    /// <c>Observation.Sample</c> med GTE/LTE (som i Trafikverkets exempel).
    /// </summary>
    public async Task<string> GetWeatherDataAsync(
        int stationLimit = 2000,
        DateTime? sampleFrom = null,
        DateTime? sampleTo = null)
    {
        var limit = Math.Clamp(stationLimit, 1, 2000);
        string filterInner;
        if (sampleFrom is { } from && sampleTo is { } to)
        {
            var fromS = from.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);
            var toS = to.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);
            filterInner = $@"
            <FILTER>
              <GTE name=""Observation.Sample"" value=""{fromS}"" />
              <LTE name=""Observation.Sample"" value=""{toS}"" />
            </FILTER>";
        }
        else
        {
            filterInner = "<FILTER></FILTER>";
        }

        var body = $@"<REQUEST>
          <LOGIN authenticationkey=""{ApiKey}""/>
          <QUERY objecttype=""WeatherMeasurepoint"" namespace=""road.weatherinfo"" schemaversion=""2.1"" limit=""{limit}"">
            {filterInner}
          </QUERY>
        </REQUEST>";

        var content = new StringContent(body, Encoding.UTF8, "application/xml");
        var response = await _http.PostAsync(ApiUrl, content);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
}
