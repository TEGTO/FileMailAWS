using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using System.Text;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace S3FileMonitorLambda;
public class Function
{
    private readonly IAmazonS3 s3Client;
    private readonly IAmazonSimpleEmailService sesClient;
    private readonly string senderEmail;

    public Function() : this(new AmazonS3Client(), new AmazonSimpleEmailServiceClient()) { }
    public Function(IAmazonS3 s3Client, IAmazonSimpleEmailService sesClient)
    {
        this.s3Client = s3Client;
        this.sesClient = sesClient;
        senderEmail = Environment.GetEnvironmentVariable("SENDER_EMAIL") ?? throw new InvalidLambdaFunctionException("Sender Email is not specified!");
    }

    public async Task FunctionHandler(S3Event s3Event, ILambdaContext context)
    {
        foreach (var record in s3Event.Records.Select(x => x.S3))
        {
            var bucketName = record.Bucket.Name;
            var objectKey = record.Object.Key;

            try
            {
                var response = await s3Client.GetObjectAsync(new GetObjectRequest
                {
                    BucketName = bucketName,
                    Key = objectKey
                });

                if (!response.Metadata.Keys.Contains("x-amz-meta-email"))
                {
                    context.Logger.LogError($"No email metadata found for {objectKey}");
                    continue;
                }

                string DecodeBase64(string encodedValue) =>
                    Encoding.UTF8.GetString(Convert.FromBase64String(encodedValue));

                var encodedEmail = response.Metadata["x-amz-meta-email"];
                var recipientEmail = DecodeBase64(encodedEmail);

                context.Logger.LogInformation($"Sending file {objectKey} to {recipientEmail}");

                var encodedFileNameWithExtension = response.Metadata["x-amz-meta-originalname"];
                var fileNameWithExtension = DecodeBase64(encodedFileNameWithExtension);

                await SendEmailWithAttachmentAsync(recipientEmail, fileNameWithExtension, response.ResponseStream);
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"Error processing file {objectKey}: {ex.Message}");
            }
        }
    }

    private async Task SendEmailWithAttachmentAsync(string recipientEmail, string fileName, Stream fileStream)
    {
        // Read the file into memory
        using var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream);
        byte[] fileBytes = memoryStream.ToArray();
        string base64File = Convert.ToBase64String(fileBytes);

        // Construct MIME email with attachment
        var boundary = Guid.NewGuid().ToString();
        var rawMessage = new StringBuilder();

        rawMessage.AppendLine($"From: {senderEmail}");
        rawMessage.AppendLine($"To: {recipientEmail}");
        rawMessage.AppendLine("Subject: New File Received");
        rawMessage.AppendLine("MIME-Version: 1.0");
        rawMessage.AppendLine($"Content-Type: multipart/mixed; boundary=\"{boundary}\"");
        rawMessage.AppendLine();

        // Email Body
        rawMessage.AppendLine($"--{boundary}");
        rawMessage.AppendLine("Content-Type: text/plain; charset=UTF-8");
        rawMessage.AppendLine();
        rawMessage.AppendLine($"A new file '{fileName}' has been uploaded.");
        rawMessage.AppendLine("Please find the attached file.");
        rawMessage.AppendLine();

        // Attachment
        rawMessage.AppendLine($"--{boundary}");
        rawMessage.AppendLine($"Content-Type: application/octet-stream; name=\"{fileName}\"");
        rawMessage.AppendLine("Content-Transfer-Encoding: base64");
        rawMessage.AppendLine($"Content-Disposition: attachment; filename=\"{fileName}\"");
        rawMessage.AppendLine();
        rawMessage.AppendLine(base64File);
        rawMessage.AppendLine($"--{boundary}--");

        // Convert MIME message to byte array
        var rawMessageBytes = Encoding.UTF8.GetBytes(rawMessage.ToString());

        // Send Email using SendRawEmail API
        var sendRequest = new SendRawEmailRequest
        {
            Source = senderEmail,
            Destinations = { recipientEmail },
            RawMessage = new RawMessage { Data = new MemoryStream(rawMessageBytes) }
        };

        await sesClient.SendRawEmailAsync(sendRequest);
    }
}