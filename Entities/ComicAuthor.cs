using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ComiServ.Entities
{
    [PrimaryKey("ComicId", "AuthorId")]
    [Index("ComicId")]
    [Index("AuthorId")]
    public class ComicAuthor
    {
        [ForeignKey(nameof(Comic))]
        public int ComicId { get; set; }
        [Required]
        public Comic Comic { get; set; } = null!;
        [ForeignKey(nameof(Author))]
        public int AuthorId { get; set; }
        [Required]
        public Author Author { get; set; } = null!;
    }
}
