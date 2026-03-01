using MediatR;
using WalletBankingAPI.Domain.Common;
using WalletBankingAPI.Domain.Repositories;

namespace WalletBankingAPI.Application.Queries.GetWalletById;

public class GetWalletByIdQueryHandler(IWalletRepository walletRepository) : IRequestHandler<GetWalletByIdQuery, ApiResponse>
{
    public async Task<ApiResponse> Handle(GetWalletByIdQuery request, CancellationToken cancellationToken)
    {
        var wallet = await walletRepository.GetByIdAsync(request.Id, cancellationToken);
        if (wallet is null)
            return new ApiResponse
            {
                IsSuccessful = false,
                Message = "Wallet not found.",
                ResponseCode = "NOT_FOUND",
                StatusCode = 404
            };

        return new ApiResponse
        {
            IsSuccessful = true,
            Message = "Success",
            ResponseCode = "00",
            StatusCode = 200,
            Data = wallet
        };
    }
}
