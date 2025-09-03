using FacturacionAPI.Models;
using FacturacionAPI.DTOs;
using FacturacionAPI.DTOs.Reportes;

namespace FacturacionAPI.Repositories
{
        public interface IFacturacionRepository
        {
            Task<Paciente?> ObtenerPacientePorIdAsync(int id_paciente);
            Task<decimal> CalcularTotalConsultaAsync(int id_consulta);

            // Pagos    
            Task<bool> FacturaExisteYActivaAsync(int id_factura); // no cancelada
            Task<(int id_paciente, decimal monto_total, string estado_pago)> ObtenerResumenFacturaAsync(int id_factura);
            Task<int> InsertarPagoAsync(RegistrarPagoRequest req);
            Task<decimal> ObtenerTotalPagadoPorFacturaAsync(int id_factura);
            Task ActualizarEstadoFacturaAsync(int id_factura, string nuevo_estado);
            Task<IEnumerable<PagoHistorialItem>> ObtenerHistorialPagosPorPacienteAsync(int id_paciente);

            // Factura y lineas
            Task<bool> ConsultaDePacienteExisteAsync(int id_consulta, int id_paciente);
            Task<int> CrearFacturaAsync(int id_paciente,int id_factura, decimal monto_total, string tipo_pago);
            Task<Models.Facturacion?> ObtenerFacturaPorIdAsync(int id_factura);
            Task<List<LineaFacturaItem>> ObtenerLineasFacturaPorConsultaAsync(int id_consulta);

            //Reportes general
            Task<int> ContarPacientesAtendidosAsync(DateTime desde, DateTime hasta, int? id_medico, string? procedimiento);
            Task<int> ContarCitasProgramadasAsync(DateTime desde, DateTime hasta, int? id_medico, string? procedimiento);
            Task<decimal> ObtenerIngresosTotalesAproxAsync(DateTime desde, DateTime hasta, int? id_medico, string? procedimiento);
            Task<IEnumerable<IngresoServicioItem>> ObtenerIngresosPorServicioAsync(DateTime desde, DateTime hasta, int? id_medico, string? procedimiento);
            Task<IEnumerable<ProductividadItem>> ObtenerProductividadMedicaAsync(DateTime desde, DateTime hasta, int? id_medico, string? procedimiento);

            //Dashboard KPIs    
            Task<int> ContarPacientesAtendidosEnDiaAsync(DateTime dia, int? id_medico, string? especialidad);
            Task<int> ContarCitasConfirmadasEnDiaAsync(DateTime dia, int? id_medico, string? especialidad);
            Task<int> ContarCitasConfirmadasFuturasHoyAsync(DateTime dia, DateTime ahora, int? id_medico, string? especialidad);
            Task<decimal> SumarIngresosPagosEnDiaAsync(DateTime dia, int? id_medico, string? especialidad);
            Task<IEnumerable<IngresoServicioItem>> TopProcedimientosEnDiaAsync(DateTime dia, int top, int? id_medico, string? especialidad);
    }
}
