using ArtAuction.Domain.Entities;
using MediatR;

namespace ArtAuction.Application.Auctions.Queries.GetAuction;

public record GetAuctionQuery(string AuctionId) : IRequest<Auction?>;
