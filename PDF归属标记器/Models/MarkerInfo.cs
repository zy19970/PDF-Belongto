namespace PdfOwnershipMarker.Models
{
    internal sealed class MarkerInfo
    {
        public string Owner { get; set; }
        public string ProvenanceId { get; set; }
        public string MarkedAt { get; set; }
        public string OriginalSha256 { get; set; }
        public string Version { get; set; }
    }
}
