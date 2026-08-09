using System.Security.Cryptography;
using System.Text;

namespace CueComplete.Core;

public class TrackOffset
{
    public int TrackNumber { get; set; }
    public int Minutes { get; set; }
    public int Seconds { get; set; }
    public int Frames { get; set; }

    public int TotalSectors => (Minutes * 60 + Seconds) * 75 + Frames + 150;
    public int TotalSeconds => Minutes * 60 + Seconds;
}

public static class DiscIdCalculator
{
    public static string CalculateMusicBrainzDiscId(List<TrackOffset> tracks, int leadOutSectors)
    {
        if (tracks == null || tracks.Count == 0) return string.Empty;

        int firstTrack = tracks[0].TrackNumber;
        int lastTrack = tracks[^1].TrackNumber;

        var sb = new StringBuilder();
        sb.Append($"{firstTrack:X2}");
        sb.Append($"{lastTrack:X2}");
        sb.Append($"{leadOutSectors:X8}");

        for (int i = 0; i < 99; i++)
        {
            if (i < tracks.Count)
            {
                sb.Append($"{tracks[i].TotalSectors:X8}");
            }
            else
            {
                sb.Append("00000000");
            }
        }

        byte[] asciiBytes = Encoding.ASCII.GetBytes(sb.ToString());
        byte[] hash = SHA1.HashData(asciiBytes);

        string base64 = Convert.ToBase64String(hash);
        return base64.Replace('+', '.').Replace('/', '_').Replace('=', '-');
    }

    public static string CalculateFreeDbId(List<TrackOffset> tracks, int leadOutSeconds)
    {
        if (tracks == null || tracks.Count == 0) return string.Empty;

        int trackCount = tracks.Count;
        int n = 0;

        foreach (var t in tracks)
        {
            int sec = t.TotalSeconds + 2;
            n += SumDigits(sec);
        }

        int firstTrackSec = tracks[0].TotalSeconds + 2;
        int totalSeconds = leadOutSeconds - firstTrackSec;

        uint discId = ((uint)(n % 0xFF) << 24) | ((uint)totalSeconds << 8) | (uint)trackCount;
        return discId.ToString("x8");
    }

    private static int SumDigits(int n)
    {
        int sum = 0;
        while (n > 0)
        {
            sum += n % 10;
            n /= 10;
        }
        return sum;
    }
}
