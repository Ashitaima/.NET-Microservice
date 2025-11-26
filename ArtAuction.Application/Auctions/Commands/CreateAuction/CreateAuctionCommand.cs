using ArtAuction.Domain.Entities;
using MediatR;

namespace ArtAuction.Application.Auctions.Commands.CreateAuction;

public record CreateAuctionCommand : IRequest<Auction>
{
    public string ArtworkName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string SellerId { get; init; } = string.Empty;
    public decimal StartPriceAmount { get; init; }
    public string StartPriceCurrency { get; init; } = "USD";
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
}
