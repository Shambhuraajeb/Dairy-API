using Dairy.Application.Requests.Animals;
using FluentValidation;

namespace Dairy.Application.Validators.Animals;

public class AnimalRequestValidator : AbstractValidator<AnimalRequest>
{
    public AnimalRequestValidator()
    {
        RuleFor(x => x.TagNumber).NotEmpty().WithMessage("Tag number is required.").MaximumLength(30);
        RuleFor(x => x.FarmerId).GreaterThan(0).WithMessage("Farmer is required.");
        RuleFor(x => x.Name).MaximumLength(100);
        RuleFor(x => x.AnimalType).NotEmpty().MustBeOneOf("Cow", "Buffalo", "Goat");
        RuleFor(x => x.Gender).NotEmpty().MustBeOneOf("Female", "Male");
        RuleFor(x => x.DateOfBirth).LessThan(DateTime.Today.AddDays(1)).When(x => x.DateOfBirth.HasValue)
            .WithMessage("Date of birth cannot be in the future.");
        RuleFor(x => x.Color).MaximumLength(50);
        RuleFor(x => x.WeightKg).GreaterThan(0m).When(x => x.WeightKg.HasValue);
        RuleFor(x => x.Source).NotEmpty().MustBeOneOf("Born", "Purchased");
        RuleFor(x => x.RegistrationDate).LessThan(DateTime.Today.AddDays(1)).When(x => x.RegistrationDate.HasValue)
            .WithMessage("Registration date cannot be in the future.");
        RuleFor(x => x.LactationNo).InclusiveBetween(0, 30);
        RuleFor(x => x.MilkingStatus).NotEmpty().MustBeOneOf("Milking", "Dry", "Heifer", "Calf", "NotApplicable");
        RuleFor(x => x.PregnancyStatus).NotEmpty().MustBeOneOf("NotPregnant", "Pregnant", "Unknown");
        RuleFor(x => x.ExpectedDeliveryDate).NotNull().When(x => x.PregnancyStatus == "Pregnant")
            .WithMessage("Expected delivery date is required for a pregnant animal.");
        RuleFor(x => x.Remarks).MaximumLength(500);
    }
}

public class AnimalSaleRequestValidator : AbstractValidator<AnimalSaleRequest>
{
    public AnimalSaleRequestValidator()
    {
        RuleFor(x => x.SaleDate).LessThan(DateTime.Today.AddDays(1)).When(x => x.SaleDate.HasValue)
            .WithMessage("Sale date cannot be in the future.");
        RuleFor(x => x.BuyerName).NotEmpty().WithMessage("Buyer name is required.").MaximumLength(150);
        RuleFor(x => x.BuyerMobile).MustBeValidMobile();
        RuleFor(x => x.BuyerAddress).MaximumLength(255);
        RuleFor(x => x.SalePrice).GreaterThanOrEqualTo(0m).WithMessage("Sale price cannot be negative.");
        RuleFor(x => x.ReferenceNo).MaximumLength(50);
        RuleFor(x => x.Reason).MaximumLength(255);
        RuleFor(x => x.Remarks).MaximumLength(500);
    }
}

public class AnimalDeathRequestValidator : AbstractValidator<AnimalDeathRequest>
{
    public AnimalDeathRequestValidator()
    {
        RuleFor(x => x.DeathDate).LessThan(DateTime.Today.AddDays(1)).When(x => x.DeathDate.HasValue)
            .WithMessage("Death date cannot be in the future.");
        RuleFor(x => x.Cause).MaximumLength(200);
        RuleFor(x => x.CauseCategory).NotEmpty().MustBeOneOf("Disease", "Accident", "Age", "Calving", "Unknown");
        RuleFor(x => x.VetName).MaximumLength(100);
        RuleFor(x => x.LossAmount).GreaterThanOrEqualTo(0m).WithMessage("Loss amount cannot be negative.");
        RuleFor(x => x.Remarks).MaximumLength(500);
    }
}

public class AnimalArchiveRequestValidator : AbstractValidator<AnimalArchiveRequest>
{
    public AnimalArchiveRequestValidator()
    {
        RuleFor(x => x.Reason).MaximumLength(255);
    }
}

