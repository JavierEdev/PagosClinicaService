using System.Collections.Generic;

namespace FacturacionAPI.DTOs
{
    public class GenerarFacturaResponse
    {
        public int id_factura { get; set; }
        public int id_paciente { get; set; }
        public DateTime fecha_emision { get; set; }
        public decimal monto_total { get; set; }
        public string estado_pago { get; set; } = "pendiente";
        public string tipo_pago { get; set; } = "efectivo";

        // Detalle informativo (opcional)
        public List<LineaFacturaItem>? lineas { get; set; }

        // PDF opcional (si generar_pdf = true)
        public string? pdf_base64 { get; set; }
        public string? pdf_filename { get; set; }
    }
}