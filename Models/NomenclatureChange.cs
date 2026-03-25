namespace CompositionMaster.Models
{
    public class NomenclatureChange
    {
        public int Identifier { get; set; } // РК Идентификатор
        public DateTime IntroducedIntoUse { get; set; } // Введен в использование
        public string DSECode { get; set; } = string.Empty; // Код ДСЕ
        public string Name { get; set; } = string.Empty; // Наименование
        public string SubsystemCode { get; set; } = string.Empty; // Код подсистемы
        public DateTime ChangeDate { get; set; } // РК Дата изменения
        public string Comment { get; set; } = string.Empty; // Комментарий
        public int Author { get; set; } // РK Автор (ссылка на User.Identifier)
    }
}