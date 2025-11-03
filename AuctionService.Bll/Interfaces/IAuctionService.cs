using AuctionService.Bll.DTOs;

namespace AuctionService.Bll.Interfaces;

public interface IAuctionService
{
    Task<AuctionDto?> GetByIdAsync(long auctionId);
    Task<IEnumerable<AuctionDto>> GetAllAsync();
    Task<IEnumerable<AuctionDto>> GetActiveAuctionsAsync();
    Task<long> CreateAsync(CreateAuctionDto dto);
    Task<bool> UpdateAsync(long auctionId, AuctionDto dto);
    Task<bool> DeleteAsync(long auctionId);
}
