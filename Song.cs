using System.Text;

namespace Wywrota;

public record Song(string Lyrics, string Chords)
{
    public override string ToString()
    {
        StringBuilder builder = new();
        builder.Append("===");
        builder.Append(Lyrics);
        builder.Append("===");
        builder.Append(Chords);
        builder.Append("===");
        return builder.ToString();
    }
}