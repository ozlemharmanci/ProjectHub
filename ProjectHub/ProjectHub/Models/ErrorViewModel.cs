using System.Diagnostics;

namespace ProjectHub.Models
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
        public string? ErrorDetails { get; set; }
        public bool ShowDetails => !string.IsNullOrEmpty(ErrorDetails);
    }
}