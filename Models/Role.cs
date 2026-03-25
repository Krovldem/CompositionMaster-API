namespace CompositionMaster.Models
{
    public class Role
    {
        public int Identifier { get; set; } // ПК Идентификатор
        public string Name { get; set; } = string.Empty; // наименование
        public string Comment { get; set; } = string.Empty; // комментарий
    }
}