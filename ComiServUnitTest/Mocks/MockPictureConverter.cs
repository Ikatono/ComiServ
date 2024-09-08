using ComiServ.Background;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComiServUnitTest.Mocks
{
	internal class MockPictureConverter : IPictureConverter
	{
		public int MakeThumbnailCount = 0;
		public int ResizeCount = 0;
		public int ResizeIfBiggerCount = 0;
		private byte[] StreamObject { get; }
		public MockPictureConverter(byte[] streamObject)
		{
			StreamObject = streamObject;
		}

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		public async Task<Stream> MakeThumbnail(Stream image)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
		{
			MakeThumbnailCount++;
			return new MemoryStream(StreamObject);
		}

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		public async Task<Stream> Resize(Stream image, Size newSize, PictureFormats? newFormat = null)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
		{
			ResizeCount++;
			return new MemoryStream(StreamObject);
		}

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		public async Task<Stream> ResizeIfBigger(Stream image, Size maxSize, PictureFormats? newFormat = null)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
		{
			ResizeIfBiggerCount++;
			return new MemoryStream(StreamObject);
		}
	}
}