public class CreateBreedRequestValidator : AbstractValidator<CreateBreedRequest>
{
    public CreateBreedRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithMessage("Code is required.").MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.").MaximumLength(100);
        RuleFor(x => x.AnimalType).NotEmpty().MustBeOneOf("Cow", "Buffalo", "Goat");
        RuleFor(x => x.AverageDailyYield).GreaterThanOrEqualTo(0m).When(x => x.AverageDailyYield.HasValue);
        RuleFor(x => x.Description).MaximumLength(255);
    }
}

public class UpdateBreedRequestValidator : AbstractValidator<UpdateBreedRequest>
{
    public UpdateBreedRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithMessage("Code is required.").MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.").MaximumLength(100);
        RuleFor(x => x.AnimalType).NotEmpty().MustBeOneOf("Cow", "Buffalo", "Goat");
        RuleFor(x => x.AverageDailyYield).GreaterThanOrEqualTo(0m).When(x => x.AverageDailyYield.HasValue);
        RuleFor(x => x.Description).MaximumLength(255);
    }
}

public class AnimalHealthRecordRequestValidator : AbstractValidator<AnimalHealthRecordRequest>
{
    public AnimalHealthRecordRequestValidator()
    {
        RuleFor(x => x.RecordDate).NotNull().WithMessage("Record date is required.")
            .LessThan(DateTime.Today.AddDays(1)).WithMessage("Record date cannot be in the future.");
        RuleFor(x => x.HealthCondition).NotEmpty().WithMessage("Health condition is required.").MaximumLength(150);
        RuleFor(x => x.Symptoms).MaximumLength(500);
        RuleFor(x => x.Diagnosis).MaximumLength(500);
        RuleFor(x => x.Treatment).MaximumLength(500);
        RuleFor(x => x.Medicines).MaximumLength(500);
        RuleFor(x => x.Severity).NotEmpty().MustBeOneOf("Mild", "Moderate", "Severe");
        RuleFor(x => x.VetName).MaximumLength(100);
        RuleFor(x => x.Cost).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.Status).NotEmpty().MustBeOneOf("Open", "Ongoing", "Recovered");
        RuleFor(x => x.RecoveredDate).NotNull().When(x => x.Status == "Recovered")
            .WithMessage("Recovered date is required when the status is Recovered.");
        RuleFor(x => x.Remarks).MaximumLength(500);
    }
}

public class VaccinationRequestValidator : AbstractValidator<VaccinationRequest>
{
    public VaccinationRequestValidator()
    {
        RuleFor(x => x.VaccineName).NotEmpty().WithMessage("Vaccine name is required.").MaximumLength(150);
        RuleFor(x => x.VaccinationDate).NotNull().WithMessage("Vaccination date is required.");
        RuleFor(x => x.VaccinationDate).LessThan(DateTime.Today.AddDays(1))
            .When(x => x.VaccinationDate.HasValue && x.Status == "Completed")
            .WithMessage("A completed vaccination cannot be dated in the future.");
        RuleFor(x => x.NextDueDate).GreaterThanOrEqualTo(x => x.VaccinationDate)
            .When(x => x.NextDueDate.HasValue && x.VaccinationDate.HasValue)
            .WithMessage("Next due date cannot be before the vaccination date.");
        RuleFor(x => x.DoseNumber).InclusiveBetween(0, 20);
        RuleFor(x => x.BatchNumber).MaximumLength(50);
        RuleFor(x => x.AdministeredBy).MaximumLength(100);
        RuleFor(x => x.Cost).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.Status).NotEmpty().MustBeOneOf("Completed", "Scheduled", "Missed");
        RuleFor(x => x.Remarks).MaximumLength(500);
    }
}

