using FacturacionAPI.Models;
using FacturacionAPI.DTOs;
using FacturacionAPI.DTOs.Reportes;

namespace FacturacionAPI.Services
{
    public interface IFacturacionService
    {
        Task<Paciente?> ObtenerPacientePorIdAsync(int id_paciente);
        Task<decimal> CalcularTotalConsultaAsync(int id_consulta);
        Task<RegistrarPagoResponse> RegistrarPagoAsync(RegistrarPagoRequest request);
        Task<IEnumerable<PagoHistorialItem>> ObtenerHistorialPagosPorPacienteAsync(int id_paciente);
        //generar factura
        Task<GenerarFacturaResponse> GenerarFacturaAsync(GenerarFacturaRequest req);
        //reportes generales
        Task<ReporteGeneralResponse> ObtenerReporteGeneralAsync(ReportFiltersDto filtros);
        //reporte productividad medica
        Task<DashboardKpisResponse> ObtenerDashboardHoyAsync(DateTime? fecha, int? id_medico, string? especialidad, int topProcedimientos = 5);

        // Endpoints unitarios (passthrough a repo)
        public Task<int> ContarPacientesAtendidosAsync(DateTime desde, DateTime hasta, int? id_medico);
        public Task<int> ContarCitasProgramadasAsync(DateTime desde, DateTime hasta, int? id_medico);
        public Task<decimal> ObtenerIngresosTotalesAproxAsync(DateTime desde, DateTime hasta, int? id_medico);
        public Task<IEnumerable<IngresoServicioItem>> ObtenerIngresosPorServicioAsync(DateTime desde, DateTime hasta, int? id_medico);
        public Task<IEnumerable<ProductividadItem>> ObtenerProductividadMedicaAsync(DateTime desde, DateTime hasta, int? id_medico);
    }
}