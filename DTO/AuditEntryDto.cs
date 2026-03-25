namespace CompositionMaster.DTO
{
    public class AuditEntryDto
    {
        public DateTime ChangeDate { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public int EntityId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public int Author { get; set; }
        public string? AuthorName { get; set; }
    }
}