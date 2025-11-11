using Avalonia.Controls;
using Avalonia.Interactivity;
using Wywrota;

namespace WywrotaSongExtractor;

public partial class MainWindow : Window
{
    private WywrotaClient? _client;

    public MainWindow()
    {
        InitializeComponent();
        InitializeClient();
    }

    private async void InitializeClient()
    {
        string username = System.Environment.GetEnvironmentVariable("WYWROTA_USERNAME") ?? string.Empty;
        string password = System.Environment.GetEnvironmentVariable("WYWROTA_PASSWORD") ?? string.Empty;
        _client = await WywrotaClient.NewLoggedAs(username, password);
    }

    public async void BtnExtract_Click(object sender, RoutedEventArgs args)
    {
        string? url = UrlTextBox.Text;
        if (string.IsNullOrWhiteSpace(url)) { return; }
        if (_client == null) { return; }

        var song = await _client.FetchSong(url);
        if (song is not null)
        {
            LyricsTextBox.Text = song.Lyrics;
            ChordsTextBox.Text = song.Chords;
        }
    }
}