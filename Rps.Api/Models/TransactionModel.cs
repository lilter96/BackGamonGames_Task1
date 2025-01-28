namespace Rps.Api.Models;

public class TransactionModel
{
    public int FromUserId { get; set; }
    public int ToUserId { get; set; }
    public decimal Amount { get; set; }
}