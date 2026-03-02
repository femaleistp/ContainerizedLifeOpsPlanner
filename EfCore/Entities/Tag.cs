using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LifeOpsPlanner_Project.EfCore.Entities
{
    public class Tag
    {
        public int TagId { get; set; }

        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        public ICollection<ActivityEntryTag> ActivityEntryTags { get; set; } = new List<ActivityEntryTag>();
    }
}
