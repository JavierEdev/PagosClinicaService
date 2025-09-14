using Microsoft.AspNetCore.Mvc;
using FacturacionAPI.Services;
using FacturacionAPI.DTOs.Reportes;

namespace FacturacionAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // → /api/Reportes
    public class ReportesController : ControllerBase
    {
        private readonly IFacturacionService _service;
        public ReportesController(IFacturacionService service) => _service = service;

        /// <summary>
        /// Reporte general: pacientes atendidos, citas programadas, ingresos por servicios y productividad médica.
        /// Filtros opcionales: id_medico, especialidad. Fechas inclusivas.
        /// </summary>
        [HttpGet("general")]
        [ProducesResponseType(typeof(ReporteGeneralResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> General(
             [FromQuery] DateTime desde,
             [FromQuery] DateTime hasta,
             [FromQuery] int? id_medico,
             [FromQuery] string? procedimiento)  
        {
            var filtros = new ReportFiltersDto
            {
                desde = desde,
                hasta = hasta,
                id_medico = id_medico,
            };
            var resp = await _service.ObtenerReporteGeneralAsync(filtros);
            return Ok(resp);
        }

        [HttpGet("dashboard")]
        [ProducesResponseType(typeof(DashboardKpisResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> Dashboard(
        [FromQuery] DateTime? fecha,           // opcional, por defecto hoy
        [FromQuery] int? id_medico,            // opcional
        [FromQuery] string? especialidad,      // opcional
        [FromQuery] int top = 5                // top procedimientos
)
        {
            var resp = await _service.ObtenerDashboardHoyAsync(fecha, id_medico, string.IsNullOrWhiteSpace(especialidad) ? null : especialidad, top);
            return Ok(resp);
        }

        // 🔹 Pacientes atendidos
        [HttpGet("pacientes-atendidos")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> PacientesAtendidos(
            [FromQuery] DateTime desde,
            [FromQuery] DateTime hasta,
            [FromQuery] int? id_medico)
        {
            if (desde == default || hasta == default || desde > hasta)
                return BadRequest(new { mensaje = "Parámetros 'desde' y 'hasta' inválidos." });

            var total = await _service.ContarPacientesAtendidosAsync(desde, hasta, id_medico);
            return Ok(new { desde, hasta, id_medico, pacientes_atendidos = total });
        }

        // 🔹 Citas programadas
        [HttpGet("citas")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> CitasProgramadas(
            [FromQuery] DateTime desde,
            [FromQuery] DateTime hasta,
            [FromQuery] int? id_medico)
        {
            if (desde == default || hasta == default || desde > hasta)
                return BadRequest(new { mensaje = "Parámetros 'desde' y 'hasta' inválidos." });

            var total = await _service.ContarCitasProgramadasAsync(desde, hasta, id_medico);
            return Ok(new { desde, hasta, id_medico, citas_programadas = total });
        }

        // 🔹 Ingresos (aprox por tarifas de procedimientos)
        [HttpGet("ingresos")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> Ingresos(
            [FromQuery] DateTime desde,
            [FromQuery] DateTime hasta,
            [FromQuery] int? id_medico)
        {
            if (desde == default || hasta == default || desde > hasta)
                return BadRequest(new { mensaje = "Parámetros 'desde' y 'hasta' inválidos." });

            var total = await _service.ObtenerIngresosTotalesAproxAsync(desde, hasta, id_medico);
            return Ok(new { desde, hasta, id_medico, ingresos_totales_aprox = total });
        }

        // 🔹 Ingresos por servicio (procedimiento)
        [HttpGet("ingresos-por-servicio")]
        [ProducesResponseType(typeof(IEnumerable<IngresoServicioItem>), StatusCodes.Status200OK)]
        public async Task<IActionResult> IngresosPorServicio(
            [FromQuery] DateTime desde,
            [FromQuery] DateTime hasta,
            [FromQuery] int? id_medico)
        {
            if (desde == default || hasta == default || desde > hasta)
                return BadRequest(new { mensaje = "Parámetros 'desde' y 'hasta' inválidos." });

            var items = await _service.ObtenerIngresosPorServicioAsync(desde, hasta, id_medico);
            return Ok(items);
        }

        // 🔹 Productividad médica (por médico)
        [HttpGet("productividad")]
        [ProducesResponseType(typeof(IEnumerable<ProductividadItem>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Productividad(
            [FromQuery] DateTime desde,
            [FromQuery] DateTime hasta,
            [FromQuery] int? id_medico)
        {
            if (desde == default || hasta == default || desde > hasta)
                return BadRequest(new { mensaje = "Parámetros 'desde' y 'hasta' inválidos." });

            var items = await _service.ObtenerProductividadMedicaAsync(desde, hasta, id_medico);
            return Ok(items);
        }

        // GET api/reportes/productividad-medicos?desde=2025-09-01&hasta=2025-09-14&idMedico=2
        [HttpGet("productividad-medicos")]
        public async Task<IActionResult> GetReporteProductividad(
            DateTime desde,
            DateTime hasta,
            int? idMedico = null)
        {
            var reporte = await _service.ObtenerReporteProductividadAsync(desde, hasta, idMedico);
            return Ok(reporte);
        }

    }
}
