namespace WalletBankingAPI.Domain.Entities;

/// <summary>
/// Domain entity representing a Wallet.
/// PURITY: No persistence attributes (no [Key], no [BsonId]). Infrastructure maps this to DB.
/// MONEY: Balance is decimal to avoid floating-point rounding errors (industry standard for currency).
/// </summary>
public class Wallet
{
    public Guid Id { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    /// <summary>Soft delete: when true, wallet is treated as removed but data is retained for audit.</summary>
    public bool IsDeleted { get; set; }
}
