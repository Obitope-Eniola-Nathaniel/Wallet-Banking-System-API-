namespace WalletBankingAPI.Domain.Dto;

/// <summary>
/// Input for updating wallet owner info (Phase 1 - Update Wallet).
/// We do not expose Balance in update for Phase 1; balance changes happen via Deposit/Withdraw (Phase 2).
/// </summary>
public class UpdateWalletDto
{
    public Guid Id { get; set; }
    public string OwnerName { get; set; } = string.Empty;
}
