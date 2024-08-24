using ComiServ.Controllers;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ComiServ.Entities
{
    [Index(nameof(Handle), IsUnique = true)]
    [Index(nameof(Filepath), IsUnique = true)]
    public class Comic
    {
        public int Id { get; set; }
        public bool Exists { get; set; }
        //id exposed through the API
        [Required]
        [StringLength(ComicsContext.HANDLE_LENGTH)]
        public string Handle { get; set; } = null!;
        [Required]
        public string Filepath { get; set; } = null!;
        [Required]
        public string Title { get; set; } = null!;
        [Required]
        public string Description { get; set; } = null!;
        public int PageCount { get; set; }
        public long SizeBytes { get; set; }
        public long FileXxhash64 { get; set; }
        [InverseProperty("Comic")]
        public ICollection<ComicTag> ComicTags { get; set; } = [];
        [InverseProperty("Comic")]
        public ICollection<ComicAuthor> ComicAuthors { get; set; } = [];
    }
}
