namespace Rps.Api.Models;

public class CreateMatchModel
{
    public int UserId { get; set; }

    public decimal Bet { get; set; }

    public string RoomName { get; set; } = null!;
}
