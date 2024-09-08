using Microsoft.EntityFrameworkCore;

namespace ComiServ.Entities;

[PrimaryKey("FileXxhash64")]
public class Cover
{
    public long FileXxhash64 { get; set; }
    public string Filename { get; set; } = null!;
    public byte[] CoverFile { get; set; } = null!;
}
