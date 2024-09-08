using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Runtime.CompilerServices;

//https://stackoverflow.com/a/42467710/25956209
//https://archive.ph/RvjOy
namespace ComiServ.Extensions;

public static class DatabaseExtensions
{
    //with a compound primary key, `ignorePrimaryKey` will ignore all of them
    public static int InsertOrIgnore<T>(this DbContext context, T item, bool ignorePrimaryKey = false)
    {
        var entityType = context.Model.FindEntityType(typeof(T));
        var tableName = entityType.GetTableName();
        //var tableSchema = entityType.GetSchema();

        var cols = entityType.GetProperties()
            .Where(c => !ignorePrimaryKey || !c.IsPrimaryKey())
            .Select(c => new {
                Name = c.GetColumnName(),
                //Type = c.GetColumnType(),
                Value = c.PropertyInfo.GetValue(item)
            })
            .ToList();
        var query = "INSERT OR IGNORE INTO " + tableName
            + " (" + string.Join(", ", cols.Select(c => c.Name)) + ") " +
            "VALUES (" + string.Join(", ", cols.Select((c,i) => "{" + i + "}")) + ")";
        var args = cols.Select(c => c.Value).ToArray();
        var formattable = FormattableStringFactory.Create(query, args);
        return context.Database.ExecuteSql(formattable);
    }
    public static int InsertOrIgnore<T>(this DbContext context, IEnumerable<T> items, bool ignorePrimaryKey = false)
    {
        var entityType = context.Model.FindEntityType(typeof(T));
        var tableName = entityType.GetTableName();
        //var tableSchema = entityType.GetSchema();

        var colProps = entityType.GetProperties().Where(c => !ignorePrimaryKey || !c.IsPrimaryKey()).ToList();
        var colNames = colProps.Select(c => c.Name).ToList();
        if (colNames.Count == 0)
            throw new InvalidOperationException("No columns to insert");
        var rows = items
            .Select(item =>
                colProps.Select(c =>
                    c.PropertyInfo.GetValue(item))
                .ToList())
            .ToList();
        int count = 0;
        var query = "INSERT OR IGNORE INTO " + tableName
            + "(" + string.Join(',', colNames) + ")"
            + "VALUES" + string.Join(',', rows.Select(row =>
                "(" + string.Join(',', row.Select(v => "{" 
                + count++
                + "}")) + ")"
            ));
        var args = rows.SelectMany(row => row).ToArray();
        var formattable = FormattableStringFactory.Create(query, args);
        return context.Database.ExecuteSql(formattable);
    }
}
