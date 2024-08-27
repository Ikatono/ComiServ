using System.Reflection.PortableExecutable;

namespace ComiServ.Models
{
    public class Truncated<T>
    {
        public int Max { get; }
        public int Count { get; }
        public bool Complete { get; }
        public List<T> Items { get; }
        public Truncated(int max, IEnumerable<T> items)
        {
            if (max <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(max), max, "must be greater than 0");
            }
            Max = max;
            Items = items.Take(max+1).ToList();
            if (Items.Count <= max)
            {
                Complete = true;
            }
            else
            {
                Items.RemoveAt(max);
                Complete = false;
            }
            Count = Items.Count;
        }
    }
}
