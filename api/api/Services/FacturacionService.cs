using  FacturacionAPI.Models;
using FacturacionAPI.DTOs;
using FacturacionAPI.DTOs.Reportes;
using FacturacionAPI.Repositories;

namespace FacturacionAPI.Services
{
    public class FacturacionService : IFacturacionService
    {
        private readonly InvoicePdfService _pdf;
        private readonly IFacturacionRepository _repo;
        public FacturacionService(IFacturacionRepository repo) => _repo = repo;

        public Task<Paciente?> ObtenerPacientePorIdAsync(int id_paciente)
            => _repo.ObtenerPacientePorIdAsync(id_paciente);

        public async Task<decimal> CalcularTotalConsultaAsync(int id_consulta)
        {
            if (id_consulta <= 0)
                throw new ArgumentOutOfRangeException(nameof(id_consulta), "id_consulta debe ser mayor que 0.");

            // Aquí podrías agregar reglas adicionales:
            // - validar que la consulta exista
            // - aplicar descuentos/recargos
            // - redondeos
            var total = await _repo.CalcularTotalConsultaAsync(id_consulta);
            return total;
        }
        public async Task<RegistrarPagoResponse> RegistrarPagoAsync(RegistrarPagoRequest request)
        {
            if (request.id_factura <= 0) throw new ArgumentOutOfRangeException(nameof(request.id_factura));
            if (request.monto <= 0) throw new ArgumentOutOfRangeException(nameof(request.monto));
            if (string.IsNullOrWhiteSpace(request.metodo_pago))
                throw new ArgumentException("metodo_pago requerido", nameof(request.metodo_pago));

            // Validar método permitido contra ENUM de BD
            var metodo = request.metodo_pago.ToLower().Trim();
            if (metodo != "efectivo" && metodo != "tarjeta" && metodo != "debito")
                throw new ArgumentException("metodo_pago debe ser 'efectivo', 'tarjeta' o 'debito'.");

            // Validar factura
            var existe = await _repo.FacturaExisteYActivaAsync(request.id_factura);
            if (!existe) throw new InvalidOperationException("Factura no existe o está cancelada.");

            // Insertar pago
            var id_pago = await _repo.InsertarPagoAsync(new RegistrarPagoRequest
            {
                id_factura = request.id_factura,
                monto = request.monto,
                metodo_pago = metodo,
                fecha_pago = request.fecha_pago
            });

            // Recalcular total pagado y estado
            var total_pagado = await _repo.ObtenerTotalPagadoPorFacturaAsync(request.id_factura);
            var resumen = await _repo.ObtenerResumenFacturaAsync(request.id_factura);

            // Tolerancia por centavos
            var saldo = resumen.monto_total - total_pagado;
            var estado = saldo <= 0.009m ? "pagado" : "pendiente";

            if (estado != resumen.estado_pago)
                await _repo.ActualizarEstadoFacturaAsync(request.id_factura, estado);

            return new RegistrarPagoResponse
            {
                id_pago = id_pago,
                id_factura = request.id_factura,
                monto = request.monto,
                metodo_pago = metodo,
                fecha_pago = request.fecha_pago ?? DateTime.Today,
                total_pagado = total_pagado,
                saldo_pendiente = saldo < 0 ? 0 : saldo,
                estado_factura = estado
            };
        }

        public Task<IEnumerable<PagoHistorialItem>> ObtenerHistorialPagosPorPacienteAsync(int id_paciente)
        {
            if (id_paciente <= 0) throw new ArgumentOutOfRangeException(nameof(id_paciente));
            return _repo.ObtenerHistorialPagosPorPacienteAsync(id_paciente);
        }


