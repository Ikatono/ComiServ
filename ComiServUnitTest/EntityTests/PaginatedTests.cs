using ComiServ.Models;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using System.CodeDom;
using System.Security.Policy;

namespace ComiServUnitTest.EntityTests;

[TestClass]
public class PaginatedTests
{
    [TestMethod]
    public void PageUnderUnderflow()
    {
        const int pageNum = 1;
        const int pageSize = 20;
        const int dataSize = 30;
        var data = Enumerable.Range(0, dataSize).ToArray();
        var page = new Paginated<int>(pageSize, pageNum, data);
        Assert.IsTrue(page.Last);
        Assert.AreEqual(pageSize, page.Max);
        Assert.AreEqual(pageNum, page.Page);
        Assert.AreEqual(10, page.Count);
        Assert.AreEqual(page.Items.Count, page.Count);
        Assert.AreEqual(20, page.Items.First());
        Assert.AreEqual(data.Last(), page.Items.Last());
    }
    [TestMethod]
    public void PageOverflow()
    {
        const int pageNum = 2;
        const int pageSize = 30;
        const int dataSize = 150;
        var data = Enumerable.Range(0, dataSize).ToArray();
        var page = new Paginated<int>(pageSize, pageNum, data);
        Assert.IsFalse(page.Last);
        Assert.AreEqual(pageSize, page.Max);
        Assert.AreEqual(pageNum, page.Page);
        Assert.AreEqual(pageSize, page.Count);
        Assert.AreEqual(page.Items.Count, page.Count);
        Assert.AreEqual(pageSize * pageNum, page.Items.First());
        Assert.AreEqual(pageSize * (pageNum + 1) - 1, page.Items.Last());
    }
    [TestMethod]
    public void PageExact()
    {
        const int pageNum = 1;
        const int pageSize = 30;
        const int dataSize = 60;
        var data = Enumerable.Range(0, dataSize).ToArray();
        var page = new Paginated<int>(pageSize, pageNum, data);
        Assert.IsTrue(page.Last);
        Assert.AreEqual(pageSize, page.Max);
        Assert.AreEqual(pageNum, page.Page);
        Assert.AreEqual(pageSize, page.Count);
        Assert.AreEqual(page.Items.Count, page.Count);
        Assert.AreEqual(pageSize * pageNum, page.Items.First());
        Assert.AreEqual(data.Last(), page.Items.Last());
    }
    [TestMethod]
    public void PageEmpty()
    {
        const int pageNum = 0;
        const int pageSize = 10;
        const int dataSize = 0;
        var data = Enumerable.Range(0, dataSize).ToArray();
        var page = new Paginated<int>(pageSize, pageNum, data);
        Assert.IsTrue(page.Last);
        Assert.AreEqual(pageSize, page.Max);
        Assert.AreEqual(pageNum, page.Page);
        Assert.AreEqual(page.Items.Count, page.Count);
        Assert.IsFalse(page.Items.Any());
    }
    [TestMethod]
    public void PageDoesntExist()
    {
        const int pageNum = 5;
        const int pageSize = 20;
        const int dataSize = 50;
        var data = Enumerable.Range(0, dataSize).ToArray();
        var page = new Paginated<int>(pageSize, pageNum, data);
        Assert.IsTrue(page.Last);
        Assert.AreEqual(pageSize, page.Max);
        Assert.AreEqual(pageNum, page.Page);
        Assert.AreEqual(page.Items.Count, page.Count);
        Assert.IsFalse(page.Items.Any());
    }
}