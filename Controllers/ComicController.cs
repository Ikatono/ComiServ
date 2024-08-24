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

namespace ComiServ.Controllers
{
    [Route("api/v1/comics")]
    [ApiController]
    public class ComicController(ComicsContext context, ILogger<ComicController> logger, IConfigService config, IComicAnalyzer analyzer)
        : ControllerBase
    {
        private readonly ComicsContext _context = context;
        private readonly ILogger<ComicController> _logger = logger;
        private readonly Configuration _config = config.Config;
        private readonly IComicAnalyzer _analyzer = analyzer;
        //TODO search parameters
        [HttpGet]
        [ProducesResponseType<Paginated<ComicData>>(StatusCodes.Status200OK)]
        public IActionResult SearchComics(
            [FromQuery(Name = "TitleSearch")]
            string? titleSearch,
            [FromQuery(Name = "DescriptionSearch")]
            string? descSearch,
            [FromQuery]
            string[] authors,
            [FromQuery]
            string[] tags,
            [FromQuery]
            string? pages,
            [FromQuery]
            string? xxhash64Hex,
            [FromQuery]
            bool? exists,
            [FromQuery]
            [DefaultValue(0)]
            int page,
            [FromQuery]
            [DefaultValue(20)]
            int pageSize
            )
        {
            //throw new NotImplementedException();
            var results = _context.Comics
                .Include("ComicAuthors.Author")
                .Include("ComicTags.Tag");
            if (exists is not null)
            {
                results = results.Where(c => c.Exists == exists);
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
                //results = results.Where(c => EF.Functions.Like(c.Title, $"*{titleSearch}*"));
                results = results.Where(c => c.Title.Contains(titleSearch));
            }
            if (descSearch is not null)
            {
                //results = results.Where(c => EF.Functions.Like(c.Description, $"*{descSearch}*"));
                results = results.Where(c => c.Description.Contains(descSearch));
            }
            int offset = page * pageSize;
            return Ok(new Paginated<ComicData>(pageSize, page, results.Skip(offset)
                .Select(c => new ComicData(c))));
        }
        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult DeleteComicsThatDontExist()
        {
            var search = _context.Comics.Where(c => !c.Exists);
            var nonExtant = search.ToList();
            search.ExecuteDelete();
            _context.SaveChanges();
            return Ok(search.Select(c => new ComicData(c)));
        }
        [HttpGet("{handle}")]
        [ProducesResponseType<ComicData>(StatusCodes.Status200OK)]
        [ProducesResponseType<RequestError>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<RequestError>(StatusCodes.Status400BadRequest)]
        public IActionResult GetSingleComicInfo(string handle)
        {
            _logger.LogInformation("GetSingleComicInfo: {handle}", handle);
            handle = handle.Trim().ToUpper();
            if (handle.Length != ComicsContext.HANDLE_LENGTH)
                return BadRequest(RequestError.InvalidHandle);
            var comic = _context.Comics
                .Include("ComicAuthors.Author")
                .Include("ComicTags.Tag")
                .SingleOrDefault(c => c.Handle == handle);
            if (comic is Comic actualComic)
                return Ok(new ComicData(comic));
            else
                return NotFound(RequestError.ComicNotFound);
        }
        [HttpPatch("{handle}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType<RequestError>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<RequestError>(StatusCodes.Status404NotFound)]
        public IActionResult UpdateComicMetadata(string handle, [FromBody] ComicMetadataUpdate metadata)
        {
            //throw new NotImplementedException();
            if (handle.Length != ComicsContext.HANDLE_LENGTH)
                return BadRequest(RequestError.InvalidHandle);
            //using var transaction = _context.Database.BeginTransaction();
            var comic = _context.Comics.SingleOrDefault(c => c.Handle == handle);
            if (comic is Comic actualComic)
            {
                if (metadata.Title != null)
                    actualComic.Title = metadata.Title;
                if (metadata.Authors is List<string> authors)
                {
                    //make sure all authors exist, without changing Id of pre-existing authors
                    //TODO try to batch these
                    authors.ForEach(author => _context.Database.ExecuteSql(
                        $"INSERT OR IGNORE INTO [Authors] (Name) VALUES ({author})"));
                    //get the Id of needed authors
                    var authorEntities = _context.Authors.Where(a => authors.Contains(a.Name)).ToList();
                    //delete existing author mappings
                    _context.ComicAuthors.RemoveRange(_context.ComicAuthors.Where(ca => ca.Comic.Id == comic.Id));
                    //add all author mappings
                    _context.ComicAuthors.AddRange(authorEntities.Select(a => new ComicAuthor { Comic = comic, Author = a }));
                }
                if (metadata.Tags is List<string> tags)
                {
                    //make sure all tags exist, without changing Id of pre-existing tags
                    //TODO try to batch these
                    tags.ForEach(tag => _context.Database.ExecuteSql(
                        $"INSERT OR IGNORE INTO [Tags] (Name) VALUES ({tag})"));
                    //get the needed tags
                    var tagEntities = _context.Tags.Where(t => tags.Contains(t.Name)).ToList();
                    //delete existing tag mappings
                    _context.ComicTags.RemoveRange(_context.ComicTags.Where(ta => ta.Comic.Id == comic.Id));
                    //add all tag mappings
                    _context.ComicTags.AddRange(tagEntities.Select(t => new ComicTag { Comic = comic, Tag = t }));
                }
                _context.SaveChanges();
                return Ok();
            }
            else
                return NotFound(RequestError.ComicNotFound);
        }
        //[HttpDelete("{handle}")]
        //public IActionResult DeleteComic(string handle)
        //{
        //    throw new NotImplementedException();
        //}
        [HttpGet("{handle}/file")]
        [ProducesResponseType<byte[]>(StatusCodes.Status200OK)]
        [ProducesResponseType<RequestError>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<RequestError>(StatusCodes.Status404NotFound)]
        public IActionResult GetComicFile(string handle)
        {
            _logger.LogInformation($"{nameof(GetComicFile)}: {handle}");
            handle = handle.Trim().ToUpper();
            if (handle.Length != ComicsContext.HANDLE_LENGTH)
                return BadRequest(RequestError.InvalidHandle);
            var comic = _context.Comics.SingleOrDefault(c => c.Handle == handle);
            if (comic is null)
                return NotFound(RequestError.ComicNotFound);
            var data = System.IO.File.ReadAllBytes(Path.Join(_config.LibraryRoot, comic.Filepath));
            return File(data, "application/octet-stream", new FileInfo(comic.Filepath).Name);
        }
        [HttpGet("{handle}/cover")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType<RequestError>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<RequestError>(StatusCodes.Status404NotFound)]
        public IActionResult GetComicCover(string handle)
        {
            _logger.LogInformation($"{nameof(GetComicCover)}: {handle}");
            var validated = ComicsContext.CleanValidateHandle(handle);
            if (validated is null)
                return BadRequest(RequestError.InvalidHandle);
            var comic = _context.Comics
                .SingleOrDefault(c => c.Handle == validated);
            if (comic is null)
                return NotFound(RequestError.ComicNotFound);
            var cover = _context.Covers
                .SingleOrDefault(cov => cov.FileXxhash64 == comic.FileXxhash64);
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
        public IActionResult GetComicPage(string handle, int page)
        {
            _logger.LogInformation($"{nameof(GetComicPage)}: {handle} {page}");
            var validated = ComicsContext.CleanValidateHandle(handle);
            if (validated is null)
                return BadRequest(RequestError.InvalidHandle);
            var comic = _context.Comics.SingleOrDefault(c => c.Handle == validated);
            if (comic is null)
                return NotFound(RequestError.ComicNotFound);
            var comicPage = _analyzer.GetComicPage(Path.Join(_config.LibraryRoot, comic.Filepath), page);
            if (comicPage is null)
                //TODO rethink error code
                return NotFound(RequestError.PageNotFound);
            return File(comicPage.Data, comicPage.Mime);
        }
        [HttpPost("cleandb")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult CleanUnusedTagAuthors()
        {
            //Since ComicAuthors uses foreign keys 
            _context.Authors
                .Include("ComicAuthors")
                .Where(a => a.ComicAuthors.Count == 0)
                .ExecuteDelete();
            _context.Tags
                .Include("ComicTags")
                .Where(a => a.ComicTags.Count == 0)
                .ExecuteDelete();
            //ExecuteDelete doesn't wait for SaveChanges
            //_context.SaveChanges();
            return Ok();
        }
    }
}
