namespace FacturacionAPI.DTOs
{
    public class LineaFacturaItem
    {
        public string procedimiento { get; set; } = string.Empty;
        public decimal precio { get; set; }
    }
}