using Amazon.S3;
using Amazon.S3.Model;
using System.Text;

namespace FileMailApi.Services
{
    public class UploadFileService : IUploadFileService
    {
        private readonly string bucketName;
        private readonly IAmazonS3 s3Client;

        public UploadFileService(IConfiguration configuration, IAmazonS3 s3Client)
        {
            this.s3Client = s3Client;
            bucketName = configuration[ConfigurationVariableKeys.BucketName] ?? "uploaded-files";
        }

        public async Task<PutObjectResponse> UploadFileAsync(string email, IFormFile file)
        {
            using var stream = file.OpenReadStream();

            string EncodeToBase64(string value) =>
                Convert.ToBase64String(Encoding.UTF8.GetBytes(value));

            var request = new PutObjectRequest
            {
                BucketName = bucketName,
                Key = $"files/{Guid.NewGuid()}",
                ContentType = file.ContentType,
                InputStream = stream,
                Metadata =
                {
                    ["x-amz-meta-originalname"] = EncodeToBase64(file.FileName),
                    ["x-amz-meta-extension"] = EncodeToBase64(Path.GetExtension(file.FileName)),
                    ["x-amz-meta-email"] = EncodeToBase64(email),
                },
            };

            return await s3Client.PutObjectAsync(request);
        }
    }
}