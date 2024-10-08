﻿using System;
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
		//returned from all MockPictureConverter functions
		byte[] mockPictureData = [5, 4, 3, 2, 1];
		MockPictureConverter converter = new MockPictureConverter(mockPictureData);
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
		var result2 = await controller.GetComicPage(comic.Handle.Substring(comic.Handle.Length-1),
			PAGE_NUMBER, null, null, null);
		Assert.IsInstanceOfType<BadRequestObjectResult>(result2);
		//valid handle but doesn't exist
		var result3 = await controller.GetComicPage(string.Join("", Enumerable.Repeat("B", ComicsContext.HANDLE_LENGTH)),
			PAGE_NUMBER, null, null, null);
		Assert.IsInstanceOfType<NotFoundObjectResult>(result3);
		//valid handle and convert
		var result4 = await controller.GetComicPage(comic.Handle, PAGE_NUMBER, 500, 500, PictureFormats.Webp);
		Assert.AreEqual(1, converter.ResizeIfBiggerCount);
		Assert.IsInstanceOfType<FileContentResult>(result4);
		Assert.IsTrue(mockPictureData.SequenceEqual(((FileContentResult)result4).FileContents));
	}
}
