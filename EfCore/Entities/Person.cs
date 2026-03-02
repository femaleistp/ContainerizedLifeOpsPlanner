using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LifeOpsPlanner_Project.EfCore.Entities
{
    public class Person
    {
        [Key]
        public int PersonId { get; set; }

        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        // Navigation property for related ActivityEntries
        public ICollection<ActivityEntry> ActivityEntries { get; set; } = new List<ActivityEntry>();
    }
}
