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
                procedimiento = string.IsNullOrWhiteSpace(procedimiento) ? null : procedimiento
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
    }
}
