using System;
using System.Data;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using HtmlAgilityPack;

namespace Wywrota;

public class WywrotaClient : IDisposable
{
    private readonly string _loginUrl = "https://moja.wywrota.pl/login";
    private readonly string _tokenUrl = "https://www.wywrota.pl/login";
    private readonly int _loginTimeout = 3000;
    private HttpClient _client = new();
    private string? _token = null;

    public void Dispose()
    {
        _client.Dispose();
        GC.SuppressFinalize(this);
    }

    ~WywrotaClient() => Dispose();

    public static async Task<WywrotaClient> NewLoggedAs(string username, string password)
    {
        WywrotaClient client = new();
        await client.Login(username, password);
        return client;
    }

    public async Task<bool> Login(string username, string password)
    {
        _token = await RetrieveToken();
        if (_token is null)
        {
            return false;
        }

        CancellationTokenSource cts = new();
        cts.CancelAfter(_loginTimeout);
        CancellationToken ctn = cts.Token;
        Dictionary<string, string> body = new()
        {
            { "username", username },
            { "password", password },
            { "_token", _token },
            { "remember", "0" },
        };

        HttpResponseMessage rsp = await _client.PostAsJsonAsync(_loginUrl, body, ctn);
        return await IsLoggedIn(rsp);
    }

    public async Task<string?> RetrieveToken()
    {
        string content = await _client.GetStringAsync(_tokenUrl);
        HtmlDocument doc = new();
        doc.LoadHtml(content);
        HtmlNode? tokenInput = doc.DocumentNode.SelectSingleNode("""//input[@name="_token"]""");
        if (tokenInput is not null)
        {
            return tokenInput.Attributes["value"].Value;
        }
        else
        {
            return null;
        }
    }

    private async Task<bool> IsLoggedIn(HttpResponseMessage rsp)
    {
        if (!rsp.IsSuccessStatusCode) return false;
        string content = await rsp.Content.ReadAsStringAsync();

        int startIndex = content.IndexOf("user_logged_in");
        if (startIndex == 0)
        {
            return false;
        }

        startIndex = content.IndexOf(':', startIndex);
        if (startIndex == 0)
        {
            return false;
        }

        int endIndex = content.IndexOf(',', startIndex);
        if (endIndex == 0)
        {
            return false;
        }

        string section = content.Substring(startIndex, endIndex - startIndex);
        char? value = section.Where(char.IsAsciiDigit).First();
        if (value is not null)
        {
            return value == '1';
        }
        else
        {
            return false;
        }
    }

    public async Task<Song?> FetchSong(string songUrl)
    {
        HttpResponseMessage rsp = await _client.GetAsync(songUrl);
        if (!rsp.IsSuccessStatusCode)
        {
            return null;
        }

        HtmlDocument doc = new();
        doc.LoadHtml(await rsp.Content.ReadAsStringAsync());

        string lyrics = RetrieveLyrics(doc.DocumentNode);
        string chords = RetrieveChords(doc.DocumentNode);
        if (lyrics.Length == 0 || chords.Length == 0)
        {
            return null;
        }
        else
        {
            return new(lyrics, chords);
        }
    }

    private string RetrieveLyrics(HtmlNode root)
    {
        StringBuilder builder = new();
        var allNodes = root.SelectNodes("""//div[@class="interpretation-content"]//node()""");
        if (allNodes is null)
        {
            return "";
        }

        int brCount = 0;
        foreach (var node in allNodes)
        {
            if (node is HtmlTextNode)
            {
                brCount = 0;
                if (node.ParentNode.Name != "code")
                {
                    string lyricsFragment = node.OuterHtml.Trim('\n').Replace("&nbsp;", "");
                    if (lyricsFragment.Length > 0)
                    {
                        builder.Append(lyricsFragment);
                    }
                }
            }
            else if (node.OuterHtml.ToLower() == "<br>")
            {
                if (++brCount < 2)
                {
                    builder.Append("\n");
                }
            }
        }

        return builder.ToString().Trim('\n');
    }

    private string RetrieveChords(HtmlNode root)
    {
        StringBuilder builder = new();
        var allNodes = root.SelectNodes("""//div[@class="interpretation-content"]//node()""");
        if (allNodes is null)
        {
            return "";
        }

        int brCount = 0;
        foreach (var node in allNodes)
        {
            if (node is HtmlTextNode)
            {
                brCount = 0;
                if (node.ParentNode.Name == "code")
                {
                    string chordsFragment = node.OuterHtml.Replace("&nbsp;", "");
                    if (chordsFragment.Length > 0)
                    {
                        builder.Append($"{chordsFragment} ");
                    }
                }
            }
            else if (node.OuterHtml.ToLower() == "<br>")
            {
                if (++brCount < 2)
                {
                    builder.Append("\n");
                }
            }
        }

        return builder.ToString().Trim('\n');
    }
}