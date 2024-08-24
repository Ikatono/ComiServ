using ComiServ.Entities;

namespace ComiServ.Models
{
    public class ComicData
    {
        public string Handle { get; set; }
        public bool Exists { get; set; }
        public string Filepath { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int PageCount { get; set; }
        public long SizeBytes { get; set; }
        public string FileXxhash64 { get; set; }
        public List<string> Authors { get; set; }
        public List<string> Tags { get; set; }
        public ComicData(Comic comic)
        {
            Handle = comic.Handle;
            Exists = comic.Exists;
            Filepath = comic.Filepath;
            Title = comic.Title;
            PageCount = comic.PageCount;
            SizeBytes = comic.SizeBytes;
            FileXxhash64 = "";
            var unsigned = (UInt64)comic.FileXxhash64;
            for (int i = 0; i < 8; i++)
            {
                var c = unsigned % 16;
                if (c < 10)
                    FileXxhash64 += ((char)('0' + c)).ToString();
                else
                    FileXxhash64 += ((char)('A' + c - 10)).ToString();
                unsigned /= 16;
            }
            Authors = comic.ComicAuthors.Select(a => a.Author.Name).ToList();
            Tags = comic.ComicTags.Select(a => a.Tag.Name).ToList();
        }
    }
}
