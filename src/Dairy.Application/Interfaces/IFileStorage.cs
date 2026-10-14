namespace Dairy.Application.Interfaces;

public record StoredFile(string RelativePath, string OriginalFileName, string StoredFileName, long Size);

/// <summary>Local-disk storage under FileStorage:BasePath. Paths are always relative and traversal-safe.</summary>
public interface IFileStorage
{
    /// <summary>Saves the stream under <paramref name="folder"/> (e.g. "farmers/12") with a generated unique name.</summary>
    Task<StoredFile> SaveAsync(Stream content, string fileName, string folder, CancellationToken ct = default);

    /// <summary>Opens a stored file for reading, or null when missing.</summary>
    Stream? OpenRead(string relativePath);

    bool Exists(string relativePath);

    /// <summary>Deletes a stored file; returns false when it did not exist.</summary>
    bool Delete(string relativePath);
}
