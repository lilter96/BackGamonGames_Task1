namespace Rps.Application.DTOs;

public class OpenMatchDto
{
    public string RoomName { get; set; }

    public decimal Bet { get; set; }

    public bool IsWaiting { get; set; }

    public void Deconstruct(out string room, out decimal bet, out bool isWait)
    {
        room = RoomName;
        bet = Bet;
        isWait = IsWaiting;
    }
}
