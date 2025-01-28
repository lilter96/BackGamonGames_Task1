using Rps.Application.DTOs;
using Rps.Domain.Enums;

namespace Rps.Application.Interfaces;

public interface IGameService
{
    Task<int> RegisterUserAsync(string username);

    Task<IEnumerable<OpenMatchDto>> GetOpenMatchesAsync();

    Task<int> CreateMatchAsync(int userId, decimal bet, string roomName);

    Task JoinMatchAsync(string roomName, int userId);

    Task<GameStatusDto> MakeMoveAsync(string roomName, int userId, MoveType move);

    Task<decimal> GetBalanceAsync(int userId);
}
