using ArtAuction.Application.Common.Interfaces;
using MediatR;

namespace ArtAuction.Application.Auctions.Commands.DeleteAuction;

public class DeleteAuctionCommandHandler : IRequestHandler<DeleteAuctionCommand, bool>
{
    private readonly IAuctionRepository _repository;

    public DeleteAuctionCommandHandler(IAuctionRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(DeleteAuctionCommand request, CancellationToken cancellationToken)
    {
        var auction = await _repository.GetByIdAsync(request.AuctionId, cancellationToken);
        
        if (auction == null)
        {
            return false;
        }

        await _repository.DeleteAsync(request.AuctionId, cancellationToken);

        return true;
    }
}
