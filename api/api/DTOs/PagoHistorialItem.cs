namespace FacturacionAPI.DTOs
{
    public class PagoHistorialItem
    {
        public int id_pago { get; set; }
        public int id_factura { get; set; }
        public DateTime fecha_pago { get; set; }
        public decimal monto { get; set; }
        public string metodo_pago { get; set; } = string.Empty;

        // Información de la factura relacionada
        public DateTime fecha_emision { get; set; }
        public decimal monto_total { get; set; }
        public string estado_pago { get; set; } = string.Empty;
    }
}
