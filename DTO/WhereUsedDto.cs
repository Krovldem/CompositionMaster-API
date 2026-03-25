namespace CompositionMaster.DTO
{
    public class WhereUsedDto
    {
        public int SpecificationId { get; set; }
        public string? SpecificationName { get; set; }
        public int LineNumber { get; set; }
        public decimal Quantity { get; set; }
        public int OwnerId { get; set; }
        public string? OwnerName { get; set; }
        public bool IsActive { get; set; }
    }
}