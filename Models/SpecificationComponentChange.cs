namespace CompositionMaster.Models
{
    public class SpecificationComponentChange
    {
        public int Identifier { get; set; } // РК Идентификатор
        public int LineNumber { get; set; } // РК Номер строки
        public int Nomenclature { get; set; } // Номенклатура (ссылка на Nomenclature.Identifier)
        public decimal Quantity { get; set; } // Количество
        public bool ParticipatesInCalculation { get; set; } // Участвует в расчете
        public DateTime ChangeDate { get; set; } // Дата изменения
        public string Comment { get; set; } = string.Empty; // Комментарий
        public int Author { get; set; } // РК Автор (ссылка на User.Identifier)
    }
}