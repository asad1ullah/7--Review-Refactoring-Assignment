namespace LegacyTaskManager.Api.Models
{
    public class TaskItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public string Status { get; set; } = "Pending";
        public int? AssignedUserId { get; set; }
        public int Priority { get; set; } = 2;
        public string CreatedBy { get; set; } = "system";
    }
}
