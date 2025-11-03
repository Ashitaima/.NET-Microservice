using Microsoft.AspNetCore.Mvc;
using AuctionService.Bll.Interfaces;
using AuctionService.Bll.DTOs;

namespace AuctionService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuctionsController : ControllerBase
{
    private readonly IAuctionService _auctionService;
    private readonly ILogger<AuctionsController> _logger;

    public AuctionsController(IAuctionService auctionService, ILogger<AuctionsController> logger)
    {
        _auctionService = auctionService ?? throw new ArgumentNullException(nameof(auctionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Отримати всі аукціони
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AuctionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AuctionDto>>> GetAll()
    {
        var auctions = await _auctionService.GetAllAsync();
        return Ok(auctions);
    }

    /// <summary>
    /// Отримати активні аукціони
    /// </summary>
    [HttpGet("active")]
    [ProducesResponseType(typeof(IEnumerable<AuctionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AuctionDto>>> GetActive()
    {
        var auctions = await _auctionService.GetActiveAuctionsAsync();
        return Ok(auctions);
    }

    /// <summary>
    /// Отримати аукціон за ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(AuctionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AuctionDto>> GetById(long id)
    {
        var auction = await _auctionService.GetByIdAsync(id);
        
        if (auction == null)
        {
            return NotFound(new { Message = $"Auction with ID {id} not found" });
        }

        return Ok(auction);
    }

    /// <summary>
    /// Створити новий аукціон
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(long), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<long>> Create([FromBody] CreateAuctionDto dto)
    {
        try
        {
            var id = await _auctionService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id }, id);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating auction");
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Оновити аукціон
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(long id, [FromBody] AuctionDto dto)
    {
        try
        {
            var updated = await _auctionService.UpdateAsync(id, dto);
            
            if (!updated)
            {
                return NotFound(new { Message = $"Auction with ID {id} not found" });
            }

            return NoContent();
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
            _logger.LogError(ex, "Error updating auction {AuctionId}", id);
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Видалити аукціон
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id)
    {
        try
        {
            var deleted = await _auctionService.DeleteAsync(id);
            
            if (!deleted)
            {
                return NotFound(new { Message = $"Auction with ID {id} not found" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting auction {AuctionId}", id);
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }
}
