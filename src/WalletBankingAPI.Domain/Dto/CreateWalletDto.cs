namespace WalletBankingAPI.Domain.Dto;

/// <summary>
/// Input for creating a new wallet (Phase 1 - Create Wallet).
/// DTOs keep API contract separate from Domain entity; validation can be applied here or in Application.
/// </summary>
public class CreateWalletDto
{
    public string OwnerName { get; set; } = string.Empty;
    /// <summary>Initial balance; must be >= 0 (enforced in handler/domain).</summary>
    public decimal InitialBalance { get; set; }
}
