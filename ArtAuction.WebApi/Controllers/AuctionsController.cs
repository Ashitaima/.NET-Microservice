using ArtAuction.Application.Auctions.Commands.CreateAuction;
using ArtAuction.Application.Auctions.Commands.PlaceBid;
using ArtAuction.Application.Auctions.Queries.GetAuction;
using ArtAuction.Application.Auctions.Queries.GetActiveAuctions;
using ArtAuction.Application.Auctions.Queries.GetAllAuctions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtAuction.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuctionsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AuctionsController> _logger;

    public AuctionsController(IMediator mediator, ILogger<AuctionsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get auction by ID (Public - no authentication required)
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAuction(string id, CancellationToken cancellationToken)
    {
        var query = new GetAuctionQuery(id);
        var auction = await _mediator.Send(query, cancellationToken);

        if (auction == null)
            return NotFound(new { Message = $"Auction {id} not found" });

        return Ok(auction);
    }

    /// <summary>
    /// Get all auctions (Public - no authentication required)
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllAuctions(CancellationToken cancellationToken)
    {
        var query = new GetAllAuctionsQuery();
        var auctions = await _mediator.Send(query, cancellationToken);

        return Ok(auctions);
    }

    /// <summary>
    /// Get all active auctions (Public - no authentication required)
    /// </summary>
    [HttpGet("active")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveAuctions(CancellationToken cancellationToken)
    {
        var query = new GetActiveAuctionsQuery();
        var auctions = await _mediator.Send(query, cancellationToken);

        return Ok(auctions);
    }

    /// <summary>
    /// Create new auction (Requires authentication - User or Admin role)
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "UserOrAdmin")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateAuction(
        [FromBody] CreateAuctionCommand command,
        CancellationToken cancellationToken)
    {
        var auction = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetAuction),
            new { id = auction.Id },
            auction);
    }

    /// <summary>
    /// Place bid on auction (Requires authentication - User or Admin role)
    /// </summary>
    [HttpPost("{id}/bids")]
    [Authorize(Policy = "UserOrAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PlaceBid(
        string id,
        [FromBody] PlaceBidRequest request,
        CancellationToken cancellationToken)
    {
        var command = new PlaceBidCommand
        {
            AuctionId = id,
            BidderId = request.BidderId,
            BidAmount = request.BidAmount
        };

        var auction = await _mediator.Send(command, cancellationToken);

        return Ok(auction);
    }
}

public record PlaceBidRequest(string BidderId, decimal BidAmount);
