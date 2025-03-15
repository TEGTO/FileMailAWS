namespace FileMailApi.Endpoints.File
{
    public class FileSendRequest
    {
        public string Email { get; set; } = default!;
        public IFormFile File { get; set; } = default!;
    }
}
