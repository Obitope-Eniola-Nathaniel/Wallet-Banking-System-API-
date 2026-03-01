using MediatR;
using WalletBankingAPI.Domain.Common;

namespace WalletBankingAPI.Application.Queries.GetWalletById;

/// <summary>
/// CQRS QUERY: Get a single wallet by ID. Returns ApiResponse with Data = Wallet or null (404).
/// Queries do not change state; they are read-only.
/// </summary>
public class GetWalletByIdQuery : IRequest<ApiResponse>
{
    public Guid Id { get; set; }
}