public class DewormingRequestValidator : AbstractValidator<DewormingRequest>
{
    public DewormingRequestValidator()
    {
        RuleFor(x => x.MedicineName).NotEmpty().WithMessage("Medicine name is required.").MaximumLength(150);
        RuleFor(x => x.DewormingDate).NotNull().WithMessage("Deworming date is required.");
        RuleFor(x => x.DewormingDate).LessThan(DateTime.Today.AddDays(1))
            .When(x => x.DewormingDate.HasValue && x.Status == "Completed")
            .WithMessage("A completed deworming cannot be dated in the future.");
        RuleFor(x => x.NextDueDate).GreaterThanOrEqualTo(x => x.DewormingDate)
            .When(x => x.NextDueDate.HasValue && x.DewormingDate.HasValue)
            .WithMessage("Next due date cannot be before the deworming date.");
        RuleFor(x => x.Dosage).MaximumLength(100);
        RuleFor(x => x.AdministeredBy).MaximumLength(100);
        RuleFor(x => x.Cost).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.Status).NotEmpty().MustBeOneOf("Completed", "Scheduled", "Missed");
        RuleFor(x => x.Remarks).MaximumLength(500);
    }
}

public class BreedingRecordRequestValidator : AbstractValidator<BreedingRecordRequest>
{
    public BreedingRecordRequestValidator()
    {
        RuleFor(x => x.BreedingDate).NotNull().WithMessage("Breeding date is required.")
            .LessThan(DateTime.Today.AddDays(1)).WithMessage("Breeding date cannot be in the future.");
        RuleFor(x => x.BreedingMethod).NotEmpty().MustBeOneOf("NaturalService", "ArtificialInsemination");
        RuleFor(x => x.BullName).MaximumLength(100);
        RuleFor(x => x.SemenCode).MaximumLength(50);
        RuleFor(x => x.TechnicianName).MaximumLength(100);
        RuleFor(x => x.ExpectedDeliveryDate).GreaterThan(x => x.BreedingDate)
            .When(x => x.ExpectedDeliveryDate.HasValue && x.BreedingDate.HasValue)
            .WithMessage("Expected delivery date must be after the breeding date.");
        RuleFor(x => x.Result).NotEmpty().MustBeOneOf("Pending", "Successful", "Failed");
        RuleFor(x => x.Cost).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.Remarks).MaximumLength(500);
    }
}

public class PregnancyRecordRequestValidator : AbstractValidator<PregnancyRecordRequest>
{
    public PregnancyRecordRequestValidator()
    {
        RuleFor(x => x.ConfirmedDate).NotNull().WithMessage("Confirmed date is required.");
        RuleFor(x => x.ExpectedDeliveryDate).NotNull().WithMessage("Expected delivery date is required.");
        RuleFor(x => x.ExpectedDeliveryDate).GreaterThanOrEqualTo(x => x.ConfirmedDate)
            .When(x => x.ExpectedDeliveryDate.HasValue && x.ConfirmedDate.HasValue)
            .WithMessage("Expected delivery date cannot be before the confirmed date.");
        RuleFor(x => x.Status).NotEmpty().MustBeOneOf("Pregnant", "Delivered", "Aborted");
        RuleFor(x => x.ActualDeliveryDate).NotNull().When(x => x.Status == "Delivered")
            .WithMessage("Actual delivery date is required when the status is Delivered.");
        RuleFor(x => x.DeliveryOutcome).MustBeOneOf("LiveBirth", "Stillbirth", "Abortion");
        RuleFor(x => x.CalfCount).InclusiveBetween(0, 5);
        RuleFor(x => x.CalfGender).MaximumLength(10);
        RuleFor(x => x.Remarks).MaximumLength(500);
    }
}

public class AnimalMilkProductionRequestValidator : AbstractValidator<AnimalMilkProductionRequest>
{
    public AnimalMilkProductionRequestValidator()
    {
        RuleFor(x => x.ProductionDate).NotNull().WithMessage("Production date is required.")
            .LessThan(DateTime.Today.AddDays(1)).WithMessage("Production date cannot be in the future.");
        RuleFor(x => x.ShiftName).NotEmpty().MustBeOneOf("Morning", "Evening");
        RuleFor(x => x.QuantityLiters).GreaterThan(0m).WithMessage("Quantity must be greater than zero.")
            .LessThanOrEqualTo(100m);
        RuleFor(x => x.Fat).InclusiveBetween(0m, 20m).When(x => x.Fat.HasValue);
        RuleFor(x => x.Snf).InclusiveBetween(0m, 20m).When(x => x.Snf.HasValue);
        RuleFor(x => x.Remarks).MaximumLength(255);
    }
}
