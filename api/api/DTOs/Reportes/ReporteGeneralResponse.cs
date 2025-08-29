using System.Collections.Generic;

namespace FacturacionAPI.DTOs.Reportes
{
    public class ReporteGeneralResponse
    {
        // Totales
        public int pacientes_atendidos { get; set; }
        public int citas_programadas { get; set; }
        public decimal ingresos_totales_aprox { get; set; }

        // Desgloses
        public List<IngresoServicioItem> ingresos_por_servicio { get; set; } = new();
        public List<ProductividadItem> productividad_medica { get; set; } = new();
    }
}
