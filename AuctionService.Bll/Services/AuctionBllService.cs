using AutoMapper;
using AuctionService.Bll.DTOs;
using AuctionService.Bll.Interfaces;
using AuctionService.Dal.Interfaces;
using AuctionService.Domain.Entities;

namespace AuctionService.Bll.Services;

public class AuctionBllService : IAuctionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public AuctionBllService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<AuctionDto?> GetByIdAsync(long auctionId)
    {
        var auction = await _unitOfWork.Auctions.GetByIdAsync(auctionId);
        return auction == null ? null : _mapper.Map<AuctionDto>(auction);
    }

    public async Task<IEnumerable<AuctionDto>> GetAllAsync()
    {
        var auctions = await _unitOfWork.Auctions.GetAllAsync();
        return _mapper.Map<IEnumerable<AuctionDto>>(auctions);
    }

    public async Task<IEnumerable<AuctionDto>> GetActiveAuctionsAsync()
    {
        var auctions = await _unitOfWork.Auctions.GetActiveAuctionsAsync();
        return _mapper.Map<IEnumerable<AuctionDto>>(auctions);
    }

    public async Task<long> CreateAsync(CreateAuctionDto dto)
    {
        // Валідація
        if (dto.StartPrice <= 0)
            throw new ArgumentException("Start price must be greater than 0");

        if (dto.EndTime <= dto.StartTime)
            throw new ArgumentException("End time must be after start time");

        if (string.IsNullOrWhiteSpace(dto.ArtworkName))
            throw new ArgumentException("Artwork name is required");

        var auction = _mapper.Map<Auction>(dto);
        await _unitOfWork.Auctions.AddAsync(auction);
        await _unitOfWork.SaveChangesAsync();
        
        return auction.AuctionId;
    }

    public async Task<bool> UpdateAsync(long auctionId, AuctionDto dto)
    {
        // Перевіряємо чи існує
        var existing = await _unitOfWork.Auctions.GetByIdAsync(auctionId);
        if (existing == null)
            throw new KeyNotFoundException($"Auction with ID {auctionId} not found");

        // Валідація
        if (dto.StartPrice <= 0)
            throw new ArgumentException("Start price must be greater than 0");

        if (dto.CurrentPrice < dto.StartPrice)
            throw new ArgumentException("Current price cannot be less than start price");

        // Оновлюємо властивості
        existing.ArtworkName = dto.ArtworkName;
        existing.StartPrice = dto.StartPrice;
        existing.CurrentPrice = dto.CurrentPrice;
        existing.StartTime = dto.StartTime;
        existing.EndTime = dto.EndTime;
        existing.Status = (AuctionStatus)dto.Status;
        existing.WinnerUserId = dto.WinnerUserId;

        _unitOfWork.Auctions.Update(existing);
        await _unitOfWork.SaveChangesAsync();
        
        return true;
    }

    public async Task<bool> DeleteAsync(long auctionId)
    {
        var auction = await _unitOfWork.Auctions.GetByIdAsync(auctionId);
        if (auction == null)
            return false;
            
        _unitOfWork.Auctions.Delete(auction);
        await _unitOfWork.SaveChangesAsync();
        
        return true;
    }
}
