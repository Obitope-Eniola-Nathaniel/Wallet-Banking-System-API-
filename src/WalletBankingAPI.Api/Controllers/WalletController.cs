using MediatR;
using Microsoft.AspNetCore.Mvc;
using WalletBankingAPI.Application.Commands.CreateWallet;
using WalletBankingAPI.Application.Commands.DeleteWallet;
using WalletBankingAPI.Application.Commands.UpdateWallet;
using WalletBankingAPI.Application.Queries.GetAllWallets;
using WalletBankingAPI.Application.Queries.GetWalletById;

namespace WalletBankingAPI.Api.Controllers;

/// <summary>
/// Wallet CRUD endpoints (Phase 1). Controllers are thin: they only send commands/queries via MediatR.
/// No repository or business logic here — that's what recruiters and senior devs look for (Clean Architecture).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class WalletController(IMediator mediator) : ControllerBase
{
    /// <summary>Create a new wallet with owner name and initial balance.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateWalletCommand command, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return result.IsSuccessful ? Ok(result) : BadRequest(result);
    }

    /// <summary>Update wallet owner info.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWalletRequest body, CancellationToken cancellationToken)
    {
        var command = new UpdateWalletCommand { Id = id, OwnerName = body.OwnerName };
        var result = await mediator.Send(command, cancellationToken);
        return result.IsSuccessful ? Ok(result) : NotFound(result);
    }

    /// <summary>Soft-delete a wallet.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var command = new DeleteWalletCommand { Id = id };
        var result = await mediator.Send(command, cancellationToken);
        return result.IsSuccessful ? Ok(result) : NotFound(result);
    }

    /// <summary>Get a single wallet by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetWalletByIdQuery { Id = id };
        var result = await mediator.Send(query, cancellationToken);
        return result.IsSuccessful ? Ok(result) : NotFound(result);
    }

    /// <summary>List all wallets (non-deleted). Pagination can be added in a later phase.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetAllWalletsQuery(), cancellationToken);
        return Ok(result);
    }
}

/// <summary>Request body for Update Wallet (owner name only).</summary>
public record UpdateWalletRequest(string OwnerName);
