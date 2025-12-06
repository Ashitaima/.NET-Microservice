using FluentValidation;

namespace ArtAuction.Application.Auctions.Commands.DeleteAuction;

public class DeleteAuctionCommandValidator : AbstractValidator<DeleteAuctionCommand>
{
    public DeleteAuctionCommandValidator()
    {
        RuleFor(x => x.AuctionId)
            .NotEmpty().WithMessage("Auction ID is required");
    }
}
