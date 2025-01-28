using Rps.Domain.Enums;

namespace Rps.Domain.Entities;

public class GameTransaction
{
    public int Id { get; set; }

    public int? FromUserId { get; set; }

    public int ToUserId { get; set; }

    public decimal Amount { get; set; }

    public TransactionType TransactionType { get; set; }

    public DateTime CreatedAt { get; set; }
}
