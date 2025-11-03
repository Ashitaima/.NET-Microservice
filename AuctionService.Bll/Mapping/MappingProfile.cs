using AutoMapper;
using AuctionService.Bll.DTOs;
using AuctionService.Domain.Entities;

namespace AuctionService.Bll.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // User mappings
        CreateMap<User, UserDto>().ReverseMap();

        // Auction mappings
        CreateMap<Auction, AuctionDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => (int)src.Status));
        
        CreateMap<CreateAuctionDto, Auction>()
            .ForMember(dest => dest.AuctionId, opt => opt.Ignore())
            .ForMember(dest => dest.CurrentPrice, opt => opt.MapFrom(src => src.StartPrice))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => AuctionStatus.Pending))
            .ForMember(dest => dest.WinnerUserId, opt => opt.Ignore());

        // Bid mappings
        CreateMap<Bid, BidDto>().ReverseMap();
        CreateMap<PlaceBidDto, Bid>()
            .ForMember(dest => dest.BidId, opt => opt.Ignore())
            .ForMember(dest => dest.Timestamp, opt => opt.MapFrom(src => DateTime.UtcNow));

        // Payment mappings
        CreateMap<Payment, PaymentDto>()
            .ForMember(dest => dest.TransactionStatus, opt => opt.MapFrom(src => (int)src.TransactionStatus));
        
        CreateMap<PaymentDto, Payment>()
            .ForMember(dest => dest.TransactionStatus, opt => opt.MapFrom(src => (TransactionStatus)src.TransactionStatus));
    }
}
