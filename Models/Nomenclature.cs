namespace CompositionMaster.Models
{
    public class Nomenclature
    {
        public int Identifier { get; set; } // РK Идентификатор
        public DateTime IntroducedIntoUse { get; set; } // Введен в использование
        public string DSECode { get; set; } = string.Empty; // Код ДСЕ
        public string Name { get; set; } = string.Empty; // Наименование
        public string SubsystemCode { get; set; } = string.Empty; // Код подсистемы
        public int NomenclatureType { get; set; } // Вид номенклатуры (ссылка на NomenclatureType.Identifier)
        public int UnitOfMeasurement { get; set; } // Единица измерения (ссылка на UnitOfMeasurement.Identifier)
    }
}