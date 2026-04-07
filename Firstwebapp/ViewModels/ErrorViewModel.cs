namespace Thekdar.Models
{
    public class ErrorViewModel
    {
        public string RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
        
        // For detailed error display
        public Exception? Exception { get; set; }
        public string? Path { get; set; }
        
        public bool ShowDetailedError => Exception != null && 
                                         (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development");
    }
}