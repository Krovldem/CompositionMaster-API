namespace CompositionMaster.Models
{
    public class UnitOfMeasurement
    {
        public int Identifier { get; set; } // ПК Идентификатор
        public string Name { get; set; } = string.Empty; // наименование
        public string Abbreviation { get; set; } = string.Empty; // Сокращение
    }
}
