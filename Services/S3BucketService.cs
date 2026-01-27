using Amazon.S3;
using Amazon.S3.Model;
using BuscaYa.Services.IServices;
using BuscaYa.Utils;
using SkiaSharp;
using System.Net;
using System.Text;
using System.Text.Json;

namespace BuscaYa.Services;

public class S3BucketService : IS3BucketService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    private readonly string _baseUrl;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string? _cloudflareZoneId;
    private readonly string? _cloudflareApiToken;

    public S3BucketService(
        IAmazonS3 s3Client,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        _s3Client = s3Client;
        var r2Settings = configuration.GetSection("R2");
        _bucketName = r2Settings["BucketName"] ?? throw new InvalidOperationException("R2 BucketName no configurado");
        var accountId = r2Settings["AccountId"] ?? throw new InvalidOperationException("R2 AccountId no configurado");
        _baseUrl = $"{accountId}.r2.cloudflarestorage.com";
        _httpClientFactory = httpClientFactory;
        _cloudflareZoneId = configuration["Cloudflare:ZoneId"];
        _cloudflareApiToken = configuration["Cloudflare:ApiToken"];
    }

    public async Task<string?> UploadImageToWebPAsync(string prefix, IFormFile image, string? previousImageUrl = null)
    {
        if (!IsValidImage(image)) return null;

        try
        {
            await DeleteFileIfExistsAsync(previousImageUrl);

            using var inputStream = image.OpenReadStream();
            using var original = await Task.Run(() => SKBitmap.Decode(inputStream));
            if (original == null) return null;

            using var imagePrepared = SKImage.FromBitmap(original);
            using var data = await Task.Run(() => imagePrepared.Encode(SKEncodedImageFormat.Webp, 40));

            var fileName = $"{Guid.NewGuid()}.webp";
            var filePath = $"{prefix}{fileName}";

            var request = new PutObjectRequest()
            {
                BucketName = _bucketName,
                Key = filePath,
                InputStream = data.AsStream(),
                ContentType = "image/webp",
                CannedACL = S3CannedACL.PublicRead,
                DisablePayloadSigning = true
            };

            await _s3Client.PutObjectAsync(request);

            return $"https://{_baseUrl}/{_bucketName}/{filePath}";
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<string?> UploadImageToJpgAsync(string prefix, IFormFile image, string? previousImageUrl = null)
    {
        if (!IsValidImage(image)) return null;

        try
        {
            await DeleteFileIfExistsAsync(previousImageUrl);

            using var inputStream = image.OpenReadStream();
            using var original = await Task.Run(() => SKBitmap.Decode(inputStream));
            if (original == null) return null;

            var info = new SKImageInfo(original.Width, original.Height, original.ColorType, SKAlphaType.Opaque);
            using var surface = SKSurface.Create(info);
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);
            canvas.DrawBitmap(original, 0, 0);
            using var skImage = surface.Snapshot();
            using var data = await Task.Run(() => skImage.Encode(SKEncodedImageFormat.Jpeg, 80));

            var fileName = $"{Guid.NewGuid()}.jpg";
            var filePath = $"{prefix}{fileName}";

            var request = new PutObjectRequest()
            {
                BucketName = _bucketName,
                Key = filePath,
                InputStream = data.AsStream(),
                ContentType = "image/jpeg",
                CannedACL = S3CannedACL.PublicRead,
                DisablePayloadSigning = true,
                Metadata =
                {
                    ["X-Original-Width"] = original.Width.ToString(),
                    ["X-Original-Height"] = original.Height.ToString()
                }
            };

            await _s3Client.PutObjectAsync(request);

            return $"https://{_baseUrl}/{_bucketName}/{filePath}";
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<string?> UploadImageAsync(string prefix, IFormFile image, string? previousImageUrl = null)
    {
        if (!IsValidImage(image)) return null;

        try
        {
            await DeleteFileIfExistsAsync(previousImageUrl);

            var extension = Path.GetExtension(image.FileName).ToLower();
            if (string.IsNullOrEmpty(extension)) extension = ".jpg";

            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = $"{prefix}{fileName}";

            var request = new PutObjectRequest()
            {
                BucketName = _bucketName,
                Key = filePath,
                InputStream = image.OpenReadStream(),
                ContentType = image.ContentType,
                CannedACL = S3CannedACL.PublicRead,
                DisablePayloadSigning = true,
                Metadata =
                {
                    ["X-Original-FileName"] = Path.GetFileName(image.FileName)
                }
            };

            await _s3Client.PutObjectAsync(request);

            return $"https://{_baseUrl}/{_bucketName}/{filePath}";
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<string?> UploadImageFromBase64ToJpgAsync(string prefix, string imageBase64, string? previousImageUrl = null)
    {
        if (!TryParseBase64Image(imageBase64, out var bytes, out var mime))
            return null;

        if (bytes.Length > 10 * 1024 * 1024) // 10MB max
            return null;

        try
        {
            await DeleteFileIfExistsAsync(previousImageUrl);

            using var stream = new MemoryStream(bytes);
            using var original = await Task.Run(() => SKBitmap.Decode(stream));
            if (original == null) return null;

            var info = new SKImageInfo(original.Width, original.Height, original.ColorType, SKAlphaType.Opaque);
            using var surface = SKSurface.Create(info);
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);
            canvas.DrawBitmap(original, 0, 0);
            using var skImage = surface.Snapshot();
            using var data = await Task.Run(() => skImage.Encode(SKEncodedImageFormat.Jpeg, 80));

            var fileName = $"{Guid.NewGuid()}.jpg";
            var filePath = $"{prefix}{fileName}";

            var request = new PutObjectRequest()
            {
                BucketName = _bucketName,
                Key = filePath,
                InputStream = data.AsStream(),
                ContentType = "image/jpeg",
                CannedACL = S3CannedACL.PublicRead,
                DisablePayloadSigning = true
            };

            await _s3Client.PutObjectAsync(request);

            return $"https://{_baseUrl}/{_bucketName}/{filePath}";
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<string?> UploadIconAsync(string prefix, IFormFile image, string? previousImageUrl = null, int size = 200)
    {
        if (!IsValidImage(image)) return null;

        try
        {
            await DeleteFileIfExistsAsync(previousImageUrl);

            using var inputStream = image.OpenReadStream();
            using var original = await Task.Run(() => SKBitmap.Decode(inputStream));
            if (original == null) return null;

            int minSide = Math.Min(original.Width, original.Height);
            int cropX = (original.Width - minSide) / 2;
            int cropY = (original.Height - minSide) / 2;

            using var square = new SKBitmap(minSide, minSide, original.ColorType, original.AlphaType);
            using (var canvas = new SKCanvas(square))
            {
                var srcRect = new SKRect(cropX, cropY, cropX + minSide, cropY + minSide);
                var destRect = new SKRect(0, 0, minSide, minSide);
                canvas.DrawBitmap(original, srcRect, destRect);
            }

            using var resized = new SKBitmap(size, size, square.ColorType, square.AlphaType);
            var sampling = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None);
            square.ScalePixels(resized, sampling);

            using var imagePrepared = SKImage.FromBitmap(resized);
            using var data = await Task.Run(() => imagePrepared.Encode(SKEncodedImageFormat.Webp, 65));

            var fileName = $"{Guid.NewGuid()}_icon_{size}.webp";
            var filePath = $"{prefix}{fileName}";

            var request = new PutObjectRequest()
            {
                BucketName = _bucketName,
                Key = filePath,
                InputStream = data.AsStream(),
                ContentType = "image/webp",
                CannedACL = S3CannedACL.PublicRead,
                DisablePayloadSigning = true,
                Metadata =
                {
                    ["X-Original-Width"] = original.Width.ToString(),
                    ["X-Original-Height"] = original.Height.ToString(),
                    ["X-Icon-Size"] = size.ToString()
                }
            };

            await _s3Client.PutObjectAsync(request);

            return $"https://{_baseUrl}/{_bucketName}/{filePath}";
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task DeleteFileIfExistsAsync(string? fileUrl)
    {
        if (string.IsNullOrWhiteSpace(fileUrl)) return;

        var expectedPrefix = $"https://{_baseUrl}/{_bucketName}/";
        if (fileUrl.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var previousImageKey = fileUrl.Substring(expectedPrefix.Length);
            try
            {
                await _s3Client.DeleteObjectAsync(_bucketName, previousImageKey);
                await PurgeCloudflareCacheAsync(fileUrl);
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                // El archivo no existe, no hay problema
            }
        }
    }

    public async Task<List<string>> ListFoldersAsync(string? prefix = null)
    {
        var folders = new HashSet<string>();
        var request = new ListObjectsV2Request
        {
            BucketName = _bucketName,
            Prefix = prefix,
            Delimiter = "/"
        };

        ListObjectsV2Response response;
        do
        {
            response = await _s3Client.ListObjectsV2Async(request);
            foreach (var commonPrefix in response.CommonPrefixes)
            {
                var folderName = commonPrefix.Replace(prefix ?? "", "").TrimEnd('/');
                if (!string.IsNullOrEmpty(folderName))
                    folders.Add(folderName);
            }
            request.ContinuationToken = response.NextContinuationToken;
        } while (response.IsTruncated);

        return folders.ToList();
    }

    public async Task<List<string>> ListFilesAsync(string? prefix = null, bool recursive = false)
    {
        var files = new List<string>();
        var request = new ListObjectsV2Request
        {
            BucketName = _bucketName,
            Prefix = prefix
        };

        if (!recursive)
            request.Delimiter = "/";

        ListObjectsV2Response response;
        do
        {
            response = await _s3Client.ListObjectsV2Async(request);
            files.AddRange(response.S3Objects.Select(o => o.Key));
            request.ContinuationToken = response.NextContinuationToken;
        } while (response.IsTruncated);

        return files;
    }

    public bool IsValidImage(IFormFile image)
    {
        var permittedContentTypes = new List<string> { "image/jpg", "image/jpeg", "image/png", "image/gif", "image/webp" };
        var permittedExtensions = new List<string> { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

        if (!permittedContentTypes.Contains(image.ContentType.ToLower()))
            return false;

        var extension = Path.GetExtension(image.FileName).ToLower();
        return permittedExtensions.Contains(extension);
    }

    private bool TryParseBase64Image(string input, out byte[] bytes, out string mime)
    {
        bytes = Array.Empty<byte>();
        mime = "image/jpeg";

        if (string.IsNullOrWhiteSpace(input))
            return false;

        var base64String = input;
        if (input.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
        {
            var parts = input.Split(',');
            if (parts.Length != 2) return false;
            base64String = parts[1];
            var mimePart = parts[0].Split(';')[0];
            mime = mimePart.Replace("data:", "");
        }

        try
        {
            bytes = Convert.FromBase64String(base64String);
            if (bytes.Length == 0) return false;

            // Validar magic numbers
            if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xD8)
                mime = "image/jpeg";
            else if (bytes.Length >= 8 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
                mime = "image/png";
            else if (bytes.Length >= 6 && bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46)
                mime = "image/gif";

            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task PurgeCloudflareCacheAsync(string fileUrl)
    {
        if (string.IsNullOrWhiteSpace(_cloudflareApiToken) || string.IsNullOrWhiteSpace(_cloudflareZoneId))
            return;

        try
        {
            var client = _httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"https://api.cloudflare.com/client/v4/zones/{_cloudflareZoneId}/purge_cache");

            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _cloudflareApiToken);

            var payload = new { files = new[] { fileUrl } };
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            await client.SendAsync(request);
        }
        catch
        {
            // Silencioso, no bloquea si falla
        }
    }
}
