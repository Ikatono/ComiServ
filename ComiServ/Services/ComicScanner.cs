using System.Collections.Generic;
using System.Runtime.InteropServices;
using ComiServ.Controllers;
using ComiServ.Entities;
using ComiServ.Extensions;
using ComiServ.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.Ini;
using Microsoft.OpenApi.Writers;

namespace ComiServ.Background;

public record class ComicScanItem
(
    string Filepath,
    long FileSizeBytes,
    Int64 Xxhash,
    int PageCount
);
public interface IComicScanner : IDisposable
{
    //TODO should be configurable
    public static readonly IReadOnlyList<string> COMIC_EXTENSIONS = [
        "cbz", "zip",
        "cbr", "rar",
        "cb7", "7zip",
    ];
    public void TriggerLibraryScan();
    public void ScheduleRepeatedLibraryScans(TimeSpan period);
    public IDictionary<string, ComicScanItem> PerfomLibraryScan(CancellationToken? token = null);
}
public class ComicScanner(
    IServiceProvider provider
    ) : IComicScanner
{
    //private readonly ComicsContext _context = context;
    private readonly ITaskManager _manager = provider.GetRequiredService<ITaskManager>();
    private readonly Configuration _config = provider.GetRequiredService<IConfigService>().Config;
    private readonly IComicAnalyzer _analyzer = provider.GetRequiredService<IComicAnalyzer>();
    private readonly IServiceProvider _provider = provider;

    public IDictionary<string, ComicScanItem> PerfomLibraryScan(CancellationToken? token = null)
    {
        return new DirectoryInfo(_config.LibraryRoot).EnumerateFiles("*", SearchOption.AllDirectories)
            .Select(fi =>
            {
                token?.ThrowIfCancellationRequested();
                var path = Path.GetRelativePath(_config.LibraryRoot, fi.FullName);
                var analysis = _analyzer.AnalyzeComic(fi.FullName);
                if (analysis is null)
                    //null will be filtered
                    return (path, null);
                return (path, new ComicScanItem
                (
                    Filepath: path,
                    FileSizeBytes: analysis.FileSizeBytes,
                    Xxhash: analysis.Xxhash,
                    PageCount: analysis.PageCount
                ));
            })
            //ignore files of the wrong extension
            .Where(p => p.Item2 is not null)
            .ToDictionary();
    }
    public void TriggerLibraryScan()
    {
        SyncTaskItem ti = new(
            TaskTypes.Scan,
            "Library Scan",
            async token =>
            {
                var items = PerfomLibraryScan(token);
                token?.ThrowIfCancellationRequested();
                await UpdateDatabaseWithScanResults(items);
            },
            null);
        _manager.StartTask(ti);
    }
    private CancellationTokenSource? RepeatedLibraryScanTokenSource = null;
    public void ScheduleRepeatedLibraryScans(TimeSpan interval)
    {
        RepeatedLibraryScanTokenSource?.Cancel();
        RepeatedLibraryScanTokenSource?.Dispose();
        RepeatedLibraryScanTokenSource = new();
        AsyncTaskItem ti = new(
            TaskTypes.Scan,
            "Scheduled Library Scan",
            async token =>
            {
                var items = PerfomLibraryScan(token);
                token?.ThrowIfCancellationRequested();
                await UpdateDatabaseWithScanResults(items);
            },
            RepeatedLibraryScanTokenSource.Token);
        _manager.ScheduleTask(ti, interval);
    }
    public async Task UpdateDatabaseWithScanResults(IDictionary<string, ComicScanItem> items)
    {
        using var scope = _provider.CreateScope();
        var services = scope.ServiceProvider;
        using var context = services.GetRequiredService<ComicsContext>();
        //not an ideal algorithm
        //need to go through every comic in the database to update `Exists`
        //also need to go through every discovered comic to add new ones
        //and should make sure not to double up on the overlaps
        //there should be a faster method than using ExceptBy but I don't it's urgent
        //TODO profile on large database
        SortedSet<string> alreadyExistingFiles = [];
        foreach (var comic in context.Comics)
        {
            ComicScanItem info;
            if (items.TryGetValue(comic.Filepath, out info))
            {
                comic.FileXxhash64 = info.Xxhash;
                comic.Exists = true;
                comic.PageCount = info.PageCount;
                comic.SizeBytes = info.FileSizeBytes;
                alreadyExistingFiles.Add(comic.Filepath);
            }
            else
            {
                comic.Exists = false;
            }
        }
        var newComics = items.ExceptBy(alreadyExistingFiles, p => p.Key).Select(p =>
            new Comic()
            {
                Handle = context.CreateHandle(),
                Exists = true,
                Filepath = p.Value.Filepath,
                Title = new FileInfo(p.Value.Filepath).Name,
                Description = "",
                SizeBytes = p.Value.FileSizeBytes,
                FileXxhash64 = p.Value.Xxhash,
                PageCount = p.Value.PageCount
            }).ToList();
        //newComics.ForEach(c => _manager.StartTask(new(
        //    TaskTypes.GetCover,
        //    $"Get Cover: {c.Title}",
        //    token => InsertCover(Path.Join(_config.LibraryRoot, c.Filepath), c.FileXxhash64)
        //    )));
        foreach (var comic in newComics)
        {
            _manager.StartTask((AsyncTaskItem)new(
                TaskTypes.GetCover,
                $"Get Cover: {comic.Title}",
                token => InsertCover(Path.Join(_config.LibraryRoot, comic.Filepath), comic.FileXxhash64)
                ));
        }
        //newComics.ForEach(c => _manager.StartTask(new(
        //    TaskTypes.MakeThumbnail,
        //    $"Make Thumbnail: {c.Title}",
        //    token => InsertThumbnail(c.Handle, Path.Join(_config.LibraryRoot, c.Filepath), 1)
        //    )));
        foreach (var comic in newComics)
        {
            _manager.StartTask((AsyncTaskItem)new(
                TaskTypes.MakeThumbnail,
                $"Make Thumbnail: {comic.Title}",
                token => InsertThumbnail(comic.Handle, Path.Join(_config.LibraryRoot, comic.Filepath), 1)
                ));
        }
        context.Comics.AddRange(newComics);
        await context.SaveChangesAsync();
    }
    protected async Task InsertCover(string filepath, long hash)
    {
        using var scope = _provider.CreateScope();
        var services = scope.ServiceProvider;
        using var context = services.GetRequiredService<ComicsContext>();
        var existing = await context.Covers.SingleOrDefaultAsync(c => c.FileXxhash64 == hash);
        //assuming no hash overlap
        //if you already have a cover, assume it's correct
        if (existing is not null)
            return;
        var page = await _analyzer.GetComicPageAsync(filepath, 1);
        if (page is null)
            return;
        Cover cover = new()
        {
            FileXxhash64 = hash,
            Filename = page.Filename,
            CoverFile = page.Data
        };
        context.InsertOrIgnore(cover, true);
    }
    protected async Task InsertThumbnail(string handle, string filepath, int page = 1)
    {
        using var scope = _provider.CreateScope();
        var services = scope.ServiceProvider;
        using var context = services.GetRequiredService<ComicsContext>();
        var comic = await context.Comics.SingleOrDefaultAsync(c => c.Handle == handle);
        if (comic?.ThumbnailWebp is null)
            return;
        var comicPage = _analyzer.GetComicPage(filepath, page);
        if (comicPage is null)
            return;
        var converter = services.GetRequiredService<IPictureConverter>();
        using var inStream = new MemoryStream(comicPage.Data);
        var outStream = await converter.MakeThumbnail(inStream);
        comic.ThumbnailWebp = outStream.ReadAllBytes();
    }
    public void Dispose()
    {
        RepeatedLibraryScanTokenSource?.Dispose();
        GC.SuppressFinalize(this);
    }
}