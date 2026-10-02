namespace PdfOwnershipMarker.Models
{
    internal sealed class InstallProgress
    {
        public int Percentage { get; set; }
        public long BytesReceived { get; set; }
        public long TotalBytes { get; set; }
        public string Stage { get; set; }
        public bool IsIndeterminate { get; set; }
    }
}
