using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;

namespace ComiServ.Entities;

[PrimaryKey(nameof(Id))]
[Index(nameof(Username), IsUnique = true)]
public class User
{
    public const int HashLengthBytes = 512 / 8;
    public const int SaltLengthBytes = HashLengthBytes;
    public int Id { get; set; }
    [MaxLength(20)]
    public string Username { get; set; }
    [MaxLength(SaltLengthBytes)]
    public byte[] Salt { get; set; }
    [MaxLength(HashLengthBytes)]
    public byte[] HashedPassword { get; set; }
    public UserType UserType { get; set; }
    public UserTypeEnum UserTypeId { get; set; }
    [InverseProperty("User")]
    public ICollection<ComicRead> ComicsRead { get; set; } = [];
    //cryptography should probably be in a different class
    public static byte[] MakeSalt()
    {
        byte[] arr = new byte[SaltLengthBytes];
        RandomNumberGenerator.Fill(new Span<byte>(arr));
        return arr;
    }
    public static byte[] Hash(byte[] password, byte[] salt)
    {
        var salted = salt.Append((byte)':').Concat(password).ToArray();
        return SHA512.HashData(salted);
    }
}
