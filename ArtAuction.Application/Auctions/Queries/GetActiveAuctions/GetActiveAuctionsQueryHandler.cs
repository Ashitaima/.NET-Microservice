using ArtAuction.Application.Common.Interfaces;
using ArtAuction.Domain.Entities;
using MediatR;

namespace ArtAuction.Application.Auctions.Queries.GetActiveAuctions;

public class GetActiveAuctionsQueryHandler : IRequestHandler<GetActiveAuctionsQuery, IEnumerable<Auction>>
{
    private readonly IAuctionRepository _repository;

    public GetActiveAuctionsQueryHandler(IAuctionRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<Auction>> Handle(GetActiveAuctionsQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetActiveAuctionsAsync(cancellationToken);
    }
}
