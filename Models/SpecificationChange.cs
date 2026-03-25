namespace CompositionMaster.Models
{
    public class SpecificationChange
    {
        public int Identifier { get; set; } // РК Идентификатор
        public string Name { get; set; } = string.Empty; // Наименование
        public int Owner { get; set; } // Владелец (ссылка на User.Identifier)
        public DateTime InputDate { get; set; } // Дата ввода
        public DateTime OutputDate { get; set; } // Дата вывода
        public bool IsMain { get; set; } // Является основной
        public DateTime ChangeDate { get; set; } // РК Дата изменений
        public string Comment { get; set; } = string.Empty; // Комментарий
        public int Author { get; set; } // РK Автор (ссылка на User.Identifier)
    }
}