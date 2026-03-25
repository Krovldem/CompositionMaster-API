namespace CompositionMaster.DTO
{
    public class FieldDifferenceDto
    {
        public string FieldName { get; set; } = string.Empty;
        public string OldValue { get; set; } = string.Empty;
        public string NewValue { get; set; } = string.Empty;
    }

    public class ComparisonResultDto
    {
        public bool HasDifferences { get; set; }
        public string? Error { get; set; }
        public List<FieldDifferenceDto> Differences { get; set; } = new List<FieldDifferenceDto>();
    }
}