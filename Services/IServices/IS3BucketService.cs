namespace BuscaYa.Services.IServices;

public interface IS3BucketService
{
    Task<string?> UploadImageToWebPAsync(string prefix, IFormFile image, string? previousImageUrl = null);
    Task<string?> UploadImageToJpgAsync(string prefix, IFormFile image, string? previousImageUrl = null);
    Task<string?> UploadImageAsync(string prefix, IFormFile image, string? previousImageUrl = null);
    Task<string?> UploadImageFromBase64ToJpgAsync(string prefix, string imageBase64, string? previousImageUrl = null);
    Task<string?> UploadIconAsync(string prefix, IFormFile image, string? previousImageUrl = null, int size = 200);
    Task DeleteFileIfExistsAsync(string? fileUrl);
    Task<List<string>> ListFoldersAsync(string? prefix = null);
    Task<List<string>> ListFilesAsync(string? prefix = null, bool recursive = false);
    bool IsValidImage(IFormFile image);
}
