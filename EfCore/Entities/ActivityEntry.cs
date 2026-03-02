using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LifeOpsPlanner_Project.EfCore.Entities;

public partial class ActivityEntry
{
    [Key]
    public int EntryId { get; set; }

    public int ActivityId { get; set; }

    [StringLength(200)]
    public string Title { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime? StartTime { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? EndTime { get; set; }

    [StringLength(50)]
    public string Status { get; set; } = null!;

    [Column(TypeName = "decimal(10, 2)")]
    public decimal? Quantity { get; set; }

    [Column(TypeName = "decimal(10, 2)")]
    public decimal? Amount { get; set; }

    [StringLength(50)]
    public string? PaymentType { get; set; }

    [StringLength(50)]
    public string? PaymentSource { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public int PersonId { get; set; }  // Foreign key to Person

    public Person Person { get; set; } = null!;  // navigation property to Person

    [ForeignKey("ActivityId")]
    [InverseProperty("ActivityEntries")]
    public virtual Activity Activity { get; set; } = null!;

    public virtual ICollection<ActivityEntryTag> ActivityEntryTags { get; set; } = new List<ActivityEntryTag>();
}
