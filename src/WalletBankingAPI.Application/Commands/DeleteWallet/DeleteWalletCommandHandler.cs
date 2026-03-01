using MediatR;
using WalletBankingAPI.Domain.Common;
using WalletBankingAPI.Domain.Repositories;

namespace WalletBankingAPI.Application.Commands.DeleteWallet;

public class DeleteWalletCommandHandler(IWalletRepository walletRepository) : IRequestHandler<DeleteWalletCommand, ApiResponse>
{
    public async Task<ApiResponse> Handle(DeleteWalletCommand request, CancellationToken cancellationToken)
    {
        return await walletRepository.DeleteAsync(request, cancellationToken);
    }
}
