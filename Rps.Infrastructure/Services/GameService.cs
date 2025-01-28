using Microsoft.EntityFrameworkCore;
using Rps.Application.DTOs;
using Rps.Application.Interfaces;
using Rps.Domain.Entities;
using Rps.Domain.Enums;
using Rps.Infrastructure.Persistence;

namespace Rps.Infrastructure.Services;

public class GameService : IGameService
{
    private const decimal InitialBalance = 1000m;
    private readonly RpsDbContext _dbContext;

    public GameService(RpsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> RegisterUserAsync(string username)
    {
        ValidateUsername(username);
        await EnsureUsernameIsUniqueAsync(username);

        var user = CreateUser(username);
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        return user.Id;
    }

    public async Task<IEnumerable<OpenMatchDto>> GetOpenMatchesAsync()
    {
        return await FetchOpenMatchesAsync();
    }

    public async Task<int> CreateMatchAsync(int userId, decimal bet, string roomName)
    {
        var user = await GetUserByIdAsync(userId);
        await ValidateUserBalanceAsync(user, bet);
        await EnsureRoomNameIsUniqueAsync(roomName);

        var match = CreateMatch(userId, bet, roomName);
        _dbContext.MatchHistories.Add(match);
        await _dbContext.SaveChangesAsync();

        return match.Id;
    }

    public async Task JoinMatchAsync(string roomName, int userId)
    {
        var match = await GetActiveMatchByRoomNameAsync(roomName);
        ValidateMatchAvailability(match);

        match.Player2Id = userId;
        match.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
    }

    public async Task<GameStatusDto> MakeMoveAsync(string roomName, int userId, MoveType move)
    {
        var match = await GetActiveMatchByRoomNameAsync(roomName);
        ValidateUserParticipation(match, userId);

        await RecordMoveAsync(match, userId, move);
        await _dbContext.SaveChangesAsync();

        if (AreBothMovesMade(match))
        {
            return await CompleteMatchAsync(match);
        }

        return new GameStatusDto { Status = GameStatus.Waiting };
    }

    public async Task<decimal> GetBalanceAsync(int userId)
    {
        var user = await GetUserByIdAsync(userId);
        return user.Balance;
    }

    private static void ValidateUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Username cannot be empty.", nameof(username));
        }
    }

    private async Task EnsureUsernameIsUniqueAsync(string username)
    {
        var userExists = await _dbContext.Users.AnyAsync(u => u.Username == username);
        if (userExists)
        {
            throw new InvalidOperationException($"Username '{username}' already exists.");
        }
    }

    private User CreateUser(string username)
    {
        return new User
        {
            Username = username,
            Balance = InitialBalance,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private async Task<MatchHistory> GetActiveMatchByRoomNameAsync(string roomName)
    {
        var match = await _dbContext.MatchHistories
            .FirstOrDefaultAsync(m => m.RoomName == roomName && !m.IsEnded);

        if (match == null)
        {
            throw new KeyNotFoundException($"Room '{roomName}' not found or already ended.");
        }

        return match;
    }

    private void ValidateMatchAvailability(MatchHistory match)
    {
        if (match.Player2Id != null)
        {
            throw new InvalidOperationException($"Room '{match.RoomName}' is already full.");
        }
    }

    private async Task<User> GetUserByIdAsync(int userId)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {userId} not found.");
        }

        return user;
    }

    private static Task ValidateUserBalanceAsync(User user, decimal bet)
    {
        if (user.Balance < bet)
        {
            throw new InvalidOperationException("Insufficient balance to create a match.");
        }

        return Task.CompletedTask;
    }

    private async Task EnsureRoomNameIsUniqueAsync(string roomName)
    {
        var roomExists = await _dbContext.MatchHistories.AnyAsync(m => m.RoomName == roomName && !m.IsEnded);
        if (roomExists)
        {
            throw new InvalidOperationException($"Room name '{roomName}' is already taken.");
        }
    }

    private MatchHistory CreateMatch(int userId, decimal bet, string roomName)
    {
        return new MatchHistory
        {
            Player1Id = userId,
            RoomName = roomName,
            Bet = bet,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private void ValidateUserParticipation(MatchHistory match, int userId)
    {
        if (userId != match.Player1Id && userId != match.Player2Id)
        {
            throw new InvalidOperationException("User is not a participant of this match.");
        }
    }

    private async Task RecordMoveAsync(MatchHistory match, int userId, MoveType move)
    {
        if (userId == match.Player1Id)
        {
            match.Player1Move = move;
        }
        else
        {
            match.Player2Move = move;
        }

        match.UpdatedAt = DateTime.UtcNow;
        await Task.CompletedTask;
    }

    private bool AreBothMovesMade(MatchHistory match)
    {
        return match.Player1Move.HasValue && match.Player2Move.HasValue;
    }

    private async Task<GameStatusDto> CompleteMatchAsync(MatchHistory match)
    {
        var winnerId = DetermineWinner(match.Player1Id, match.Player2Id!.Value,
            match.Player1Move!.Value, match.Player2Move!.Value);

        match.IsEnded = true;
        match.WinnerId = winnerId;
        match.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        if (winnerId.HasValue)
        {
            await UpdateBalancesAndRecordTransactionsAsync(match, winnerId.Value);
        }

        return winnerId.HasValue
            ? new GameStatusDto { Status = GameStatus.Finished, WinnerId = winnerId }
            : new GameStatusDto { Status = GameStatus.Finished, WinnerId = null };
    }

    private int? DetermineWinner(int player1Id, int player2Id, MoveType player1Move,
        MoveType player2Move)
    {
        if (player1Move == player2Move)
        {
            return null;
        }

        var player1Wins = (player1Move == MoveType.Rock && player2Move == MoveType.Scissors)
                          || (player1Move == MoveType.Scissors && player2Move == MoveType.Paper)
                          || (player1Move == MoveType.Paper && player2Move == MoveType.Rock);

        return player1Wins
            ? player1Id
            : player2Id;
    }

    private async Task UpdateBalancesAndRecordTransactionsAsync(MatchHistory match, int winnerId)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            var winner = await _dbContext.Users.FindAsync(winnerId);
            var player1 = await _dbContext.Users.FindAsync(match.Player1Id);
            var player2 = await _dbContext.Users.FindAsync(match.Player2Id!.Value);

            if (winner == null || player1 == null || player2 == null)
            {
                throw new InvalidOperationException("One of the players was not found in the database.");
            }

            if (player1.Balance < match.Bet || player2.Balance < match.Bet)
            {
                throw new InvalidOperationException("One of the players does not have enough balance for the bet.");
            }

            player1.Balance -= match.Bet;
            player2.Balance -= match.Bet;

            winner.Balance += match.Bet * 2;

            var transactions = new List<GameTransaction>
            {
                new()
                {
                    FromUserId = player1.Id,
                    ToUserId = winner.Id,
                    Amount = match.Bet,
                    TransactionType = TransactionType.GameBet,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    FromUserId = player2.Id,
                    ToUserId = winner.Id,
                    Amount = match.Bet,
                    TransactionType = TransactionType.GameBet,
                    CreatedAt = DateTime.UtcNow
                }
            };

            _dbContext.GameTransactions.AddRange(transactions);
            await _dbContext.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task<List<OpenMatchDto>> FetchOpenMatchesAsync()
    {
        return await _dbContext.MatchHistories
            .Where(m => !m.IsEnded)
            .Select(m => new OpenMatchDto
            {
                RoomName = m.RoomName,
                Bet = m.Bet,
                IsWaiting = m.Player2Id == null
            })
            .ToListAsync();
    }
}
