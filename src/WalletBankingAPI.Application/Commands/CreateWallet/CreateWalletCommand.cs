using MediatR;
using WalletBankingAPI.Domain.Common;
using WalletBankingAPI.Domain.Dto;

namespace WalletBankingAPI.Application.Commands.CreateWallet;

/// <summary>
/// CQRS COMMAND: Create a new wallet.
/// Implements IRequest of ApiResponse so MediatR routes to CreateWalletCommandHandler.
/// </summary>
public class CreateWalletCommand : CreateWalletDto, IRequest<ApiResponse>
{
}
