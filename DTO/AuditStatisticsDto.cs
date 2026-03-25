namespace CompositionMaster.DTO
{
    public class AuditStatisticsDto
    {
        public DateTime PeriodFrom { get; set; }
        public DateTime PeriodTo { get; set; }
        public int TotalChanges { get; set; }
        public Dictionary<string, int> ChangesByUser { get; set; } = new();
        public Dictionary<string, int> ChangesByEntityType { get; set; } = new();
    }
}