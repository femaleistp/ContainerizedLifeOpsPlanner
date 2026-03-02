using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LifeOpsPlanner_Project.EfCore.Entities;

public partial class Activity
{
    [Key]
    public int ActivityId { get; set; }

    [StringLength(100)]
    public string Name { get; set; } = null!;

    [InverseProperty("Activity")]
    public virtual ICollection<ActivityEntry> ActivityEntries { get; set; } = new List<ActivityEntry>();
}
