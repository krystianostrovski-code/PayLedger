using MediatR;
using Microsoft.AspNetCore.Mvc;
using PayLedger.Application.Commands;
using PayLedger.Application.DTOs;
using PayLedger.Application.Queries;
using PayLedger.Domain.Enums;

namespace PayLedger.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AccountsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<List<AccountDto>>> GetAccounts(
        [FromQuery] AccountType? type,
        [FromQuery] string? currency,
        [FromQuery] string? search)
    {
        var result = await _mediator.Send(new GetAccountsQuery(type, currency, search));
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AccountDetailsDto>> GetAccountDetails(Guid id)
    {
        var result = await _mediator.Send(new GetAccountDetailsQuery(id));
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<AccountDto>> CreateAccount([FromBody] CreateAccountCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetAccountDetails), new { id = result.Id }, result);
    }
}
