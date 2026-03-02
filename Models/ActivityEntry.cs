using System;

namespace LifeOpsPlanner_Project.Models
{
    public class ActivityEntry
    {
        public int EntryId { get; set; }
        public int ActivityId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Status { get; set; } = "Planned";
        public decimal? Quantity { get; set; }
        public decimal? Amount { get; set; }
        public string? PaymentType { get; set; }
        public string? PaymentSource { get; set; }
        public string? Notes { get; set; }

        public string Person { get; set; } = "Me";

        public string? ActivityName { get; set; }   
    }
}
