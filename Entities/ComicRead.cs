using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace ComiServ.Entities
{
    [PrimaryKey(nameof(UserId), nameof(ComicId))]
    [Index(nameof(UserId))]
    [Index(nameof(ComicId))]
    public class ComicRead
    {
        public int UserId { get; set; }
        public User User { get; set; }
        public int ComicId { get; set; }
        public Comic Comic { get; set; }
    }
}
