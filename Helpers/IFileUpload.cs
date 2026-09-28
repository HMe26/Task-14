namespace CinemaApp.Helpers;

public interface IFileUpload
{
    string GenerateFileName(string fileName);
    string? GenerateFullPath(FileType fileType, string entityTypeFileName, string fileName);
    bool UploadFileLocally(string path, IFormFile file);
    bool DeleteFileLocally(string path);
}
