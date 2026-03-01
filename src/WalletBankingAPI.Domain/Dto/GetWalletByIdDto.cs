namespace WalletBankingAPI.Domain.Dto;

/// <summary>
/// Input for retrieving a single wallet by ID (Phase 1 - Get Wallet).
/// </summary>
public class GetWalletByIdDto
{
    public Guid Id { get; set; }
}
