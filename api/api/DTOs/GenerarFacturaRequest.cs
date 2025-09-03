namespace FacturacionAPI.DTOs
{
    public class GenerarFacturaRequest
    {
        public int id_paciente { get; set; }
        public int id_consulta { get; set; }
        // 'efectivo' | 'tarjeta' | 'debito'
        public string tipo_pago { get; set; } = "efectivo";
    }
}
