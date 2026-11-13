using FluentValidation;

namespace LabTrack.Application.Common.Pagination;

public sealed class PaginationValidator<T> : AbstractValidator<T> where T : PagedRequest
{
    public PaginationValidator()
    {
        RuleFor(x => x.PageIndex)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page index must be greater than or equal to 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Page size must be between 1 and 100.");

        RuleFor(x => x.SearchTerm)
            .MaximumLength(100)
            .WithMessage("Search term must not exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.SearchTerm));

        RuleFor(x => x.SortBy)
            .MaximumLength(50)
            .WithMessage("Sort column name must not exceed 50 characters.")
            .Matches(@"^[a-zA-Z0-9_]+$")
            .WithMessage("Sort column name can only contain alphanumeric characters and underscores.")
            .When(x => !string.IsNullOrEmpty(x.SortBy));
    }
}