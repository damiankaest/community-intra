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
        RuleFor(request => request.ThemePackKey)
            .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$")
            .MaximumLength(64)
            .When(request => !string.IsNullOrWhiteSpace(request.ThemePackKey));
        RuleFor(request => request.EnabledModules)
            .Must(modules => modules is null || modules.Count is > 0 and <= 10)
            .WithMessage("EnabledModules must contain between 1 and 10 modules.");
        RuleForEach(request => request.EnabledModules!)
            .Must(module =>
                !string.IsNullOrWhiteSpace(module)
                && OrganizationModuleKeys.All.Contains(
                    module.Trim().ToLowerInvariant()))
            .WithMessage("EnabledModules contains an unknown module.");
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
        RuleFor(request => request.ThemePackKey)
            .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$")
            .MaximumLength(64)
            .When(request => !string.IsNullOrWhiteSpace(request.ThemePackKey));
        RuleFor(request => request.EnabledModules)
            .Must(modules => modules is null || modules.Count is > 0 and <= 10)
            .WithMessage("EnabledModules must contain between 1 and 10 modules.");
        RuleForEach(request => request.EnabledModules!)
            .Must(module =>
                !string.IsNullOrWhiteSpace(module)
                && OrganizationModuleKeys.All.Contains(
                    module.Trim().ToLowerInvariant()))
            .WithMessage("EnabledModules contains an unknown module.");
    }
}
