namespace CinemaApp.Helpers;

public enum FileType
{
    Img
}

public class FileUpload : IFileUpload
{
    public string GenerateFileName(string fileName)
    {
        return $"{Guid.NewGuid().ToString()}-{DateTime.Now.ToString("dd-MM-yyyy")}{Path.GetExtension(fileName)}";
    }

    public string? GenerateFullPath(FileType fileType, string entityTypeFileName, string fileName)
    {
        switch (fileType)
        {
            case FileType.Img:
                {
                    string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "imgs", entityTypeFileName, fileName);
                    return filePath;
                }
        }

        return null;
    }

    public bool UploadFileLocally(string path, IFormFile file)
    {
        using (var stream = System.IO.File.Create(path))
        {
            file.CopyTo(stream);
            return true;
        }
    }

    public bool DeleteFileLocally(string path)
    {
        if (System.IO.File.Exists(path))
        {
            System.IO.File.Delete(path);
            return true;
        }

        return false;
    }
}
