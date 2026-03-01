using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using WalletBankingAPI.Domain.Common;
using WalletBankingAPI.Domain.Dto;
using WalletBankingAPI.Domain.Entities;
using WalletBankingAPI.Domain.Repositories;
using WalletBankingAPI.Infrastructure.Persistence;

namespace WalletBankingAPI.Infrastructure.Repositories;

/// <summary>
/// PostgreSQL implementation of IWalletRepository.
/// Uses Dapper for simple, fast queries. Connection string from IConfiguration (e.g. appsettings or env).
/// </summary>
public class WalletRepository(IConfiguration configuration) : IWalletRepository
{
    private readonly string _connectionString = configuration.GetConnectionString("PostgreSQL")
        ?? throw new InvalidOperationException("Connection string 'PostgreSQL' not found.");

    public async Task<ApiResponse> CreateAsync(CreateWalletDto dto, CancellationToken cancellationToken = default)
    {
        var wallet = new Wallet
        {
            Id = Guid.NewGuid(),
            OwnerName = dto.OwnerName,
            Balance = dto.InitialBalance,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await conn.ExecuteAsync(
            new CommandDefinition(WalletSql.Create, wallet, cancellationToken: cancellationToken));

        return new ApiResponse
        {
            IsSuccessful = true,
            Message = "Wallet created successfully.",
            ResponseCode = "00",
            StatusCode = 200,
            Data = new { wallet.Id, wallet.OwnerName, wallet.Balance, wallet.CreatedAt }
        };
    }

    public async Task<ApiResponse> UpdateAsync(UpdateWalletDto dto, CancellationToken cancellationToken = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        var updated = await conn.ExecuteAsync(
            new CommandDefinition(WalletSql.Update, new { dto.Id, dto.OwnerName, UpdatedAt = DateTime.UtcNow }, cancellationToken: cancellationToken));

        if (updated == 0)
            return new ApiResponse
            {
                IsSuccessful = false,
                Message = "Wallet not found or already deleted.",
                ResponseCode = "NOT_FOUND",
                StatusCode = 404
            };

        return new ApiResponse
        {
            IsSuccessful = true,
            Message = "Wallet updated successfully.",
            ResponseCode = "00",
            StatusCode = 200
        };
    }

    public async Task<ApiResponse> DeleteAsync(DeleteWalletDto dto, CancellationToken cancellationToken = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        var updated = await conn.ExecuteAsync(
            new CommandDefinition(WalletSql.SoftDelete, new { dto.Id, UpdatedAt = DateTime.UtcNow }, cancellationToken: cancellationToken));

        if (updated == 0)
            return new ApiResponse
            {
                IsSuccessful = false,
                Message = "Wallet not found or already deleted.",
                ResponseCode = "NOT_FOUND",
                StatusCode = 404
            };

        return new ApiResponse
        {
            IsSuccessful = true,
            Message = "Wallet deleted successfully.",
            ResponseCode = "00",
            StatusCode = 200
        };
    }

    public async Task<Wallet?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        return await conn.QuerySingleOrDefaultAsync<Wallet>(
            new CommandDefinition(WalletSql.GetById, new { Id = id }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<Wallet>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        var list = await conn.QueryAsync<Wallet>(
            new CommandDefinition(WalletSql.GetAll, cancellationToken: cancellationToken));
        return list.ToList();
    }
}
