using MediatR;
using WalletBankingAPI.Domain.Common;
using WalletBankingAPI.Domain.Repositories;

namespace WalletBankingAPI.Application.Queries.GetAllWallets;

public class GetAllWalletsQueryHandler(IWalletRepository walletRepository) : IRequestHandler<GetAllWalletsQuery, ApiResponse>
{
    public async Task<ApiResponse> Handle(GetAllWalletsQuery request, CancellationToken cancellationToken)
    {
        var wallets = await walletRepository.GetAllAsync(cancellationToken);
        return new ApiResponse
        {
            IsSuccessful = true,
            Message = "Success",
            ResponseCode = "00",
            StatusCode = 200,
            Data = wallets
        };
    }
}
