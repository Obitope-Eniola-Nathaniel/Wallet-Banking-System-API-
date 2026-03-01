namespace WalletBankingAPI.Domain.Dto;

/// <summary>
/// Input for deleting a wallet (Phase 1 - Delete Wallet).
/// Soft delete is recommended: set IsDeleted = true instead of physical delete (audit/compliance).
/// </summary>
public class DeleteWalletDto
{
    public Guid Id { get; set; }
}
