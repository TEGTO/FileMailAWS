using FileMailApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace FileMailApi.Endpoints.File
{
    [ApiController]
    [Route("file")]
    public class FileController : ControllerBase
    {
        private readonly long maxFileSize;
        private readonly IUploadFileService uploadFileService;

        public FileController(IConfiguration configuration, IUploadFileService uploadFileService)
        {
            maxFileSize = long.TryParse(configuration[ConfigurationVariableKeys.MaxFileSize], out var result) ? result : 1 * 1024 * 1024;
            this.uploadFileService = uploadFileService;
        }

        [HttpPost]
        public async Task<IActionResult> UploadFileAsync(FileSendRequest request, CancellationToken cancellationToken)
        {
            if (request.File.Length > maxFileSize)
            {
                return BadRequest($"File size must be {maxFileSize / (1024 * 1024)}MB or less.");
            }

            var response = await uploadFileService.UploadFileAsync(request.Email, request.File);

            if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new InvalidOperationException("Failed to upload file!");
            }

            return Ok();
        }
    }
}
