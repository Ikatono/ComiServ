using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.AspNetCore.StaticFiles;
using SharpCompress.Archives;
using SharpCompress.Archives.Rar;
using SharpCompress.Archives.SevenZip;
using System.Diagnostics;
using System.IO.Compression;
using System.IO.Hashing;
using System.Linq;

namespace ComiServ.Background;

public record class ComicAnalysis
(
    long FileSizeBytes,
    int PageCount,
    Int64 Xxhash
);
public record class ComicPage
(
    string Filename,
    string Mime,
    byte[] Data
);
public interface IComicAnalyzer
{
    public static readonly IReadOnlyList<string> ZIP_EXTS =  [".cbz", ".zip"];
    public static readonly IReadOnlyList<string> RAR_EXTS =  [".cbr", ".rar"];
    public static readonly IReadOnlyList<string> ZIP7_EXTS = [".cb7", ".7z"];
    public bool ComicFileExists(string filename);
    public void DeleteComicFile(string filename);
    //returns null on invalid filetype, throws on analysis error
    public ComicAnalysis? AnalyzeComic(string filename);
    public Task<ComicAnalysis?> AnalyzeComicAsync(string filename);
    //returns null if out of range, throws for file error
    public ComicPage? GetComicPage(string filepath, int page);
    public Task<ComicPage?> GetComicPageAsync(string filepath, int page);
    //based purely on filename, doesn't try to open file
    //returns null for ALL UNRECOGNIZED OR NON-IMAGES
    public static string? GetImageMime(string filename)
    {
        if (new FileExtensionContentTypeProvider().TryGetContentType(filename, out string? _mime))
        {
            if (_mime?.StartsWith("image") ?? false)
                return _mime;
        }
        return null;
    }
}
//async methods actually just block
public class SynchronousComicAnalyzer(ILogger<IComicAnalyzer>? logger)
    : IComicAnalyzer
{
    private readonly ILogger<IComicAnalyzer>? _logger = logger;
    public bool ComicFileExists(string filename)
    {
        return File.Exists(filename);
    }
    public void DeleteComicFile(string filename)
    {
        try
        {
            File.Delete(filename);
        }
        catch (DirectoryNotFoundException)
        {
            return;
        }
    }
    public ComicAnalysis? AnalyzeComic(string filepath)
    {
        _logger?.LogTrace($"Analyzing comic: {filepath}");
        var ext = new FileInfo(filepath).Extension.ToLower();
        if (IComicAnalyzer.ZIP_EXTS.Contains(ext))
            return ZipAnalyze(filepath);
        else if (IComicAnalyzer.RAR_EXTS.Contains(ext))
            return RarAnalyze(filepath);
        else if (IComicAnalyzer.ZIP7_EXTS.Contains(ext))
            return Zip7Analyze(filepath);
        else
            //throw new ArgumentException("Cannot analyze this file type");
            return null;
    }
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public async Task<ComicAnalysis?> AnalyzeComicAsync(string filename)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        return AnalyzeComic(filename);
    }
    protected ComicAnalysis ZipAnalyze(string filepath)
    {
        var filedata = File.ReadAllBytes(filepath);
        var hash = ComputeHash(filedata);
        using var stream = new MemoryStream(filedata);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, false);
        return new
        (
            FileSizeBytes: filedata.LongLength,
            PageCount: archive.Entries.Count,
            Xxhash: hash
        );
    }
    protected ComicAnalysis RarAnalyze(string filepath)
    {
        var filedata = File.ReadAllBytes(filepath);
        var hash = ComputeHash(filedata);
        using var stream = new MemoryStream(filedata);
        using var rar = RarArchive.Open(stream, new SharpCompress.Readers.ReaderOptions()
        {
            LeaveStreamOpen = false
        });
        return new
        (
            FileSizeBytes: filedata.LongLength,
            PageCount: rar.Entries.Count,
            Xxhash: hash
        );
    }
    protected ComicAnalysis Zip7Analyze(string filepath)
    {
        var filedata = File.ReadAllBytes(filepath);
        var hash = ComputeHash(filedata);
        using var stream = new MemoryStream(filedata);
        using var zip7 = SevenZipArchive.Open(stream, new SharpCompress.Readers.ReaderOptions()
        {
            LeaveStreamOpen = false
        });
        return new
        (
            FileSizeBytes: filedata.LongLength,
            PageCount: zip7.Entries.Count,
            Xxhash: hash
        );
    }
    protected static Int64 ComputeHash(ReadOnlySpan<byte> data)
        => unchecked((Int64)XxHash64.HashToUInt64(data));

    public ComicPage? GetComicPage(string filepath, int page)
    {
        var fi = new FileInfo(filepath);
        var ext = fi.Extension;
        if (IComicAnalyzer.ZIP_EXTS.Contains(ext))
            return GetPageZip(filepath, page);
        else if (IComicAnalyzer.RAR_EXTS.Contains(ext))
            return GetPageRar(filepath, page);
        else if (IComicAnalyzer.ZIP7_EXTS.Contains(ext))
            return GetPage7Zip(filepath, page);
        else return null;
    }
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public async Task<ComicPage?> GetComicPageAsync(string filepath, int page)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        return GetComicPage(filepath, page);
    }
    protected ComicPage? GetPageZip(string filepath, int page)
    {
        Debug.Assert(page >= 1, "Page number must be positive");
        try
        {
            using var fileStream = new FileStream(filepath, FileMode.Open);
            using var arc = new ZipArchive(fileStream, ZipArchiveMode.Read, false);
            (var entry, var mime) = arc.Entries
                .Select((ZipArchiveEntry e) => (e, IComicAnalyzer.GetImageMime(e.Name)))
                .Where(static pair => pair.Item2 is not null)
                .OrderBy(static pair => pair.Item1.FullName)
                .Skip(page - 1)
                .FirstOrDefault();
            if (entry is null || mime is null)
                return null;
            using var pageStream = entry.Open();
            using var pageStream2 = new MemoryStream();
            pageStream.CopyTo(pageStream2);
            pageStream2.Seek(0, SeekOrigin.Begin);
            var pageData = pageStream2.ToArray();
            return new
            (
                Filename: entry.Name,
                Mime: mime,
                Data: pageData
            );
        }
        catch (FileNotFoundException)
        {
            return null;
        }
        catch (DirectoryNotFoundException)
        {
            return null;
        }
    }
    protected ComicPage? GetPageRar(string filepath, int page)
    {
        using var rar = RarArchive.Open(filepath);
        (var entry, var mime) = rar.Entries
                .Select((RarArchiveEntry e) => (e, IComicAnalyzer.GetImageMime(e.Key)))
                .Where(static pair => pair.Item2 is not null)
                .OrderBy(static pair => pair.Item1.Key)
                .Skip(page - 1)
                .FirstOrDefault();
        if (entry is null || mime is null)
            return null;
        using var stream = new MemoryStream();
        entry.WriteTo(stream);
        var pageData = stream.ToArray();
        return new
        (
            Filename: entry.Key ?? "",
            Mime: mime,
            Data: pageData
        );
    }
    protected ComicPage? GetPage7Zip(string filepath, int page)
    {
        using var zip7 = SevenZipArchive.Open(filepath);
        (var entry, var mime) = zip7.Entries
                .Select((SevenZipArchiveEntry e) => (e, IComicAnalyzer.GetImageMime(e.Key)))
                .Where(static pair => pair.Item2 is not null)
                .OrderBy(static pair => pair.Item1.Key)
                .Skip(page - 1)
                .FirstOrDefault();
        if (entry is null || mime is null)
            return null;
        using var stream = new MemoryStream();
        entry.WriteTo(stream);
        var pageData = stream.ToArray();
        return new
        (
            Filename: entry.Key ?? "",
            Mime: mime,
            Data: pageData
        );
    }
}
