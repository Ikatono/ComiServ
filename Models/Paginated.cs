namespace ComiServ.Models
{
    public class Paginated<T>
    {
        public int Max { get; }
        public int Page { get;}
        public bool Last { get; }
        public int Count { get; }
        public List<T> Items { get; }
        public Paginated(int max, int page, IEnumerable<T> iter)
        {
            Max = max;
            Page = page;
            if (max <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(max), max, "must be greater than 0");
            }
            if (page < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(page), page, "must be greater than or equal to 0");
            }
            Items = iter.Skip(max * page).Take(max + 1).ToList();
            if (Items.Count > max)
            {
                Last = false;
                Items.RemoveAt(max);
            }
            else
            {
                Last = true;
            }
            Count = Items.Count;
        }
    }
}
