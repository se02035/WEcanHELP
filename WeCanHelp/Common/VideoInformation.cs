using System;

namespace Common
{
    public class VideoInformation
    {
        public int AssetId { get; set; }
        public Uri Uri { get; set; }
        public string AssetName { get; set; }
        public string Name => Uri.Segments[Uri.Segments.Length - 1];
    }
}