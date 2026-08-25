using ERP.Application.Common.Abstractions.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace ERP.Infrastructure.Services;

public sealed class ImageService : IImageService
{
    private readonly IWebHostEnvironment _environment;

    private static readonly Dictionary<string, byte[][]> ImageSignatures =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] =
            [
                [0xFF, 0xD8, 0xFF]
            ],

            [".jpeg"] =
            [
                [0xFF, 0xD8, 0xFF]
            ],

            [".png"] =
            [
                [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]
            ],

            [".gif"] =
            [
                [0x47, 0x49, 0x46, 0x38]
            ],

            [".webp"] =
            [
                [0x52, 0x49, 0x46, 0x46]
            ]
        };

    public async Task<bool> IsValidImageAsync(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return false;

        if (file.Length > MaxFileSize)
            return false;

        var extension = Path.GetExtension(file.FileName);

        if (string.IsNullOrWhiteSpace(extension))
            return false;

        if (!ImageSignatures.TryGetValue(extension, out var signatures))
            return false;

        await using var stream = file.OpenReadStream();

        foreach (var signature in signatures)
        {
            stream.Position = 0;

            var header = new byte[signature.Length];

            var bytesRead = await stream.ReadAsync(header);

            if (bytesRead != signature.Length)
                continue;

            if (header.SequenceEqual(signature))
                return true;
        }

        return false;
    }
    private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB

    public ImageService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }



    public async Task<string> SaveImageAsync(
        IFormFile file,
        string folderName)
    {
        //if (!await IsValidImageAsync(file))
        //    throw new ArgumentException(
        //        "Invalid image file.",
        //        nameof(file));

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
