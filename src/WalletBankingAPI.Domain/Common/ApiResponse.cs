namespace WalletBankingAPI.Domain.Common;

/// <summary>
/// Standard API response wrapper used across the application.
/// Enables consistent response shape for success and failure (helps frontend and API consumers).
/// </summary>
public class ApiResponse
{
    public int StatusCode { get; set; } = 500;
    public bool IsSuccessful { get; set; }
    public string? Message { get; set; }
    public string? ResponseCode { get; set; } = "99";
    public object? Data { get; set; }
    public DateTime ResponseTime { get; set; } = DateTime.UtcNow;
}
