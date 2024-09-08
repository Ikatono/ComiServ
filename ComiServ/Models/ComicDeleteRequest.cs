namespace ComiServ.Models;

//handle is taken from URL
public record class ComicDeleteRequest
(
    bool DeleteIfFileExists
);
