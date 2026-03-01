namespace WalletBankingAPI.Infrastructure.Persistence;

/// <summary>
/// SQL for Wallet table. Using quoted identifiers for case-sensitive column names (e.g. "OwnerName").
/// Table creation script is in /database/ or docs so DB can be set up separately (e.g. migrations later).
/// </summary>
public static class WalletSql
{
    public const string Create = """
        INSERT INTO "Wallet" ("Id", "OwnerName", "Balance", "CreatedAt", "UpdatedAt", "IsDeleted")
        VALUES (@Id, @OwnerName, @Balance, @CreatedAt, @UpdatedAt, @IsDeleted)
        """;

    public const string Update = """
        UPDATE "Wallet"
        SET "OwnerName" = @OwnerName, "UpdatedAt" = @UpdatedAt
        WHERE "Id" = @Id AND "IsDeleted" = false
        """;

    /// <summary>Soft delete: set IsDeleted = true, UpdatedAt = now.</summary>
    public const string SoftDelete = """
        UPDATE "Wallet"
        SET "IsDeleted" = true, "UpdatedAt" = @UpdatedAt
        WHERE "Id" = @Id AND "IsDeleted" = false
        """;

    public const string GetById = """
        SELECT "Id", "OwnerName", "Balance", "CreatedAt", "UpdatedAt", "IsDeleted"
        FROM "Wallet"
        WHERE "Id" = @Id AND "IsDeleted" = false
        """;

    public const string GetAll = """
        SELECT "Id", "OwnerName", "Balance", "CreatedAt", "UpdatedAt", "IsDeleted"
        FROM "Wallet"
        WHERE "IsDeleted" = false
        ORDER BY "CreatedAt" DESC
        """;
}
