using ArtAuction.Application.Common.Interfaces;
using ArtAuction.Domain.Entities;
using ArtAuction.Domain.Exceptions;
using MediatR;

namespace ArtAuction.Application.Auctions.Commands.UpdateAuction;

public class UpdateAuctionCommandHandler : IRequestHandler<UpdateAuctionCommand, Auction>
{
    private readonly IAuctionRepository _repository;

    public UpdateAuctionCommandHandler(IAuctionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Auction> Handle(UpdateAuctionCommand request, CancellationToken cancellationToken)
    {
        var auction = await _repository.GetByIdAsync(request.AuctionId, cancellationToken);
        
        if (auction == null)
        {
            throw new DomainException($"Auction {request.AuctionId} not found");
        }

        // Use domain method to update
        auction.Update(request.ArtworkName, request.Description, request.StartTime, request.EndTime);

        await _repository.UpdateAsync(auction, cancellationToken);

        return auction;
    }
}
