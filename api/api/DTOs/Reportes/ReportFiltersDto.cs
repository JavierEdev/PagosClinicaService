namespace FacturacionAPI.DTOs.Reportes
{
    public class ReportFiltersDto
    {
        public DateTime desde { get; set; }
        public DateTime hasta { get; set; }
        public int? id_medico { get; set; }
        public string? procedimiento { get; set; }   // 🔹 antes era especialidad
    }
}