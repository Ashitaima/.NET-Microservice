using MediatR;

namespace ArtAuction.Application.Auctions.Commands.DeleteAuction;

public record DeleteAuctionCommand : IRequest<bool>
{
    public string AuctionId { get; init; } = string.Empty;
}
