using Microsoft.AspNetCore.Http;

namespace ERP.Application.Common.Abstractions.Services;

public interface IImageService
{
    bool IsValidImage(IFormFile file);

    Task<string> SaveImageAsync(
        IFormFile file,
        string folderName);

    void DeleteImage(string? imagePath);
}