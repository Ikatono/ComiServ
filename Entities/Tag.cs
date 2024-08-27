using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ComiServ.Entities
{
    [Index(nameof(Name), IsUnique = true)]
    public class Tag
    {
        //[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = null!;
        public ICollection<ComicTag> ComicTags { get; set; } = null!;
    }
}
