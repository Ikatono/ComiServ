using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComiServ;
using ComiServ.Entities;
using ComiServ.Services;
using ComiServ.Controllers;
using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.Extensions.Logging;
using ComiServUnitTest.Mocks;
using ComiServ.Background;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace ComiServUnitTest.ContollerTests;

[TestClass]
public class ComicControllerTests
{
	private ILoggerFactory? _factory;
	public ILoggerFactory factory
	{ get
		{
			_factory = _factory ?? LoggerFactory.Create(builder => builder.AddConsole());
			return _factory;
		}
	}

	[TestMethod]
	public async Task TestGetPage()
	{
		const int PAGE_NUMBER = 7;
		const string PAGE_FILEPATH = "Page File.jpg";
		const string PAGE_MIME = "image/jpeg";
		var context = DatabaseHelper.CreateDatabase();
		Comic comic = new()
		{
			Title = "Test Comic",
			Filepath = Path.Join("test", "filepath"),
			Handle = string.Join("", Enumerable.Repeat("A", ComicsContext.HANDLE_LENGTH)),
			Description = ""
		};
		context.Comics.Add(comic);
		User user = new()
		{
			Username = "user",
			UserTypeId = UserTypeEnum.User,
			Salt = User.MakeSalt(),
			HashedPassword = User.Hash([], []),
		};
		context.Users.Add(user);
		context.SaveChanges();
		Configuration config = new()
		{
			AutoScanPeriodHours = 24,
			DatabaseFile = "",
			LibraryRoot = "root",
		};
		ComicPage comicPage = new(PAGE_FILEPATH, PAGE_MIME, [1, 2, 3, 4, 5]);
		MockComicAnalyzer analyzer = new();
		analyzer.ComicPages.Add((Path.Join(config.LibraryRoot, comic.Filepath), PAGE_NUMBER), comicPage);
		IPictureConverter converter = new MockPictureConverter();
		AuthenticationService auth = new();
		auth.Authenticate(user);
		var controller = new ComicController(
			context,
			factory.CreateLogger<ComicController>(),
			new MockConfig(config),
			analyzer,
			converter,
			auth
			);
		//get actual page
		var result = await controller.GetComicPage(comic.Handle, PAGE_NUMBER, null, null, null);
		Assert.IsInstanceOfType<FileContentResult>(result);
		var contents = ((FileContentResult)result).FileContents;
		Assert.IsTrue(comicPage.Data.SequenceEqual(contents));
		//invalid handle (too short)
		var result2 = await controller.GetComicFile(string.Join("", Enumerable.Repeat("A", ComicsContext.HANDLE_LENGTH - 1)));
		Assert.IsInstanceOfType<BadRequestObjectResult>(result2);
		//valid handle but doesn't exist
		var result3 = await controller.GetComicFile(string.Join("", Enumerable.Repeat("B", ComicsContext.HANDLE_LENGTH)));
		Assert.IsInstanceOfType<NotFoundObjectResult>(result3);
	}
}
