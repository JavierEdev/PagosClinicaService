namespace FacturacionAPI.DTOs.Reportes
{
    public class DashboardKpisResponse
    {
        public DateTime fecha { get; set; }

        public int pacientes_atendidos { get; set; }

        public int citas_confirmadas { get; set; }
        public int citas_pendientes { get; set; }   // confirmadas con hora futura en el día

        public decimal ingresos_acumulados { get; set; } // suma Pagos del día

        // Top procedimientos del día
        public List<IngresoServicioItem> top_procedimientos { get; set; } = new();
    }
}
