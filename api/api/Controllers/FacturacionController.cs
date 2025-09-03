using FacturacionAPI.DTOs;
using FacturacionAPI.DTOs.Reportes;
using FacturacionAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace FacturacionAPI
{
    [ApiController]
    [Route("api/[controller]")]
    public class FacturacionController : ControllerBase
    {
        private readonly IFacturacionService _service;

        public FacturacionController(IFacturacionService service)
        {
            _service = service;
        }

        [HttpGet("consulta/{id_consulta:int}/total")]
        public async Task<IActionResult> ObtenerTotalConsulta(int id_consulta)
        {
            var total = await _service.CalcularTotalConsultaAsync(id_consulta);
            return Ok(new { id_consulta, total });
        }

        [HttpPost("generar")]
        [ProducesResponseType(typeof(GenerarFacturaResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GenerarFactura([FromBody] GenerarFacturaRequest req)
        {
            try
            {
                var resp = await _service.GenerarFacturaAsync(req);
                return Ok(resp);
            }
            catch (ArgumentOutOfRangeException ex) { return BadRequest(new { mensaje = ex.Message }); }
            catch (ArgumentException ex) { return BadRequest(new { mensaje = ex.Message }); }
            catch (InvalidOperationException ex) { return UnprocessableEntity(new { mensaje = ex.Message }); }
        }

        [HttpPost("pagos")]
        public async Task<IActionResult> RegistrarPago([FromBody] RegistrarPagoRequest request)
        {
            try
            {
                var resp = await _service.RegistrarPagoAsync(request);
                return Ok(resp);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return UnprocessableEntity(new { mensaje = ex.Message });
            }
        }

        [HttpGet("pagos/{id_paciente:int}")]
        public async Task<IActionResult> HistorialPagosPaciente(int id_paciente)
        {
            var items = await _service.ObtenerHistorialPagosPorPacienteAsync(id_paciente);
            return Ok(items);
        }
    }
}

