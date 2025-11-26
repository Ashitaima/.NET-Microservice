using ArtAuction.Application.Common.Interfaces;
using ArtAuction.Domain.Entities;
using ArtAuction.Domain.ValueObjects;
using ArtAuction.Domain.Exceptions;
using MediatR;

namespace ArtAuction.Application.Auctions.Commands.PlaceBid;

public class PlaceBidCommandHandler : IRequestHandler<PlaceBidCommand, Auction>
{
    private readonly IAuctionRepository _repository;

    public PlaceBidCommandHandler(IAuctionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Auction> Handle(PlaceBidCommand request, CancellationToken cancellationToken)
    {
        var auction = await _repository.GetByIdAsync(request.AuctionId, cancellationToken);
        
        if (auction == null)
        {
            throw new DomainException($"Auction {request.AuctionId} not found");
        }

        var bidAmount = Money.Create(request.BidAmount);
        auction.PlaceBid(request.BidderId, bidAmount);

        await _repository.UpdateAsync(auction, cancellationToken);

        return auction;
    }
}
