using WalletBankingAPI.Domain.Common;
using WalletBankingAPI.Domain.Dto;
using WalletBankingAPI.Domain.Entities;

namespace WalletBankingAPI.Domain.Repositories;

/// <summary>
/// Repository interface for Wallet aggregate.
/// INDUSTRIAL PRACTICE: Interface lives in DOMAIN (not Infrastructure). Application depends on this
/// abstraction; Infrastructure implements it. This preserves the Dependency Rule and makes testing easy.
/// </summary>
public interface IWalletRepository
{
    Task<ApiResponse> CreateAsync(CreateWalletDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse> UpdateAsync(UpdateWalletDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse> DeleteAsync(DeleteWalletDto dto, CancellationToken cancellationToken = default);
    Task<Wallet?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Wallet>> GetAllAsync(CancellationToken cancellationToken = default);
}
