using FacturacionAPI.Models;
using FacturacionAPI.DTOs;
using FacturacionAPI.DTOs.Reportes;

namespace FacturacionAPI.Services
{
    public interface IFacturacionService
    {
        Task<Paciente?> ObtenerPacientePorIdAsync(int id_paciente);
        Task<decimal> CalcularTotalConsultaAsync(int id_consulta);
        //registrar pago
        Task<RegistrarPagoResponse> RegistrarPagoAsync(RegistrarPagoRequest request);
        Task<IEnumerable<PagoHistorialItem>> ObtenerHistorialPagosPorPacienteAsync(int id_paciente);
        //generar factura
        Task<GenerarFacturaResponse> GenerarFacturaAsync(GenerarFacturaRequest req);
        //reportes generales
        Task<ReporteGeneralResponse> ObtenerReporteGeneralAsync(ReportFiltersDto filtros);
        //reporte productividad medica
        Task<DashboardKpisResponse> ObtenerDashboardHoyAsync(DateTime? fecha, int? id_medico, string? especialidad, int topProcedimientos = 5);
    }
}