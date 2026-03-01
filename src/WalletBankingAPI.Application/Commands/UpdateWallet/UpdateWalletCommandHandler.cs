using MediatR;
using WalletBankingAPI.Domain.Common;
using WalletBankingAPI.Domain.Repositories;

namespace WalletBankingAPI.Application.Commands.UpdateWallet;

public class UpdateWalletCommandHandler(IWalletRepository walletRepository) : IRequestHandler<UpdateWalletCommand, ApiResponse>
{
    public async Task<ApiResponse> Handle(UpdateWalletCommand request, CancellationToken cancellationToken)
    {
        return await walletRepository.UpdateAsync(request, cancellationToken);
    }
}
