using Amazon.S3.Model;

namespace FileMailApi.Services
{
    public interface IUploadFileService
    {
        public Task<PutObjectResponse> UploadFileAsync(string email, IFormFile file);
    }
}