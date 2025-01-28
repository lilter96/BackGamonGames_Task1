namespace Rps.Client
{
    public record CommandResult(string Message, int MatchId);

    public record OpenMatchDto(string RoomName, decimal Bet, bool IsWaiting);
}
