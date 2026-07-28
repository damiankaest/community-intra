using CommunityIntranet.Modules.Organizations.Contracts;
using FluentValidation;

namespace CommunityIntranet.Modules.Organizations.Validation;

public sealed class CreateOrganizationRequestValidator
    : AbstractValidator<CreateOrganizationRequest>
{
    public CreateOrganizationRequestValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(120);
        RuleFor(request => request.Description).MaximumLength(1000);
        RuleFor(request => request.Language)
            .NotEmpty()
            .Matches("^[a-z]{2}(-[A-Z]{2})?$")
            .MaximumLength(10);
        RuleFor(request => request.TimeZone)
            .NotEmpty()
            .MaximumLength(100);
        RuleFor(request => request.VisibleTitle).MaximumLength(100);
    }
}

public sealed class UpdateOrganizationRequestValidator
    : AbstractValidator<UpdateOrganizationRequest>
{
    public UpdateOrganizationRequestValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(120);
        RuleFor(request => request.Description).MaximumLength(1000);
        RuleFor(request => request.Language)
            .NotEmpty()
            .Matches("^[a-z]{2}(-[A-Z]{2})?$")
            .MaximumLength(10);
        RuleFor(request => request.TimeZone)
            .NotEmpty()
            .MaximumLength(100);
    }
}
