using MediatR;
using Microsoft.AspNetCore.Mvc;
using PayLedger.Application.DTOs;
using PayLedger.Application.Interfaces;
using PayLedger.Application.Queries;

namespace PayLedger.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LedgerController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IExchangeRateService _exchangeRateService;

    public LedgerController(IMediator mediator, IExchangeRateService exchangeRateService)
    {
        _mediator = mediator;
        _exchangeRateService = exchangeRateService;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<LedgerSummaryDto>> GetSummary()
    {
        var result = await _mediator.Send(new GetLedgerSummaryQuery());
        return Ok(result);
    }

    [HttpGet("integrity")]
    public async Task<ActionResult<LedgerIntegrityReportDto>> CheckIntegrity()
    {
        var result = await _mediator.Send(new VerifyLedgerIntegrityQuery());
        return Ok(result);
    }

    [HttpGet("rates")]
    public ActionResult<Dictionary<string, decimal>> GetRates()
    {
        var rates = _exchangeRateService.GetRatesAgainstPln();
        return Ok(rates);
    }
}
