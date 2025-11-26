using ArtAuction.Domain.Entities;
using MediatR;

namespace ArtAuction.Application.Auctions.Queries.GetActiveAuctions;

public record GetActiveAuctionsQuery : IRequest<IEnumerable<Auction>>;
