using ComiServ.Background;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComiServUnitTest.Mocks
{
	internal class MockComicAnalyzer : IComicAnalyzer
	{
		//preseed these
		public readonly Dictionary<string, ComicAnalysis> AnalysisResults = [];
		public readonly HashSet<string> ComicsThatExist = [];
		public readonly Dictionary<(string, int), ComicPage> ComicPages = [];

		//check these afterwards
		public readonly List<string> Analyzed = [];
		public readonly List<string> CheckedForExistance = [];
		public readonly List<string> Deleted = [];
		public readonly List<(string, int)> RetreivedPages = [];
		public MockComicAnalyzer()
		{
			
		}
		public void Clear()
		{
			Analyzed.Clear();
			CheckedForExistance.Clear();
			Deleted.Clear();
			RetreivedPages.Clear();
		}
		public ComicAnalysis? AnalyzeComic(string filename)
		{
			Analyzed.Add(filename);
			if (AnalysisResults.TryGetValue(filename, out var analysis))
				return analysis;
			return null;
		}

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		public async Task<ComicAnalysis?> AnalyzeComicAsync(string filename)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
		{
			return AnalyzeComic(filename);
		}

		public bool ComicFileExists(string filename)
		{
			CheckedForExistance.Add(filename);
			return ComicsThatExist.Contains(filename);
		}

		public void DeleteComicFile(string filename)
		{
			Deleted.Add(filename);
		}

		public ComicPage? GetComicPage(string filepath, int page)
		{
			var key = (filepath, page);
			RetreivedPages.Add(key);
			if (ComicPages.TryGetValue(key, out var comicPage))
				return comicPage;
			return null;
		}

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		public async Task<ComicPage?> GetComicPageAsync(string filepath, int page)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
		{
			return GetComicPage(filepath, page);
		}
	}
}
