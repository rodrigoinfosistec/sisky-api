using Amazon.S3;
using Amazon.S3.Model;
using SiskyApi.Constants;

namespace SiskyApi.Services;

public class StorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    private readonly string _publicUrl;

    public StorageService(IAmazonS3 s3Client, IConfiguration configuration)
    {
        _s3Client = s3Client;
        _bucketName = configuration["Storage:BucketName"]!;
        _publicUrl = configuration["Storage:PublicUrl"]!;
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, string folder = StorageFolders.Avatars)
    {
        var key = $"{folder}/{Guid.NewGuid()}-{fileName}";

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