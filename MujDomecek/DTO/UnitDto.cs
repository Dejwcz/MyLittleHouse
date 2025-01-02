namespace MujDomecek.DTO {
    public class UnitDto {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public UnitType UnitType { get; set; }
        public int PropertyId { get; set; }
        public int? ParentUnitId { get; set; }
        public string? ParentUnitName { get; set; }
    }
}
