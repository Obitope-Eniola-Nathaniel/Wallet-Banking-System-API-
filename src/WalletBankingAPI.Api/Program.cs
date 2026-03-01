using Microsoft.OpenApi.Models;
using WalletBankingAPI.Api.Extensions;
using WalletBankingAPI.Application.Commands.CreateWallet;
using WalletBankingAPI.Domain.Repositories;
using WalletBankingAPI.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IWalletRepository, WalletRepository>();
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(CreateWalletCommand).Assembly);
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Wallet / Banking API",
        Version = "v1",
        Description = "Phase 1: CRUD Wallets. Clean Architecture + CQRS + MediatR."
    });
});

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
