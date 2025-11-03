using AuctionService.Bll.DTOs;

namespace AuctionService.Bll.Interfaces;

public interface IBidService
{
    Task<BidDto?> GetByIdAsync(long bidId);
    Task<IEnumerable<BidDto>> GetByAuctionIdAsync(long auctionId);
    Task<IEnumerable<BidDto>> GetByUserIdAsync(long userId);
    Task<bool> PlaceBidAsync(PlaceBidDto dto);
}
