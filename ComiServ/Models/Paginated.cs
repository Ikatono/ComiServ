using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.EntityFrameworkCore;

namespace ComiServ.Models;
public class Paginated<T>
{
    public int Max { get; }
    public int Page { get; }
    public bool Last { get; }
    public int Count { get; }
    public List<T> Items { get; }
    public Paginated(int max, int page, IEnumerable<T> iter)
    {
        if (max <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(max), max, "must be greater than 0");
        }
        if (page < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(page), page, "must be greater than or equal to 0");
        }
        Max = max;
        Page = page;
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
    private Paginated(int max, int page, bool last, List<T> items)
    {
        Max = max;
        Page = page;
        Last = last;
        Items = items;
        Count = Items.Count;
    }
    public static async Task<Paginated<T>> CreateAsync(int max, int page, IQueryable<T> iter)
    {
        if (max <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(max), max, "must be greater than 0");
        }
        if (page < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(page), page, "must be greater than or equal to 0");
        }
        var items = await iter.Skip(max * page).Take(max + 1).ToListAsync();
        bool last = true;
        if (items.Count > max)
        {
            last = false;
            items.RemoveAt(max);
        }
        return new(max, page, last, items);
    }
    public static async Task<Paginated<T>> CreateAsync(int max, int page, IAsyncEnumerable<T> iter)
    {
        if (max <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(max), max, "must be greater than 0");
        }
        if (page < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(page), page, "must be greater than or equal to 0");
        }
        List<T> items = [];
        var skip = max * page;
        await foreach (T item in iter)
        {
            if (skip > 0)
            {
                skip--;
                continue;
            }
            items.Add(item);
            if (items.Count >= max + 1)
                break;
        }
        var last = true;
        if (items.Count > max)
        {
            last = false;
            items.RemoveAt(max);
        }
        return new(max, page, last, items);
    }
}
