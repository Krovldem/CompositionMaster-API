namespace CompositionMaster.Models
{
    public class OperationCardChange
    {
        public int Identifier { get; set; } // РК Идентификатор
        public int LineNumber { get; set; } // РК Номер строки
        public string Department { get; set; } = string.Empty; // Подразделение
        public string Section { get; set; } = string.Empty; // Участок
        public string Operation { get; set; } = string.Empty; // Операция
        public string Equipment { get; set; } = string.Empty; // Оборудование
        public decimal TimeNorm { get; set; } // Норма времени
        public decimal Tariff { get; set; } // Тариф
        public decimal Cost { get; set; } // Стоимость
        public decimal Sum { get; set; } // Сумма
        public int Author { get; set; } // РK Автор (ссылка на User.Identifier)
    }
}