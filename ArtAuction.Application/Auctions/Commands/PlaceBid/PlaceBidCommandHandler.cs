using ArtAuction.Application.Common.Interfaces;
using ArtAuction.Domain.Entities;
using ArtAuction.Domain.Events;
using ArtAuction.Domain.ValueObjects;
using ArtAuction.Domain.Exceptions;
using MediatR;
using System.Diagnostics;

namespace ArtAuction.Application.Auctions.Commands.PlaceBid;

public class PlaceBidCommandHandler : IRequestHandler<PlaceBidCommand, Auction>
{
    private readonly IAuctionRepository _repository;
    private readonly IEventPublisher _eventPublisher;

    public PlaceBidCommandHandler(IAuctionRepository repository, IEventPublisher eventPublisher)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
    }

    public async Task<Auction> Handle(PlaceBidCommand request, CancellationToken cancellationToken)
    {
        var auction = await _repository.GetByIdAsync(request.AuctionId, cancellationToken);
        
        if (auction == null)
        {
            throw new DomainException($"Auction {request.AuctionId} not found");
        }

        var previousPrice = auction.CurrentPrice.Amount;
        var bidAmount = Money.Create(request.BidAmount);
        auction.PlaceBid(request.BidderId, bidAmount);

        await _repository.UpdateAsync(auction, cancellationToken);

        // Publish event after successful update
        try
        {
            var @event = new BidPlacedEvent
            {
                CorrelationId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString(),
                AuctionId = auction.Id,
                BidderId = request.BidderId,
                BidAmount = request.BidAmount,
                PreviousPrice = previousPrice,
                TotalBids = auction.Bids.Count
            };

            await _eventPublisher.PublishAsync(@event, "auction.bid.placed", cancellationToken);
        }
        catch (Exception)
        {
            // Log but don't fail - best effort event publishing
        }

        return auction;
    }
}
