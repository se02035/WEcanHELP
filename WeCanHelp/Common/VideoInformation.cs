using System;

namespace Common
{
    public class VideoInformation
    {
        public Guid Id { get; set; }
        public Uri Uri { get; set; }
        public string Name => Uri.Segments[Uri.Segments.Length - 1];
    }
}