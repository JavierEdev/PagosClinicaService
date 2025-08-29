namespace FacturacionAPI.Models
{
    public class Pagos
    {
        public int id_pago { get; set; }
        public int id_factura { get; set; }
        public decimal monto { get; set; }
        public DateTime fecha_pago { get; set; }
        public string metodo_pago { get; set; } = "efectivo"; // 'efectivo','tarjeta','debito'
    }
}