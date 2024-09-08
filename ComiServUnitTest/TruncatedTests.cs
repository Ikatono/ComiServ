using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComiServ.Models;

namespace ComiServUnitTest;

[TestClass]
public class TruncatedTests
{
	[TestMethod]
	public void TruncationUnderflow()
	{
		const int truncMax = 20;
		const int dataSize = 10;
		var data = Enumerable.Range(0, dataSize).ToArray();
		var trunc = new Truncated<int>(truncMax, data);
		Assert.IsTrue(trunc.Complete);
		Assert.AreEqual(truncMax, trunc.Max);
		Assert.AreEqual(dataSize, trunc.Count);
		Assert.AreEqual(trunc.Items.Count, trunc.Count);
		Assert.AreEqual(data.First(), trunc.Items.First());
		Assert.AreEqual(data.Last(), trunc.Items.Last());
	}
	[TestMethod]
	public void TruncationOverflow()
	{
		const int truncMax = 20;
		const int dataSize = 30;
		var data = Enumerable.Range(0, dataSize).ToArray();
		var trunc = new Truncated<int>(truncMax, data);
		Assert.IsFalse(trunc.Complete);
		Assert.AreEqual(truncMax, trunc.Max);
		Assert.AreEqual(truncMax, trunc.Count);
		Assert.AreEqual(trunc.Items.Count, trunc.Count);
		Assert.AreEqual(data.First(), trunc.Items.First());
		Assert.AreEqual(truncMax - 1, trunc.Items.Last());
	}
	[TestMethod]
	public void TruncationExact()
	{
		const int truncMax = 5;
		const int dataSize = 5;
		var data = Enumerable.Range(0, dataSize).ToArray();
		var trunc = new Truncated<int>(truncMax, data);
		Assert.IsTrue(trunc.Complete);
		Assert.AreEqual(truncMax, trunc.Max);
		Assert.AreEqual(truncMax, trunc.Count);
		Assert.AreEqual(trunc.Items.Count, trunc.Count);
		Assert.AreEqual(data.First(), trunc.Items.First());
		Assert.AreEqual(data.Last(), trunc.Items.Last());
	}
	[TestMethod]
	public void TruncationEmpty()
	{
		const int truncMax = 5;
		const int dataSize = 0;
		var data = Enumerable.Range(0, dataSize).ToArray();
		var trunc = new Truncated<int>(truncMax, data);
		Assert.IsTrue(trunc.Complete);
		Assert.AreEqual(truncMax, trunc.Max);
		Assert.AreEqual(0, trunc.Count);
		Assert.AreEqual(trunc.Items.Count, trunc.Count);
	}
}
