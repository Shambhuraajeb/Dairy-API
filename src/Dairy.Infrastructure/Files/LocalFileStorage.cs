using System.Text;
using Dairy.Application.Common;
using Dairy.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Dairy.Infrastructure.Files;

public class LocalFileStorage : IFileStorage
{
    private readonly string _root;
    private readonly StringComparison _pathComparison =
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    public LocalFileStorage(IConfiguration configuration, IHostEnvironment environment)
    {
        var basePath = configuration["FileStorage:BasePath"];
        if (string.IsNullOrWhiteSpace(basePath)) basePath = "uploads";

        _root = Path.GetFullPath(Path.IsPathRooted(basePath)
            ? basePath
            : Path.Combine(environment.ContentRootPath, basePath));
        Directory.CreateDirectory(_root);
    }

    public async Task<StoredFile> SaveAsync(Stream content, string fileName, string folder, CancellationToken ct = default)
    {
        var safeFolder = SanitizeFolder(folder);
        var safeName = SanitizeFileName(fileName);
        var extension = new string(Path.GetExtension(safeName).Where(c => char.IsLetterOrDigit(c) || c == '.').ToArray());
        if (extension.Length > 10) extension = string.Empty;

        var storedName = Guid.NewGuid().ToString("N") + extension;
        var relative = safeFolder.Length == 0 ? storedName : safeFolder + "/" + storedName;
        var fullPath = Resolve(relative);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await using (var fs = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
        {
            await content.CopyToAsync(fs, ct);
        }

        return new StoredFile(relative, safeName, storedName, new FileInfo(fullPath).Length);
    }

    public Stream? OpenRead(string relativePath)
    {
        var fullPath = Resolve(relativePath);
        return File.Exists(fullPath)
            ? new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true)
            : null;
    }

    public bool Exists(string relativePath) => File.Exists(Resolve(relativePath));

    public bool Delete(string relativePath)
    {
        var fullPath = Resolve(relativePath);
        if (!File.Exists(fullPath)) return false;
        File.Delete(fullPath);
        return true;
    }

    private string Resolve(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            throw new BusinessRuleException("Invalid file path.");

        var fullPath = Path.GetFullPath(Path.Combine(_root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        var rootWithSeparator = _root.EndsWith(Path.DirectorySeparatorChar) ? _root : _root + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(rootWithSeparator, _pathComparison))
            throw new BusinessRuleException("Invalid file path.");

        return fullPath;
    }

    private static string SanitizeFolder(string? folder)
    {
        if (string.IsNullOrWhiteSpace(folder)) return string.Empty;

        var segments = folder.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => new string(s.Where(c => char.IsAsciiLetterOrDigit(c) || c == '_' || c == '-').ToArray()))
            .Where(s => s.Length > 0);
        return string.Join('/', segments);
    }

    private static string SanitizeFileName(string? fileName)
    {
        var name = Path.GetFileName(fileName ?? string.Empty);
        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder();
        foreach (var c in name)
        {
            if (Array.IndexOf(invalid, c) < 0) sb.Append(c);
        }

        var cleaned = sb.ToString().Trim().TrimStart('.');
        if (cleaned.Length == 0) cleaned = "file";
        if (cleaned.Length > 100)
        {
            var ext = Path.GetExtension(cleaned);
            cleaned = cleaned.Substring(0, Math.Max(1, 100 - ext.Length)) + ext;
        }
        return cleaned;
    }
}
