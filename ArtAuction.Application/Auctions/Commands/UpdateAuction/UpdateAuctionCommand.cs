using ArtAuction.Domain.Entities;
using MediatR;

namespace ArtAuction.Application.Auctions.Commands.UpdateAuction;

public record UpdateAuctionCommand : IRequest<Auction>
{
    public string AuctionId { get; init; } = string.Empty;
    public string ArtworkName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
}
