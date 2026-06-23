using Amazon.S3;
using Amazon.S3.Model;

namespace SiskyApi.Services;

public class StorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    private readonly string _publicUrl;

    public StorageService(IConfiguration configuration)
    {
        var accountId = configuration["Storage:AccountId"]!;
        var accessKeyId = configuration["Storage:AccessKeyId"]!;
        var secretAccessKey = configuration["Storage:SecretAccessKey"]!;
        _bucketName = configuration["Storage:BucketName"]!;
        _publicUrl = configuration["Storage:PublicUrl"]!;

        var config = new AmazonS3Config
        {
            ServiceURL = $"https://{accountId}.r2.cloudflarestorage.com",
            ForcePathStyle = true
        };

        _s3Client = new AmazonS3Client(accessKeyId, secretAccessKey, config);
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType)
    {
        var key = $"avatars/{Guid.NewGuid()}-{fileName}";

        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = fileStream,
            ContentType = contentType,
            DisablePayloadSigning = true
        };

        await _s3Client.PutObjectAsync(request);

        return $"{_publicUrl}/{key}";
    }

    public async Task DeleteAsync(string fileUrl)
    {
        if (string.IsNullOrEmpty(fileUrl)) return;

        var key = fileUrl.Replace($"{_publicUrl}/", "");

        var request = new DeleteObjectRequest
        {
            BucketName = _bucketName,
            Key = key
        };

        await _s3Client.DeleteObjectAsync(request);
    }
}