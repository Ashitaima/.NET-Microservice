using FluentValidation;

namespace ArtAuction.Application.Auctions.Commands.CreateAuction;

public class CreateAuctionCommandValidator : AbstractValidator<CreateAuctionCommand>
{
    public CreateAuctionCommandValidator()
    {
        RuleFor(x => x.ArtworkName)
            .NotEmpty().WithMessage("Artwork name is required")
            .MaximumLength(200).WithMessage("Artwork name cannot exceed 200 characters");

        RuleFor(x => x.SellerId)
            .NotEmpty().WithMessage("Seller ID is required");

        RuleFor(x => x.StartPriceAmount)
            .GreaterThan(0).WithMessage("Start price must be greater than 0");

        RuleFor(x => x.StartPriceCurrency)
            .NotEmpty().WithMessage("Currency is required")
            .Length(3).WithMessage("Currency must be 3 characters (ISO code)");

        RuleFor(x => x.StartTime)
            .NotEmpty().WithMessage("Start time is required");

        RuleFor(x => x.EndTime)
            .NotEmpty().WithMessage("End time is required")
            .GreaterThan(x => x.StartTime).WithMessage("End time must be after start time");

        RuleFor(x => x)
            .Must(x => x.EndTime > DateTime.UtcNow)
            .WithMessage("End time must be in the future");
    }
}
