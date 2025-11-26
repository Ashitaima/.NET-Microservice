using ArtAuction.Application.Common.Interfaces;
using ArtAuction.Domain.Entities;
using MediatR;

namespace ArtAuction.Application.Auctions.Queries.GetAllAuctions;

public class GetAllAuctionsQueryHandler : IRequestHandler<GetAllAuctionsQuery, IEnumerable<Auction>>
{
    private readonly IAuctionRepository _repository;

    public GetAllAuctionsQueryHandler(IAuctionRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<Auction>> Handle(GetAllAuctionsQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetAllAsync(cancellationToken);
    }
}
