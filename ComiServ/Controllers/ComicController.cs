using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ComiServ.Models;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using ComiServ.Entities;
using ComiServ.Background;
using System.ComponentModel;
using ComiServ.Extensions;
using System.Runtime.InteropServices;
using ComiServ.Services;
using System.Security.Cryptography.X509Certificates;
using System.Data;
using SQLitePCL;

namespace ComiServ.Controllers;

[Route(ROUTE)]
[ApiController]
public class ComicController(ComicsContext context, ILogger<ComicController> logger, IConfigService config, IComicAnalyzer analyzer, IPictureConverter converter, IAuthenticationService _auth)
    : ControllerBase
{
    public const string ROUTE = "/api/v1/comics";
    private readonly ComicsContext _context = context;
    private readonly ILogger<ComicController> _logger = logger;
    private readonly Configuration _config = config.Config;
    private readonly IComicAnalyzer _analyzer = analyzer;
    private readonly IPictureConverter _converter = converter;
    private readonly IAuthenticationService _auth = _auth;
    //TODO search parameters
    [HttpGet]
    [ProducesResponseType<Paginated<ComicData>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchComics(
        [FromQuery(Name = "title")]
        string? titleSearch,
        [FromQuery(Name = "description")]
        string? descSearch,
        [FromQuery]
        string[] authors,
        [FromQuery]
        string[] tags,
        [FromQuery]
        string? pages,
        [FromQuery(Name = "hash")]
        string? xxhash64Hex,
        [FromQuery]
        bool? exists,
        [FromQuery]
        [DefaultValue(null)]
        bool? read,
        [FromQuery]
        [DefaultValue(0)]
        int page,
        [FromQuery]
        [DefaultValue(20)]
        int pageSize
        )
    {
        var results = _context.Comics
            .Include("ComicAuthors.Author")
            .Include("ComicTags.Tag");
        if (exists is not null)
        {
            results = results.Where(c => c.Exists == exists);
        }
        string username;
        if (_auth.User is null)
        {
            return Unauthorized(RequestError.NotAuthenticated);
        }
        if (read is bool readStatus)
        {
            if (readStatus)
                results = results.Where(c => c.ReadBy.Any(u => EF.Functions.Like(_auth.User.Username, u.User.Username)));
            else
                results = results.Where(c => c.ReadBy.All(u => !EF.Functions.Like(_auth.User.Username, u.User.Username)));
        }
        foreach (var author in authors)
        {
            results = results.Where(c => c.ComicAuthors.Any(ca => EF.Functions.Like(ca.Author.Name, author)));
        }
        foreach (var tag in tags)
        {
            results = results.Where(c => c.ComicTags.Any(ct => EF.Functions.Like(ct.Tag.Name, tag)));
        }
        if (pages is not null)
        {
            pages = pages.Trim();
            if (pages.StartsWith("<="))
            {
                var pageMax = int.Parse(pages.Substring(2));
                results = results.Where(c => c.PageCount <= pageMax);
            }
            else if (pages.StartsWith('<'))
            {
                var pageMax = int.Parse(pages.Substring(1));
                results = results.Where(c => c.PageCount < pageMax);
            }
            else if (pages.StartsWith(">="))
            {
                var pageMin = int.Parse(pages.Substring(2));
                results = results.Where(c => c.PageCount >= pageMin);
            }
            else if (pages.StartsWith('>'))
            {
                var pageMin = int.Parse(pages.Substring(1));
                results = results.Where(c => c.PageCount > pageMin);
            }
            else
            {
                if (pages.StartsWith('='))
                    pages = pages.Substring(1);
                var pageExact = int.Parse(pages);
                results = results.Where(c => c.PageCount == pageExact);
            }
        }
        if (xxhash64Hex is not null)
        {
            xxhash64Hex = xxhash64Hex.Trim().ToUpper();
            if (!xxhash64Hex.All(c => (c >= '0' && c <= '9') || (c >= 'A' && c <= 'F')))
                return BadRequest();
            Int64 hash = 0;
            foreach (char c in xxhash64Hex)
            {
                if (c >= '0' && c <= '9')
                    hash = hash * 16 + (c - '0');
                else if (c >= 'A' && c <= 'F')
                    hash = hash * 16 + (c - 'A' + 10);
                else
                    throw new ArgumentException("Invalid hex character bypassed filter");
            }
            results = results.Where(c => c.FileXxhash64 == hash);
        }
        if (titleSearch is not null)
        {
            titleSearch = titleSearch.Trim();
            results = results.Where(c => EF.Functions.Like(c.Title, $"%{titleSearch}%"));
        }
        if (descSearch is not null)
        {
            descSearch = descSearch.Trim();
            results = results.Where(c => EF.Functions.Like(c.Description, $"%{descSearch}%"));
        }
        int offset = page * pageSize;
        return Ok(await Paginated<ComicData>.CreateAsync(pageSize, page, results
                                                                            .OrderBy(c => c.Id)
                                                                            .Select(c => new ComicData(c))));
    }
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteComicsThatDontExist()
    {
        var search = _context.Comics.Where(c => !c.Exists);
        var nonExtant = await search.ToListAsync();
        search.ExecuteDelete();
        _context.SaveChanges();
        return Ok(search.Select(c => new ComicData(c)));
    }
    [HttpGet("{handle}")]
    [ProducesResponseType<ComicData>(StatusCodes.Status200OK)]
    [ProducesResponseType<RequestError>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<RequestError>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSingleComicInfo(string handle)
    {
        //_logger.LogInformation("GetSingleComicInfo: {handle}", handle);
        var validated = ComicsContext.CleanValidateHandle(handle);
        if (validated is null)
            return BadRequest(RequestError.InvalidHandle);
        var comic = await _context.Comics
            .Include("ComicAuthors.Author")
            .Include("ComicTags.Tag")
            .SingleOrDefaultAsync(c => c.Handle == handle);
        if (comic is Comic actualComic)
            return Ok(new ComicData(comic));
        else
            return NotFound(RequestError.ComicNotFound);
    }
    [HttpPatch("{handle}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<RequestError>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<RequestError>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<RequestError>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateComicMetadata(string handle, [FromBody] ComicMetadataUpdateRequest metadata)
    {
        if (handle.Length != ComicsContext.HANDLE_LENGTH)
            return BadRequest(RequestError.InvalidHandle);
        var comic = await _context.Comics.SingleOrDefaultAsync(c => c.Handle == handle);
        if (comic is Comic actualComic)
        {
            if (metadata.Title != null)
                actualComic.Title = metadata.Title;
            if (metadata.Authors is List<string> authors)
            {
                //make sure all authors exist, without changing Id of pre-existing authors
                _context.InsertOrIgnore(authors.Select(author => new Author() { Name = author }), ignorePrimaryKey: true);
                //get the Id of needed authors
                var authorEntities = await _context.Authors.Where(a => authors.Contains(a.Name)).ToListAsync();
                //delete existing author mappings
                _context.ComicAuthors.RemoveRange(_context.ComicAuthors.Where(ca => ca.Comic.Id == comic.Id));
                //add all author mappings
                _context.ComicAuthors.AddRange(authorEntities.Select(a => new ComicAuthor { Comic = comic, Author = a }));
            }
            if (metadata.Tags is List<string> tags)
            {
                //make sure all tags exist, without changing Id of pre-existing tags
                _context.InsertOrIgnore(tags.Select(t => new Tag() { Name = t }), ignorePrimaryKey: true);
                //get the needed tags
                var tagEntities = await _context.Tags.Where(t => tags.Contains(t.Name)).ToListAsync();
                //delete existing tag mappings
                _context.ComicTags.RemoveRange(_context.ComicTags.Where(ta => ta.Comic.Id == comic.Id));
                //add all tag mappings
                _context.ComicTags.AddRange(tagEntities.Select(t => new ComicTag { Comic = comic, Tag = t }));
            }
            await _context.SaveChangesAsync();
            return Ok();
        }
        else
            return NotFound(RequestError.ComicNotFound);
    }
    [HttpPatch("{handle}/markread")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<RequestError>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<RequestError>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MarkComicAsRead(
        ComicsContext context,
        string handle)
    {
        var validated = ComicsContext.CleanValidateHandle(handle);
        if (validated is null)
            return BadRequest(RequestError.InvalidHandle);
        var comic = await context.Comics.SingleOrDefaultAsync(c => c.Handle == validated);
        if (comic is null)
            return NotFound(RequestError.ComicNotFound);
        if (_auth.User is null)
            //user shouldn't have passed authentication if username doesn't match
            return StatusCode(StatusCodes.Status500InternalServerError);
        var comicRead = new ComicRead()
        {
            UserId = _auth.User.Id,
            ComicId = comic.Id
        };
        context.InsertOrIgnore(comicRead, ignorePrimaryKey: false);
        await context.SaveChangesAsync();
        return Ok();
    }
    [HttpPatch("{handle}/markunread")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<RequestError>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<RequestError>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MarkComicAsUnread(
        ComicsContext context,
        string handle)
    {
        var validated = ComicsContext.CleanValidateHandle(handle);
        if (validated is null)
            return BadRequest(RequestError.InvalidHandle);
        var comic = await context.Comics.SingleOrDefaultAsync(c => c.Handle == validated);
        if (comic is null)
            return NotFound(RequestError.ComicNotFound);
        if (_auth.User is null)
            //user shouldn't have passed authentication if username doesn't match
            return StatusCode(StatusCodes.Status500InternalServerError);
        var comicRead = await context.ComicsRead.SingleOrDefaultAsync(cr =>
            cr.ComicId == comic.Id && cr.UserId == _auth.User.Id);
        if (comicRead is null)
            return Ok();
        context.ComicsRead.Remove(comicRead);
        await context.SaveChangesAsync();
        return Ok();
    }
    [HttpDelete("{handle}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<RequestError>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<RequestError>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<RequestError>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteComic(
        string handle,
        [FromBody]
        ComicDeleteRequest req)
    {
        if (_auth.User is null)
        {
            HttpContext.Response.Headers.WWWAuthenticate = "Basic";
            return Unauthorized(RequestError.NoAccess);
        }
        if (_auth.User.UserTypeId != UserTypeEnum.Administrator)
            return Forbid();
        var comic = await _context.Comics.SingleOrDefaultAsync(c => c.Handle == handle);
        if (comic is null)
            return NotFound(RequestError.ComicNotFound);
        comic.Exists = _analyzer.ComicFileExists(string.Join(config.Config.LibraryRoot, comic.Filepath));
        if (comic.Exists && !req.DeleteIfFileExists)
            return BadRequest(RequestError.ComicFileExists);
        _context.Comics.Remove(comic);
        await _context.SaveChangesAsync();
        _analyzer.DeleteComicFile(string.Join(config.Config.LibraryRoot, comic.Filepath));
        return Ok();
    }
    [HttpGet("{handle}/file")]
    [ProducesResponseType<byte[]>(StatusCodes.Status200OK)]
    [ProducesResponseType<RequestError>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<RequestError>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetComicFile(string handle)
    {
        //_logger.LogInformation(nameof(GetComicFile) + ": {handle}", handle);
        var validated = ComicsContext.CleanValidateHandle(handle);
        if (validated is null)
            return BadRequest(RequestError.InvalidHandle);
        var comic = await _context.Comics.SingleOrDefaultAsync(c => c.Handle == handle);
        if (comic is null)
            return NotFound(RequestError.ComicNotFound);
        //can't mock this easily, move to a service?
        //maybe IComicAnalyzer should be used even just to read a file
        var data = await System.IO.File.ReadAllBytesAsync(Path.Join(_config.LibraryRoot, comic.Filepath));
        return File(data, "application/octet-stream", new FileInfo(comic.Filepath).Name);
    }
    [HttpGet("{handle}/cover")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<RequestError>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<RequestError>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetComicCover(string handle)
    {
        //_logger.LogInformation(nameof(GetComicCover) + ": {handle}", handle);
        var validated = ComicsContext.CleanValidateHandle(handle);
        if (validated is null)
            return BadRequest(RequestError.InvalidHandle);
        var comic = await _context.Comics
            .SingleOrDefaultAsync(c => c.Handle == validated);
        if (comic is null)
            return NotFound(RequestError.ComicNotFound);
        var cover = await _context.Covers
            .SingleOrDefaultAsync(cov => cov.FileXxhash64 == comic.FileXxhash64);
        if (cover is null)
            return NotFound(RequestError.CoverNotFound);
        var mime = IComicAnalyzer.GetImageMime(cover.Filename);
        if (mime is null)
            return File(cover.CoverFile, "application/octet-stream", cover.Filename);
        return File(cover.CoverFile, mime);
    }
    [HttpGet("{handle}/page/{page}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<RequestError>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<RequestError>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetComicPage(string handle, int page,
        [FromQuery]
        [DefaultValue(0)]
        int? maxWidth,
        [FromQuery]
        [DefaultValue(0)]
        int? maxHeight,
        [FromQuery]
        [DefaultValue(null)]
        PictureFormats? format)
    {
        //_logger.LogInformation(nameof(GetComicPage) + ": {handle} {page}", handle, page);
        var validated = ComicsContext.CleanValidateHandle(handle);
        if (validated is null)
            return BadRequest(RequestError.InvalidHandle);
        var comic = await _context.Comics.SingleOrDefaultAsync(c => c.Handle == validated);
        if (comic is null)
            return NotFound(RequestError.ComicNotFound);
        var comicPage = await _analyzer.GetComicPageAsync(Path.Join(_config.LibraryRoot, comic.Filepath), page);
        if (comicPage is null)
            //TODO rethink error code
            return NotFound(RequestError.PageNotFound);
        var limitWidth = maxWidth ?? -1;
        var limitHeight = maxHeight ?? -1;
        if (maxWidth > 0 || maxHeight > 0 || format is not null)
        {
            //TODO this copy is not strictly necessary, but avoiding it would mean keeping the comic file
            //open after GetComicPage returns to keep the stream. Not unreasonable (that's what IDisposable
            //is for) but need to be careful.
            using var stream = new MemoryStream(comicPage.Data);
            System.Drawing.Size limit = new(
                limitWidth > 0 ? limitWidth : int.MaxValue,
                limitHeight > 0 ? limitHeight : int.MaxValue
            );
            string mime = format switch
            {
                PictureFormats f => IPictureConverter.GetMime(f),
                null => comicPage.Mime,
            };
            //TODO using the stream directly throws but I think it should be valid, need to debug
            using var resizedStream = await _converter.ResizeIfBigger(stream, limit, format);
            var arr = await resizedStream.ReadAllBytesAsync();
            return File(arr, mime);
        }
        else
            return File(comicPage.Data, comicPage.Mime);
    }
    [HttpGet("{handle}/thumbnail")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<RequestError>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<RequestError>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetComicThumbnail(
        string handle,
        [FromQuery]
        [DefaultValue(false)]
        //if thumbnail doesn't exist, try to find a cover
        bool fallbackToCover)
    {
        RequestError accErrors = new();
        var validated = ComicsContext.CleanValidateHandle(handle);
        if (validated is null)
            return BadRequest(RequestError.InvalidHandle);
        var comic = await _context.Comics.SingleOrDefaultAsync(c => c.Handle == validated);
        if (comic is null)
            return NotFound(RequestError.ComicNotFound);
        if (comic.ThumbnailWebp is byte[] img)
        {
            return File(img, "application/webp");
        }
        if (fallbackToCover)
        {
            var cover = await _context.Covers.SingleOrDefaultAsync(c => c.FileXxhash64 == comic.FileXxhash64);
            if (cover is not null)
            {
                //TODO should this convert to a thumbnail on the fly?
                return File(cover.CoverFile, IComicAnalyzer.GetImageMime(cover.Filename) ?? "application/octet-stream");
            }
            accErrors = accErrors.And(RequestError.CoverNotFound);
        }
        return NotFound(RequestError.ThumbnailNotFound.And(accErrors));
    }
    [HttpPost("cleandb")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> CleanUnusedTagAuthors()
    {
        await _context.Authors
            .Include(a => a.ComicAuthors)
            .Where(a => a.ComicAuthors.Count == 0)
            .ExecuteDeleteAsync();
        await _context.Tags
            .Include(a => a.ComicTags)
            .Where(a => a.ComicTags.Count == 0)
            .ExecuteDeleteAsync();
        //ExecuteDelete doesn't wait for SaveChanges
        //_context.SaveChanges();
        return Ok();
    }
    [HttpGet("duplicates")]
    [ProducesResponseType<Paginated<ComicDuplicateList>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDuplicateFiles(
        [FromQuery]
        [DefaultValue(0)]
        int page,
        [FromQuery]
        [DefaultValue(20)]
        int pageSize)
    {
        var groups = _context.Comics
            .Include("ComicAuthors.Author")
            .Include("ComicTags.Tag")
            .GroupBy(c => c.FileXxhash64)
            .Where(g => g.Count() > 1)
            .OrderBy(g => g.Key);
        var ret = await Paginated<ComicDuplicateList>.CreateAsync(pageSize, page,
            groups.Select(g =>
                new ComicDuplicateList(g.Key, g.Select(g => g))
            ));
        return Ok(ret);
    }
    [HttpGet("library")]
    [ProducesResponseType<LibraryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<RequestError>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetLibraryStats()
    {
        return Ok(new LibraryResponse(
            ComicCount: await _context.Comics.CountAsync(),
            UniqueFiles: await _context.Comics.Select(c => c.FileXxhash64).Distinct().CountAsync()
            ));
    }
}
