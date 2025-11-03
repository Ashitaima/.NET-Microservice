using AutoMapper;
using AuctionService.Bll.DTOs;
using AuctionService.Bll.Interfaces;
using AuctionService.Dal.Interfaces;

namespace AuctionService.Bll.Services;

public class BidBllService : IBidService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public BidBllService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<BidDto?> GetByIdAsync(long bidId)
    {
        var bid = await _unitOfWork.Bids.GetByIdAsync(bidId);
        return bid == null ? null : _mapper.Map<BidDto>(bid);
    }

    public async Task<IEnumerable<BidDto>> GetByAuctionIdAsync(long auctionId)
    {
        var bids = await _unitOfWork.Bids.GetByAuctionIdAsync(auctionId);
        return _mapper.Map<IEnumerable<BidDto>>(bids);
    }

    public async Task<IEnumerable<BidDto>> GetByUserIdAsync(long userId)
    {
        var bids = await _unitOfWork.Bids.GetByUserIdAsync(userId);
        return _mapper.Map<IEnumerable<BidDto>>(bids);
    }

    public async Task<bool> PlaceBidAsync(PlaceBidDto dto)
    {
        // Валідація
        if (dto.BidAmount <= 0)
            throw new ArgumentException("Bid amount must be greater than 0");

        // Перевіряємо чи існує аукціон
        var auction = await _unitOfWork.Auctions.GetByIdAsync(dto.AuctionId);
        if (auction == null)
            throw new InvalidOperationException("Auction not found");

        // Перевіряємо чи ставка більша за поточну ціну
        if (dto.BidAmount <= auction.CurrentPrice)
            throw new ArgumentException($"Bid must be higher than current price ({auction.CurrentPrice})");

        // Використовуємо stored procedure для атомарної операції
        return await _unitOfWork.Bids.PlaceBidAsync(dto.AuctionId, dto.UserId, dto.BidAmount);
    }
}
