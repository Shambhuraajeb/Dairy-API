using Dairy.Application.Requests.RateCharts;
using FluentValidation;

namespace Dairy.Application.Validators.RateCharts;

public class RateChartQueryValidator : AbstractValidator<RateChartQuery>
{
    public RateChartQueryValidator()
    {
        RuleFor(x => x.MilkType)
            .Must(v => v is not null && new[] { "Cow", "Buffalo", "Mixed" }.Contains(v.Trim(), StringComparer.OrdinalIgnoreCase))
            .When(x => !string.IsNullOrWhiteSpace(x.MilkType))
            .WithMessage("Milk type must be Cow, Buffalo or Mixed.");
        RuleFor(x => x.Status)
            .Must(v => v is not null && new[] { "Draft", "Active", "Expired" }.Contains(v.Trim(), StringComparer.OrdinalIgnoreCase))
            .When(x => !string.IsNullOrWhiteSpace(x.Status))
            .WithMessage("Status must be Draft, Active or Expired.");
    }
}

public class SaveRateChartRequestValidator : AbstractValidator<SaveRateChartRequest>
{
    public SaveRateChartRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.").MaximumLength(150);
        RuleFor(x => x.MilkType)
            .Must(v => v is not null && new[] { "Cow", "Buffalo", "Mixed" }.Contains(v.Trim(), StringComparer.OrdinalIgnoreCase))
            .WithMessage("Milk type must be Cow, Buffalo or Mixed.");
        RuleFor(x => x.CollectionCenterId).GreaterThan(0L).WithMessage("Collection center is invalid.");
        RuleFor(x => x.EffectiveFrom)
            .Must(d => d.Year >= 2000).WithMessage("Effective-from date is required.");
        RuleFor(x => x.EffectiveTo)
            .Must((r, to) => !to.HasValue || to.Value.Date >= r.EffectiveFrom.Date)
            .WithMessage("Effective-to date must be on or after the effective-from date.");
        RuleFor(x => x.CalculationMethod)
            .Must(v => v is not null && new[] { "Slab", "Formula" }.Contains(v.Trim(), StringComparer.OrdinalIgnoreCase))
            .WithMessage("Calculation method must be Slab or Formula.");

        RuleFor(x => x.BaseRatePerLiter).InclusiveBetween(0m, 10000m).WithMessage("Base rate must be between 0 and 10000.");
        RuleFor(x => x.BaseFat).InclusiveBetween(0m, 25m).WithMessage("Base FAT must be between 0 and 25.");
        RuleFor(x => x.BaseSnf).InclusiveBetween(0m, 25m).WithMessage("Base SNF must be between 0 and 25.");
        RuleFor(x => x.FatRatePerPoint).InclusiveBetween(0m, 10000m).WithMessage("FAT rate per point must be between 0 and 10000.");
        RuleFor(x => x.SnfRatePerPoint).InclusiveBetween(0m, 10000m).WithMessage("SNF rate per point must be between 0 and 10000.");
        RuleFor(x => x.MinRatePerLiter).InclusiveBetween(0m, 10000m).WithMessage("Minimum rate must be between 0 and 10000.");
        RuleFor(x => x.MaxRatePerLiter).InclusiveBetween(0m, 10000m).WithMessage("Maximum rate must be between 0 and 10000.");
        RuleFor(x => x.MaxRatePerLiter)
            .Must((r, max) => !max.HasValue || max.Value >= r.MinRatePerLiter)
            .WithMessage("Maximum rate must be greater than or equal to the minimum rate.");
        RuleFor(x => x.BonusMinFat).InclusiveBetween(0m, 25m);
        RuleFor(x => x.BonusMinSnf).InclusiveBetween(0m, 25m);
        RuleFor(x => x.PenaltyBelowFat).InclusiveBetween(0m, 25m);
        RuleFor(x => x.PenaltyBelowSnf).InclusiveBetween(0m, 25m);
        RuleFor(x => x.QualityBonusPerLiter).InclusiveBetween(0m, 10000m);
        RuleFor(x => x.QualityPenaltyPerLiter).InclusiveBetween(0m, 10000m);
        RuleFor(x => x.Description).MaximumLength(255);

        RuleFor(x => x.Details)
            .Must(d => d.Count <= 500).WithMessage("A rate chart can have at most 500 slabs.");
        RuleForEach(x => x.Details)
            .SetValidator(new RateChartDetailRequestValidator())
            .When(x => string.Equals(x.CalculationMethod, "Slab", StringComparison.OrdinalIgnoreCase));
        RuleFor(x => x.Details)
            .Must(NoOverlap).WithMessage("Rate slabs must not overlap (FAT range and SNF range both intersect).")
            .When(x => string.Equals(x.CalculationMethod, "Slab", StringComparison.OrdinalIgnoreCase) && x.Details.Count <= 500);
    }

    private static bool NoOverlap(List<RateChartDetailRequest> details)
    {
        for (var i = 0; i < details.Count; i++)
        {
            for (var j = i + 1; j < details.Count; j++)
            {
                var a = details[i];
                var b = details[j];
                if (a.FatFrom < b.FatTo && b.FatFrom < a.FatTo && a.SnfFrom < b.SnfTo && b.SnfFrom < a.SnfTo)
                    return false;
            }
        }
        return true;
    }
}

public class RateChartDetailRequestValidator : AbstractValidator<RateChartDetailRequest>
{
    public RateChartDetailRequestValidator()
    {
        RuleFor(x => x.FatFrom).InclusiveBetween(0m, 25m).WithMessage("FAT from must be between 0 and 25.");
        RuleFor(x => x.FatTo).InclusiveBetween(0m, 25m).WithMessage("FAT to must be between 0 and 25.");
        RuleFor(x => x.SnfFrom).InclusiveBetween(0m, 25m).WithMessage("SNF from must be between 0 and 25.");
        RuleFor(x => x.SnfTo).InclusiveBetween(0m, 25m).WithMessage("SNF to must be between 0 and 25.");
        RuleFor(x => x.FatTo).GreaterThan(x => x.FatFrom).WithMessage("FAT to must be greater than FAT from.");
        RuleFor(x => x.SnfTo).GreaterThan(x => x.SnfFrom).WithMessage("SNF to must be greater than SNF from.");
        RuleFor(x => x.Rate).InclusiveBetween(0m, 10000m).WithMessage("Rate must be between 0 and 10000.");
        RuleFor(x => x.BonusPerLiter).InclusiveBetween(0m, 10000m);
        RuleFor(x => x.PenaltyPerLiter).InclusiveBetween(0m, 10000m);
    }
}
