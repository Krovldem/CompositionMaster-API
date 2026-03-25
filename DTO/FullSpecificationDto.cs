using System;
using System.Collections.Generic;

namespace CompositionMaster.DTO
{
    public class SpecificationDto
    {
        public int Identifier { get; set; }
        public DateTime InputDate { get; set; }
        public DateTime OutputDate { get; set; }
        public bool IsMain { get; set; }
        public int Owner { get; set; }
        public string? OwnerName { get; set; }
    }

    public class ComponentDto
    {
        public int LineNumber { get; set; }
        public int NomenclatureId { get; set; }
        public string? NomenclatureName { get; set; }
        public string? DSECode { get; set; }
        public string? NomenclatureTypeName { get; set; }
        public string? UnitOfMeasurementName { get; set; }
        public string? UnitAbbreviation { get; set; }
        public decimal Quantity { get; set; }
        public bool ParticipatesInCalculation { get; set; }
    }

    public class OperationCardDto
    {
        public int LineNumber { get; set; }
        public string Department { get; set; } = string.Empty;
        public string Section { get; set; } = string.Empty;
        public string Operation { get; set; } = string.Empty;
        public string Equipment { get; set; } = string.Empty;
        public decimal TimeNorm { get; set; }
        public decimal Tariff { get; set; }
        public decimal Cost { get; set; }
        public decimal Sum { get; set; }
    }

    public class SpecificationChangeDto
    {
        public int Identifier { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Owner { get; set; }
        public DateTime InputDate { get; set; }
        public DateTime OutputDate { get; set; }
        public bool IsMain { get; set; }
        public DateTime ChangeDate { get; set; }
        public string Comment { get; set; } = string.Empty;
        public int Author { get; set; }
        public string? AuthorName { get; set; }
    }

    public class SpecificationComponentChangeDto
    {
        public int Identifier { get; set; }
        public int LineNumber { get; set; }
        public int Nomenclature { get; set; }
        public string? NomenclatureName { get; set; }
        public decimal Quantity { get; set; }
        public bool ParticipatesInCalculation { get; set; }
        public DateTime ChangeDate { get; set; }
        public string? Comment { get; set; }
        public int Author { get; set; }
        public string? AuthorName { get; set; }
    }

    public class OperationCardChangeDto
    {
        public int Identifier { get; set; }
        public int LineNumber { get; set; }
        public string? Department { get; set; }
        public string? Section { get; set; }
        public string? Operation { get; set; }
        public string? Equipment { get; set; }
        public decimal TimeNorm { get; set; }
        public decimal Tariff { get; set; }
        public decimal Cost { get; set; }
        public decimal Sum { get; set; }
        public DateTime ChangeDate { get; set; }
        public string? Comment { get; set; }
        public int Author { get; set; }
        public string? AuthorName { get; set; }
    }

    public class FullSpecificationDto
    {
        public SpecificationDto Specification { get; set; } = new SpecificationDto();
        public List<ComponentDto> Components { get; set; } = new List<ComponentDto>();
        public List<OperationCardDto> OperationCards { get; set; } = new List<OperationCardDto>();
        public List<object> ChangeHistory { get; set; } = new List<object>();
    }

    public class SpecificationSummaryDto
    {
        public int SpecificationIdentifier { get; set; }
        public DateTime CreatedDate { get; set; }
        public int TotalComponents { get; set; }
        public decimal TotalQuantity { get; set; }
        public int InCalculationCount { get; set; }
        public int TotalOperations { get; set; }
        public decimal TotalCost { get; set; }
    }

    /// <summary>
    /// DTO для узла дерева спецификации
    /// </summary>
    public class TreeNodeDto
    {
        public int Identifier { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? DSECode { get; set; }
        public int NomenclatureType { get; set; }
        public decimal? Quantity { get; set; }
        public int Level { get; set; }
        public string UnitName { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public bool Expanded { get; set; } = true;
        public List<TreeNodeDto> Children { get; set; } = new List<TreeNodeDto>();
    }

    /// <summary>
    /// DTO для статистики дерева
    /// </summary>
    public class TreeStatisticsDto
    {
        public int TotalNodes { get; set; }
        public int TotalLevels { get; set; }
        public int MaxDepth { get; set; }
        public int AssembliesCount { get; set; }
        public int ComponentsCount { get; set; }
        public Dictionary<int, int> NodesByLevel { get; set; } = new Dictionary<int, int>();
    }

    /// <summary>
    /// DTO для полной спецификации с деревом
    /// </summary>
    public class FullSpecificationTreeDto
    {
        public SpecificationDto Specification { get; set; } = new SpecificationDto();
        public List<TreeNodeDto> Tree { get; set; } = new List<TreeNodeDto>();
        public TreeStatisticsDto Statistics { get; set; } = new TreeStatisticsDto();
    }
}