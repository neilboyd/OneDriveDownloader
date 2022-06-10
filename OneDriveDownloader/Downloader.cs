using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;

namespace OneDriveUtils;

public static class Downloader
{
    private static readonly HttpClient _httpClient = new();

    public static async Task<Stream> GetStreamAsync(string url, Action<string, string> setFilename)
    {
        var one = await OneAsync(url);

        var fileId = HttpUtility.ParseQueryString(new Uri(one).Query).Get("id");

        var two = await TwoAsync(one);

        var three = await ThreeAsync(fileId, two);

        var item = three.Items[0];
        setFilename(item.Name, item.Extension);

        var stream = await _httpClient.GetStreamAsync(item.Urls.Download);

        return stream;
    }

    private static readonly Regex OneRegex = new(@"<noscript>.*url=(.*?)""");
    private static async Task<string> OneAsync(string url)
    {
        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
        using var response = await _httpClient.SendAsync(requestMessage);
        var content = await response.Content.ReadAsStringAsync();
        var match = OneRegex.Match(content);
        var noScriptUrlEncoded = match.Groups[1].Captures[0].Value;
        var noScriptUrlDecoded = HttpUtility.HtmlDecode(noScriptUrlEncoded);
        return noScriptUrlDecoded;
    }


    private static readonly Regex TwoRegex = new(@"var FilesConfig=(.*?);");
    private static async Task<TwoResult> TwoAsync(string url)
    {
        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
        using var response = await _httpClient.SendAsync(requestMessage);
        var content = await response.Content.ReadAsStringAsync();
        var match = TwoRegex.Match(content);
        var config = match.Groups[1].Captures[0].Value;
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var two = JsonSerializer.Deserialize<TwoResult>(config, options);
        return two;
    }

    private static async Task<ThreeResult> ThreeAsync(string fileId, TwoResult two)
    {
        var url = $"{two.BaseApiUrl}GetItems?authKey={two.AuthKey}&id={fileId}";

        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
        requestMessage.Headers.Add("appid", two.AppId);
        requestMessage.Headers.Add("accept", "application/json");

        using var response = await _httpClient.SendAsync(requestMessage);
        var content = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var three = JsonSerializer.Deserialize<ThreeResult>(content, options);
        return three;
    }

    private class TwoResult
    {
        public string AppId { get; set; }
        public string AuthKey { get; set; }
        public string BaseApiUrl { get; set; }
    }

    private class ThreeResult
    {
        public ThreeItems[] Items { get; set; }
    }

    private class ThreeItems
    {
        public string Name { get; set; }
        public string Extension { get; set; }
        public ThreeUrls Urls { get; set; }
    }

    private class ThreeUrls
    {
        public string Download { get; set; }
    }
}
