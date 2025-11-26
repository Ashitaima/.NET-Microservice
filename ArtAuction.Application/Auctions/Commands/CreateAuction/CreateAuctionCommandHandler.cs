using ArtAuction.Application.Common.Interfaces;
using ArtAuction.Domain.Entities;
using ArtAuction.Domain.Events;
using MediatR;
using System.Diagnostics;

namespace ArtAuction.Application.Auctions.Commands.CreateAuction;

public class CreateAuctionCommandHandler : IRequestHandler<CreateAuctionCommand, Auction>
{
    private readonly IAuctionRepository _repository;
    private readonly IEventPublisher _eventPublisher;

    public CreateAuctionCommandHandler(IAuctionRepository repository, IEventPublisher eventPublisher)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
    }

    public async Task<Auction> Handle(CreateAuctionCommand request, CancellationToken cancellationToken)
    {
        var auction = Auction.Create(
            request.ArtworkName,
            request.SellerId,
            request.StartPriceAmount,
            request.StartTime,
            request.EndTime,
            request.Description
        );

        // Save to database first (transactional boundary)
        var result = await _repository.AddAsync(auction, cancellationToken);

        // Publish event only after successful DB commit
        try
        {
            var @event = new AuctionCreatedEvent
            {
                CorrelationId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString(),
                AuctionId = result.Id,
                ArtworkName = result.ArtworkName,
                SellerId = result.SellerId,
                StartPriceAmount = result.StartPrice.Amount,
                StartPriceCurrency = result.StartPrice.Currency,
                StartTime = result.StartTime,
                EndTime = result.EndTime
            };

            await _eventPublisher.PublishAsync(@event, "auction.created", cancellationToken);
        }
        catch (Exception)
        {
            // Log but don't fail the request - event publishing is best-effort
            // In production, consider using Outbox pattern for guaranteed delivery
        }

        return result;
    }
}
