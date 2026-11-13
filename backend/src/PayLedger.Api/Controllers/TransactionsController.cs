using MediatR;
using Microsoft.AspNetCore.Mvc;
using PayLedger.Api.Middleware;
using PayLedger.Application.Commands;
using PayLedger.Application.DTOs;
using PayLedger.Application.Queries;
using PayLedger.Domain.Enums;

namespace PayLedger.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransactionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TransactionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<List<LedgerTransactionDto>>> GetTransactions(
        [FromQuery] Guid? accountId,
        [FromQuery] TransactionType? type,
        [FromQuery] TransactionStatus? status,
        [FromQuery] int limit = 50)
    {
        var result = await _mediator.Send(new GetTransactionsQuery(accountId, type, status, limit));
        return Ok(result);
    }

    [HttpPost("transfer")]
    public async Task<ActionResult<LedgerTransactionDto>> Transfer([FromBody] ExecuteTransferCommand command)
    {
        var idempotencyKey = ExtractIdempotencyKey();
        var cmd = command with { IdempotencyKey = idempotencyKey ?? command.IdempotencyKey };
        var result = await _mediator.Send(cmd);
        return Ok(result);
    }

    [HttpPost("deposit")]
    public async Task<ActionResult<LedgerTransactionDto>> Deposit([FromBody] ExecuteDepositCommand command)
    {
        var idempotencyKey = ExtractIdempotencyKey();
        var cmd = command with { IdempotencyKey = idempotencyKey ?? command.IdempotencyKey };
        var result = await _mediator.Send(cmd);
        return Ok(result);
    }

    [HttpPost("withdraw")]
    public async Task<ActionResult<LedgerTransactionDto>> Withdraw([FromBody] ExecuteWithdrawalCommand command)
    {
        var idempotencyKey = ExtractIdempotencyKey();
        var cmd = command with { IdempotencyKey = idempotencyKey ?? command.IdempotencyKey };
        var result = await _mediator.Send(cmd);
        return Ok(result);
    }

    [HttpPost("exchange")]
    public async Task<ActionResult<LedgerTransactionDto>> Exchange([FromBody] ExecuteExchangeCommand command)
    {
        var idempotencyKey = ExtractIdempotencyKey();
        var cmd = command with { IdempotencyKey = idempotencyKey ?? command.IdempotencyKey };
        var result = await _mediator.Send(cmd);
        return Ok(result);
    }

    [HttpPost("{id:guid}/reverse")]
    public async Task<ActionResult<LedgerTransactionDto>> Reverse(Guid id, [FromBody] ReverseRequest request)
    {
        var result = await _mediator.Send(new ReverseTransactionCommand(id, request.Reason));
        return Ok(result);
    }

    private string? ExtractIdempotencyKey()
    {
        return Request.Headers.TryGetValue(IdempotencyMiddleware.IdempotencyHeader, out var val)
            ? val.ToString().Trim()
            : null;
    }
}

public record ReverseRequest(string Reason);
