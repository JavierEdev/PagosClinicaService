namespace FacturacionAPI.DTOs.Reportes
{
    public class ProductividadItem
    {
        public int id_medico { get; set; }
        public string medico { get; set; } = string.Empty;      // nombres + apellidos
        public string especialidad { get; set; } = string.Empty;
        public int consultas { get; set; }
        public decimal ingresos_aprox { get; set; }              // suma de tarifas por procedimientos del médico
    }
}
