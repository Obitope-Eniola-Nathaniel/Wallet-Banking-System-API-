using MediatR;
using WalletBankingAPI.Domain.Common;
using WalletBankingAPI.Domain.Repositories;

namespace WalletBankingAPI.Application.Commands.CreateWallet;

/// <summary>
/// Handles CreateWalletCommand: delegates to repository.
/// Single responsibility: orchestrate the use case; business rules (e.g. InitialBalance >= 0) can live here or in Domain.
/// </summary>
public class CreateWalletCommandHandler(IWalletRepository walletRepository) : IRequestHandler<CreateWalletCommand, ApiResponse>
{
    public async Task<ApiResponse> Handle(CreateWalletCommand request, CancellationToken cancellationToken)
    {
        if (request.InitialBalance < 0)
            return new ApiResponse
            {
                IsSuccessful = false,
                Message = "Initial balance cannot be negative.",
                ResponseCode = "VALIDATION",
                StatusCode = 400
            };

        return await walletRepository.CreateAsync(request, cancellationToken);
    }
}
