using Microsoft.EntityFrameworkCore;
using ComiServ.Entities;

namespace ComiServ
{
    public class ComicsContext : DbContext
    {
        //TODO is this the best place for this to live?
        public const int HANDLE_LENGTH = 12;
        //relies on low probability of repeat handles in a short period of time
        //duplicate handles could be created before either of them are commited
        public string CreateHandle()
        {
            char ToChar(int i)
            {
                if (i < 10)
                    return (char)('0' + i);
                if (i - 10 + 'A' < 'O')
                    return (char)('A' + i - 10);
                else
                    //skip 'O'
                    return (char)('A' + i - 9);
            }
            string handle = "";
            do
            {
                handle = string.Join("", Enumerable.Repeat(0, HANDLE_LENGTH)
                .Select(_ => ToChar(Random.Shared.Next(0, 35))));
            } while (Comics.Any(c => c.Handle == handle));
            return handle;
        }
        public DbSet<Comic> Comics { get; set; }
        public DbSet<ComicTag> ComicTags { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<ComicAuthor> ComicAuthors { get; set; }
        public DbSet<Author> Authors { get; set; }
        public DbSet<Cover> Covers { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserType> UserTypes { get; set; }
        public DbSet<ComicRead> ComicsRead { get; set; }
        public ComicsContext(DbContextOptions<ComicsContext> options)
            : base(options)
        {

        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Comic>().ToTable("Comics");
            modelBuilder.Entity<ComicTag>().ToTable("ComicTags");
            modelBuilder.Entity<Tag>().ToTable("Tags");
            modelBuilder.Entity<ComicAuthor>().ToTable("ComicAuthors");
            modelBuilder.Entity<Author>().ToTable("Authors");
            modelBuilder.Entity<Cover>().ToTable("Covers");
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<UserType>().ToTable("UserTypes")
                .HasData(
                    Enum.GetValues(typeof(UserTypeEnum))
                        .Cast<UserTypeEnum>()
                        .Select(e => new UserType()
                        {
                            Id = e,
                            Name = e.ToString()
                        })
                );
        }
        /// <summary>
        /// puts a user-provided handle into the proper form
        /// </summary>
        /// <param name="handle"></param>
        /// <returns>formatted handle or null if invalid</returns>
        public static string? CleanValidateHandle(string? handle)
        {
            if (handle is null)
                return null;
            handle = handle.Trim();
            if (handle.Length != HANDLE_LENGTH)
                return null;
            return handle.ToUpper();
        }
    }
}
