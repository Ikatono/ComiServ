namespace ComiServ.Extensions;

public static class StreamExtensions
{
    //https://stackoverflow.com/questions/1080442/how-do-i-convert-a-stream-into-a-byte-in-c
    //https://archive.ph/QUKys
    public static byte[] ReadAllBytes(this Stream instream)
    {
        if (instream is MemoryStream)
            return ((MemoryStream)instream).ToArray();

        using var memoryStream = new MemoryStream();
        instream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }
    public static async Task<byte[]> ReadAllBytesAsync(this Stream instream)
    {
        if (instream is MemoryStream)
            return ((MemoryStream)instream).ToArray();

        using var memoryStream = new MemoryStream();
        await instream.CopyToAsync(memoryStream);
        return memoryStream.ToArray();
    }
}
