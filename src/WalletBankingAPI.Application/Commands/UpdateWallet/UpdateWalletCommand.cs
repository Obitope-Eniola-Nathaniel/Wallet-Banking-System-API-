using MediatR;
using WalletBankingAPI.Domain.Common;
using WalletBankingAPI.Domain.Dto;

namespace WalletBankingAPI.Application.Commands.UpdateWallet;

/// <summary>
/// CQRS COMMAND: Update wallet owner info (Phase 1).
/// </summary>
public class UpdateWalletCommand : UpdateWalletDto, IRequest<ApiResponse>
{
}
