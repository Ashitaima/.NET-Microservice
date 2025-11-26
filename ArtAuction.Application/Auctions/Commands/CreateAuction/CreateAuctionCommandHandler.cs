using ArtAuction.Application.Common.Interfaces;
using ArtAuction.Domain.Entities;
using ArtAuction.Domain.ValueObjects;
using MediatR;

namespace ArtAuction.Application.Auctions.Commands.CreateAuction;

public class CreateAuctionCommandHandler : IRequestHandler<CreateAuctionCommand, Auction>
{
    private readonly IAuctionRepository _repository;

    public CreateAuctionCommandHandler(IAuctionRepository repository)
    {
        _repository = repository;
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

        var result = await _repository.AddAsync(auction, cancellationToken);

        return result;
    }
}
