namespace FacturacionAPI.DTOs
{
    public class RegistrarPagoRequest
    {
        public int id_factura { get; set; }
        public decimal monto { get; set; }
        public string metodo_pago { get; set; } = "efectivo"; // efectivo, tarjeta, debito
        public DateTime? fecha_pago { get; set; } // opcional
    }
}


