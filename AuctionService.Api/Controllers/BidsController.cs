using Microsoft.AspNetCore.Mvc;
using AuctionService.Bll.Interfaces;
using AuctionService.Bll.DTOs;

namespace AuctionService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class BidsController : ControllerBase
{
    private readonly IBidService _bidService;
    private readonly ILogger<BidsController> _logger;

    public BidsController(IBidService bidService, ILogger<BidsController> logger)
    {
        _bidService = bidService ?? throw new ArgumentNullException(nameof(bidService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Отримати ставку за ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(BidDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BidDto>> GetById(long id)
    {
        var bid = await _bidService.GetByIdAsync(id);
        
        if (bid == null)
        {
            return NotFound(new { Message = $"Bid with ID {id} not found" });
        }

        return Ok(bid);
    }

    /// <summary>
    /// Отримати всі ставки для аукціону
    /// </summary>
    [HttpGet("auction/{auctionId}")]
    [ProducesResponseType(typeof(IEnumerable<BidDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<BidDto>>> GetByAuctionId(long auctionId)
    {
        var bids = await _bidService.GetByAuctionIdAsync(auctionId);
        return Ok(bids);
    }

    /// <summary>
    /// Отримати всі ставки користувача
    /// </summary>
    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(IEnumerable<BidDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<BidDto>>> GetByUserId(long userId)
    {
        var bids = await _bidService.GetByUserIdAsync(userId);
        return Ok(bids);
    }

    /// <summary>
    /// Розмістити ставку на аукціоні
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PlaceBid([FromBody] PlaceBidDto dto)
    {
        try
        {
            var success = await _bidService.PlaceBidAsync(dto);
            
            if (!success)
            {
                return Conflict(new { Message = "Failed to place bid. Auction may have ended or bid is too low." });
            }

            return CreatedAtAction(nameof(GetByAuctionId), new { auctionId = dto.AuctionId }, null);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error placing bid");
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }
}
