namespace ComiServ.Models;

public class ComicMetadataUpdateRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public List<string>? Tags { get; set; }
    public List<string>? Authors { get; set; }
}
