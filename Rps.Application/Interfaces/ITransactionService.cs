namespace Rps.Application.Interfaces;

public interface ITransactionService
{
    Task CreateTransactionAsync(int fromUserId, int toUserId, decimal amount);
}
