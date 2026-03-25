namespace CompositionMaster.Models
{
    public class SpecificationComponent
    {
        public int Identifier { get; set; } // РK Идентификатор
        public int LineNumber { get; set; } // РK Номер строки
        public int Nomenclature { get; set; } // Номенклатура (ссылка на Nomenclature.Identifier)
        public decimal Quantity { get; set; } // Количество
        public bool ParticipatesInCalculation { get; set; } // Участвует в расчете
    }
}