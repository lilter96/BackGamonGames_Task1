using Rps.Domain.Enums;

namespace Rps.Domain.Entities;

public class MatchHistory
{
    public int Id { get; set; }

    public string RoomName { get; set; } = null!;

    public decimal Bet { get; set; }

    public int Player1Id { get; set; }

    public int? Player2Id { get; set; }

    public bool IsEnded { get; set; }

    public int? WinnerId { get; set; }

    public MoveType? Player1Move { get; set; }

    public MoveType? Player2Move { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
