using System;
using System.IO;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    // Existing OST IDs stay unchanged; local IDs remain compatible with stored libraries.
    public static class MusicSourceId
    {
        public static string Local(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName) || Path.GetFileName(fileName) != fileName)
                throw new ArgumentException("A local track ID requires a file name", nameof(fileName));
            return "local:" + fileName.ToLowerInvariant();
        }

        public static string Online(string provider, string trackId)
        {
            if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(trackId) ||
                provider.IndexOf(':') >= 0 || trackId.IndexOf(':') >= 0)
                throw new ArgumentException("An online track ID requires a provider and track ID without colons");
            return "cloud:" + provider.Trim().ToLowerInvariant() + ":" + trackId.Trim();
        }
    }
}
