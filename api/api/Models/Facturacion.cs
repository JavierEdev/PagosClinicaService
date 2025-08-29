namespace FacturacionAPI.Models
{
    public class Facturacion
    {
        public int id_factura { get; set; }
        public int id_paciente { get; set; }
        public DateTime fecha_emision { get; set; }
        public decimal monto_total { get; set; }
        public string estado_pago { get; set; } = "pendiente"; // 'pendiente','pagado','cancelado'
        public string tipo_pago { get; set; } = "efectivo";    // 'efectivo','tarjeta','debito'
    }
}