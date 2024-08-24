using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ComiServ.Entities
{
    /// <summary>
    /// This was originally made to remove Entity types that were being added to the Swagger schema.
    /// I found that there was a bug a `ProducesResponseTypeAttribute` that caused it, and this is
    /// no longer necessary. I changed Apply to a nop but am keeping this around as an example and
    /// in case I actually need something like this in the future.
    /// </summary>
    public class EntitySwaggerFilter : ISchemaFilter
    {
        public readonly static string[] FILTER = [
            nameof(Author),
            nameof(Comic),
            nameof(ComicAuthor),
            nameof(ComicTag),
            nameof(Cover),
            nameof(Tag)
        ];
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            return;
            foreach (var item in context.SchemaRepository.Schemas.Keys)
            {
                if (FILTER.Contains(item))
                {
                    context.SchemaRepository.Schemas.Remove(item);
                }
            }
        }
    }
}
