using ArtAuction.Application.Common.Interfaces;
using ArtAuction.Domain.Entities;
using MediatR;

namespace ArtAuction.Application.Auctions.Queries.GetAuction;

public class GetAuctionQueryHandler : IRequestHandler<GetAuctionQuery, Auction?>
{
    private readonly IAuctionRepository _repository;

    public GetAuctionQueryHandler(IAuctionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Auction?> Handle(GetAuctionQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetByIdAsync(request.AuctionId, cancellationToken);
    }
}
