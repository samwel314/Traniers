using Microsoft.AspNetCore.Http;

namespace ERP.Application.Common.Abstractions.Services;

public interface IImageService
{
    Task<bool> IsValidImageAsync(IFormFile file);

    Task<string> SaveImageAsync(
        IFormFile file,
        string folderName);

    void DeleteImage(string? imagePath);
}
