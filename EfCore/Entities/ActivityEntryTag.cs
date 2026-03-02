namespace LifeOpsPlanner_Project.EfCore.Entities
{
    public class ActivityEntryTag
    {
        public int EntryId { get; set; }
        public ActivityEntry ActivityEntry { get; set; } = null!;

        public int TagId { get; set; }
        public Tag Tag { get; set; } = null!;
    }
}