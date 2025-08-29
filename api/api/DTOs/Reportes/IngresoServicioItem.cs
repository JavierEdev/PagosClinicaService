namespace FacturacionAPI.DTOs.Reportes
{
    public class IngresoServicioItem
    {
        public string procedimiento { get; set; } = string.Empty;
        public int cantidad { get; set; }
        public decimal total { get; set; }
    }
}

