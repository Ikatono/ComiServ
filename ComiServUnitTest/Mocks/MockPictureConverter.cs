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
		public MockPictureConverter()
		{

		}

		public Task<Stream> MakeThumbnail(Stream image)
		{
			throw new NotImplementedException();
		}

		public Task<Stream> Resize(Stream image, Size newSize, PictureFormats? newFormat = null)
		{
			throw new NotImplementedException();
		}

		public Task<Stream> ResizeIfBigger(Stream image, Size maxSize, PictureFormats? newFormat = null)
		{
			throw new NotImplementedException();
		}
	}
}
