using ArtAuction.Domain.Entities;
using MediatR;

namespace ArtAuction.Application.Auctions.Commands.PlaceBid;

public record PlaceBidCommand : IRequest<Auction>
{
    public string AuctionId { get; init; } = string.Empty;
    public string BidderId { get; init; } = string.Empty;
    public decimal BidAmount { get; init; }
}
