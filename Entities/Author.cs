using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
//using System.ComponentModel.DataAnnotations.Schema;

namespace ComiServ.Entities
{
    [Index(nameof(Name), IsUnique = true)]
    public class Author
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = null!;
        public ICollection<ComicAuthor> ComicAuthors { get; set; } = null!;
    }
}
