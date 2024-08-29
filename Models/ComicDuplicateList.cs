using ComiServ.Entities;

namespace ComiServ.Models;

public class ComicDuplicateList
{
    public long Hash { get; set; }
    public int Count { get; set; }
    public List<ComicData> Comics { get; set; }
    public ComicDuplicateList(long hash, IEnumerable<Comic> comics)
    {
        Hash = hash;
        Comics = comics.Select(c => new ComicData(c)).ToList();
        Count = Comics.Count;
    }
    public ComicDuplicateList(long hash, IEnumerable<ComicData> comics)
    {
        Hash = hash;
        Comics = comics.ToList();
        Count = Comics.Count;
    }
}
