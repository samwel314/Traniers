using ERP.Application.Common.Abstractions.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace ERP.Infrastructure.Services;

public sealed class ImageService : IImageService
{
    private readonly IWebHostEnvironment _environment;

    private static readonly string[] AllowedExtensions =
    [
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    ];

    private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB

    public ImageService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public bool IsValidImage(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return false;

        if (file.Length > MaxFileSize)
            return false;

        var extension = Path.GetExtension(file.FileName);

        if (string.IsNullOrWhiteSpace(extension))
            return false;

        return AllowedExtensions.Contains(
            extension,
            StringComparer.OrdinalIgnoreCase);
    }

    public async Task<string> SaveImageAsync(
        IFormFile file,
        string folderName)
    {
        if (!IsValidImage(file))
            throw new ArgumentException(
                "Invalid image file.",
                nameof(file));

        var uploadsFolder = Path.Combine(
            _environment.WebRootPath,
            folderName);

        Directory.CreateDirectory(uploadsFolder);

        var extension = Path.GetExtension(file.FileName);

        var fileName = $"{Guid.NewGuid()}{extension}";

        var filePath = Path.Combine(
            uploadsFolder,
            fileName);

        await using var stream = new FileStream(
            filePath,
            FileMode.Create);

        await file.CopyToAsync(stream);

        return Path.Combine(folderName, fileName)
            .Replace("\\", "/");
    }

    public void DeleteImage(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
            return;

        var fullPath = Path.Combine(
            _environment.WebRootPath,
            imagePath);

        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }
}