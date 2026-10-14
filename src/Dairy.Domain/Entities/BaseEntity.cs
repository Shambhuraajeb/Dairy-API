namespace Dairy.Domain.Entities;

/// <summary>Common columns present on (almost) every table. Optional base for row models.</summary>
public abstract class BaseEntity
{
    public long Id { get; set; }
    public long? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public long? ModifiedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public bool IsActive { get; set; } = true;
}
