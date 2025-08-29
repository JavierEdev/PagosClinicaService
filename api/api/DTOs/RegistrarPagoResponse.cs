namespace FacturacionAPI.DTOs
{
    public class RegistrarPagoResponse
    {
        public int id_pago { get; set; }
        public int id_factura { get; set; }
        public decimal monto { get; set; }
        public string metodo_pago { get; set; } = string.Empty;
        public DateTime fecha_pago { get; set; }
        public decimal total_pagado { get; set; }
        public decimal saldo_pendiente { get; set; }
        public string estado_factura { get; set; } = string.Empty;
    }
}


