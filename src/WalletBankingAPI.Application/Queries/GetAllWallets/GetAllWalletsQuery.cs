using MediatR;
using WalletBankingAPI.Domain.Common;

namespace WalletBankingAPI.Application.Queries.GetAllWallets;

/// <summary>
/// CQRS QUERY: List all wallets (Phase 1 - List Wallets).
/// Later phases can add pagination (e.g. PageNumber, PageSize) here.
/// </summary>
public class GetAllWalletsQuery : IRequest<ApiResponse>
{
}
