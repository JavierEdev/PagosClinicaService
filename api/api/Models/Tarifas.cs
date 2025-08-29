namespace FacturacionAPI.Models
{
    public class Tarifas
    {
        public int id_tarifa { get; set; }
        public int id_procedimiento { get; set; }
        public decimal precio { get; set; }
    }
}