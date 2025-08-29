namespace FacturacionAPI.DTOs
{
    public class GenerarFacturaRequest
    {
        public int id_paciente { get; set; }
        public int id_consulta { get; set; }
        // 'efectivo' | 'tarjeta' | 'debito'
        public string tipo_pago { get; set; } = "efectivo";

        // Si true, el servicio generará un PDF (base64) y lo incluirá en la respuesta
        public bool generar_pdf { get; set; } = false;
    }
}
