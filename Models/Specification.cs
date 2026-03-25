namespace CompositionMaster.Models
{
    public class Specification
    {
        public int Identifier { get; set; } // РК Идентификатор
        public DateTime InputDate { get; set; } // Дата ввода
        public DateTime OutputDate { get; set; } // Дата вывода
        public bool IsMain { get; set; } // Является основной
        public int Owner { get; set; } // Владелец (ссылка на User.Identifier)
    }
}