        public async Task<GenerarFacturaResponse> GenerarFacturaAsync(GenerarFacturaRequest req)
        {
            if (req.id_paciente <= 0) throw new ArgumentOutOfRangeException(nameof(req.id_paciente));
            if (req.id_consulta <= 0) throw new ArgumentOutOfRangeException(nameof(req.id_consulta));

            var metodo = (req.tipo_pago ?? "").Trim().ToLowerInvariant();
            if (metodo != "efectivo" && metodo != "tarjeta" && metodo != "debito")
                throw new ArgumentException("tipo_pago debe ser 'efectivo', 'tarjeta' o 'debito'.");

            var total = await _repo.CalcularTotalConsultaAsync(req.id_consulta);
            if (total <= 0) throw new InvalidOperationException("La consulta no tiene procedimientos con tarifa asociada.");

            var id_factura = await _repo.CrearFacturaAsync(req.id_paciente, req.id_consulta, total, metodo);
            var factura = await _repo.ObtenerFacturaPorIdAsync(id_factura);
            if (factura is null) throw new InvalidOperationException("No fue posible obtener la factura recién creada.");

            var lineas = await _repo.ObtenerLineasFacturaPorConsultaAsync(req.id_consulta);

            return new GenerarFacturaResponse
            {
                id_factura = factura.id_factura,
                id_paciente = factura.id_paciente,
                fecha_emision = factura.fecha_emision,
                monto_total = factura.monto_total,
                estado_pago = factura.estado_pago,
                tipo_pago = factura.tipo_pago,
                lineas = lineas
            };

        }
        public async Task<ReporteGeneralResponse> ObtenerReporteGeneralAsync(ReportFiltersDto f)
        {
            if (f.desde == default || f.hasta == default)
                throw new ArgumentException("Debe especificar 'desde' y 'hasta'.");
            if (f.desde > f.hasta)
                throw new ArgumentException("'desde' no puede ser mayor que 'hasta'.");

            var pacientes = await _repo.ContarPacientesAtendidosAsync(f.desde, f.hasta, f.id_medico, f.procedimiento);
            var citas = await _repo.ContarCitasProgramadasAsync(f.desde, f.hasta, f.id_medico, f.procedimiento);
            var ingresos = await _repo.ObtenerIngresosTotalesAproxAsync(f.desde, f.hasta, f.id_medico, f.procedimiento);
            var servicios = (await _repo.ObtenerIngresosPorServicioAsync(f.desde, f.hasta, f.id_medico, f.procedimiento)).ToList();
            var prod = (await _repo.ObtenerProductividadMedicaAsync(f.desde, f.hasta, f.id_medico, f.procedimiento)).ToList();

            return new ReporteGeneralResponse
            {
                pacientes_atendidos = pacientes,
                citas_programadas = citas,
                ingresos_totales_aprox = ingresos,
                ingresos_por_servicio = servicios,
                productividad_medica = prod
            };
        }

        public async Task<DashboardKpisResponse> ObtenerDashboardHoyAsync(DateTime? fecha, int? id_medico, string? especialidad, int topProcedimientos = 5)
        {
            var dia = (fecha ?? DateTime.Now).Date;
            var ahora = DateTime.Now; // para pendientes de hoy

            var tareas = new Task[]
            {
                _repo.ContarPacientesAtendidosEnDiaAsync(dia, id_medico, especialidad),
                _repo.ContarCitasConfirmadasEnDiaAsync(dia, id_medico, especialidad),
                _repo.ContarCitasConfirmadasFuturasHoyAsync(dia, ahora, id_medico, especialidad),
                _repo.SumarIngresosPagosEnDiaAsync(dia, id_medico, especialidad),
            };

            await Task.WhenAll(tareas);

            var pacientesAtendidos = ((Task<int>)tareas[0]).Result;
            var citasConfirmadas = ((Task<int>)tareas[1]).Result;
            var citasPendientes = ((Task<int>)tareas[2]).Result;
            var ingresosDia = ((Task<decimal>)tareas[3]).Result;

            var top = (await _repo.TopProcedimientosEnDiaAsync(dia, topProcedimientos, id_medico, especialidad)).ToList();

            return new DashboardKpisResponse
            {
                fecha = dia,
                pacientes_atendidos = pacientesAtendidos,
                citas_confirmadas = citasConfirmadas,
                citas_pendientes = citasPendientes,
                ingresos_acumulados = ingresosDia,
                top_procedimientos = top
            };
        }
    }
}
