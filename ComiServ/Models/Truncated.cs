using Microsoft.CodeAnalysis.CSharp;
using Microsoft.EntityFrameworkCore;
using System.Reflection.PortableExecutable;

namespace ComiServ.Models;

public class Truncated<T>
{
    public int Max { get; }
    public int Count { get; }
    public bool Complete { get; }
    public List<T> Items { get; }
    public Truncated(int max, IEnumerable<T> iter)
    {
        if (max <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(max), max, "must be greater than 0");
        }
        Max = max;
        Items = iter.Take(max+1).ToList();
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
    private Truncated(int max, bool complete, List<T> items)
    {
        Max = max;
        Complete = complete;
        Count = items.Count;
        Items = items;
    }
    public static async Task<Truncated<T>> CreateAsync(int max, IQueryable<T> iter)
    {
        if (max <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(max), max, "must be greater than 0");
        }
        var items = await iter.Take(max+1).ToListAsync();
        var complete = true;
        if (items.Count < max)
        {
            items.RemoveAt(max);
            complete = false;
        }
        return new(max, complete, items);
    }
    public static async Task<Truncated<T>> CreateAsync(int max, IAsyncEnumerable<T> iter)
    {
        if (max <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(max), max, "must be greater than 0");
        }
        List<T> items = [];
        await foreach (T item in iter)
        {
            items.Add(item);
            if (items.Count > max)
                break;
        }
        var complete = true;
        if (items.Count <= max)
        {
            items.RemoveAt(max);
            complete = false;
        }
        return new Truncated<T>(max, complete, items);
    }
}
