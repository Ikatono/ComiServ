using ComiServ;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComiServUnitTest;

internal static class DatabaseHelper
{
	public static ComicsContext CreateDatabase()
	{
		var connection = new SqliteConnection("Filename=:memory:");
		connection.Open();
		var contextOptions = new DbContextOptionsBuilder<ComicsContext>()
			.UseSqlite(connection)
			.Options;
		var context = new ComicsContext(contextOptions);
		context.Database.EnsureCreated();
		return context;
	}
}
