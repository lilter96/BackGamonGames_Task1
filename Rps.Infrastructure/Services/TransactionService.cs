using Rps.Application.Interfaces;
using Rps.Domain.Entities;
using Rps.Domain.Enums;
using Rps.Infrastructure.Persistence;

namespace Rps.Infrastructure.Services;

public class TransactionService : ITransactionService
{
    private readonly RpsDbContext _context;

    public TransactionService(RpsDbContext context)
    {
        _context = context;
    }

    public async Task CreateTransactionAsync(int fromUserId, int toUserId, decimal amount)
    {
        var fromUser = await _context.Users.FindAsync(fromUserId);
        var toUser = await _context.Users.FindAsync(toUserId);

        if (fromUser == null || toUser == null)
        {
            throw new Exception("User(s) not found.");
        }

        if (fromUser.Balance < amount)
        {
            throw new Exception("Not enough balance.");
        }

        fromUser.Balance -= amount;
        toUser.Balance += amount;

        var transaction = new GameTransaction
        {
            FromUserId = fromUserId,
            ToUserId = toUserId,
            Amount = amount,
            TransactionType = TransactionType.UserToUser,
            CreatedAt = DateTime.UtcNow
        };

        await _context.GameTransactions.AddAsync(transaction);
        await _context.SaveChangesAsync();
    }
}
