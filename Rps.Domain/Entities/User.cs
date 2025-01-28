namespace Rps.Domain.Entities;

public class User
{
    public int Id { get; set; }

    public string Username { get; set; } = null!;

    public decimal Balance { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
