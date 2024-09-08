using ComiServ.Services;
using Microsoft.Build.Framework;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComiServUnitTest.Mocks;

internal class MockConfig : IConfigService
{
	private readonly Configuration _Config;
	public Configuration Config => _Config.Copy();
	public MockConfig(Configuration config)
	{
		_Config = config;
	}
}
