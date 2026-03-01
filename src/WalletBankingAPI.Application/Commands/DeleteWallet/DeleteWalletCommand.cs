using MediatR;
using WalletBankingAPI.Domain.Common;
using WalletBankingAPI.Domain.Dto;

namespace WalletBankingAPI.Application.Commands.DeleteWallet;

/// <summary>
/// CQRS COMMAND: Delete wallet (soft delete recommended in PRD).
/// </summary>
public class DeleteWalletCommand : DeleteWalletDto, IRequest<ApiResponse>
{
}
