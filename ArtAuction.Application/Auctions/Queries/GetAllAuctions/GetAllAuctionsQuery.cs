using ArtAuction.Domain.Entities;
using MediatR;

namespace ArtAuction.Application.Auctions.Queries.GetAllAuctions;

public record GetAllAuctionsQuery : IRequest<IEnumerable<Auction>>;
