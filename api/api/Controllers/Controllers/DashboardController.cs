using System.Data;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;

namespace FacturacionAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly MySqlConnection _conn;
    public DashboardController(MySqlConnection conn) => _conn = conn;

    static (DateTime d1, DateTime d2) Clamp(string? d, string? h)
    {
        var desde = DateTime.Parse($"{(string.IsNullOrWhiteSpace(d) ? DateTime.Today.ToString("yyyy-MM-dd") : d)}T00:00:00");
        var hasta = DateTime.Parse($"{(string.IsNullOrWhiteSpace(h) ? DateTime.Today.ToString("yyyy-MM-dd") : h)}T23:59:59");
        return (desde, hasta);
    }

    async Task EnsureOpenAsync()
    {
        if (_conn.State != ConnectionState.Open)
            await _conn.OpenAsync();
    }

    [HttpGet("especialidades")]
    public async Task<IActionResult> GetEspecialidades()
    {
        await EnsureOpenAsync();
        var rows = await _conn.QueryAsync<string>(@"
            SELECT DISTINCT m.especialidad
            FROM pacientes_service.medicos m
            WHERE m.especialidad IS NOT NULL AND m.especialidad <> ''
            ORDER BY m.especialidad;");
        return Ok(rows);
    }

    [HttpGet("kpis")]
    public async Task<IActionResult> GetKpis([FromQuery] string? desde, [FromQuery] string? hasta, [FromQuery] string? especialidad = null)
    {
        await EnsureOpenAsync();
        var (d1, d2) = Clamp(desde, hasta);

        var citas = await _conn.QueryAsync(@"
            SELECT LOWER(c.estado) AS estado, COUNT(*) cnt
            FROM pacientes_service.citasmedicas c
            LEFT JOIN pacientes_service.medicos m ON m.id_medico = c.id_medico
            WHERE c.fecha BETWEEN @d1 AND @d2
              AND (@esp IS NULL OR @esp = '' OR m.especialidad = @esp)
            GROUP BY LOWER(c.estado);", new { d1, d2, esp = especialidad });

        int cConf = 0, cRep = 0, cCanc = 0;
        foreach (var r in citas)
        {
            string st = (string?)r.estado ?? "";
            int n = Convert.ToInt32(r.cnt);
            if (st == "confirmada") cConf = n;
            else if (st == "reprogramada") cRep = n;
            else if (st == "cancelada") cCanc = n;
        }

        var cons = await _conn.QuerySingleAsync<(long total, long conProc)>(@"
            SELECT 
                COUNT(*) AS total,
                SUM(CASE WHEN x.tiene = 1 THEN 1 ELSE 0 END) AS conProc
            FROM (
                SELECT cm.id_consulta,
                       CASE WHEN EXISTS (
                            SELECT 1 FROM pacientes_service.procedimientosmedicos pm 
                            WHERE pm.id_consulta = cm.id_consulta
                       ) THEN 1 ELSE 0 END AS tiene
                FROM pacientes_service.consultasmedicas cm
                INNER JOIN pacientes_service.citasmedicas c ON c.id_cita = cm.id_cita
                LEFT JOIN pacientes_service.medicos m ON m.id_medico = c.id_medico
                WHERE cm.fecha BETWEEN @d1 AND @d2
                  AND (@esp IS NULL OR @esp = '' OR m.especialidad = @esp)
            ) x;", new { d1, d2, esp = especialidad });

        int pacientesAt = await _conn.QuerySingleAsync<int>(@"
            SELECT COUNT(DISTINCT cm.id_paciente)
            FROM pacientes_service.consultasmedicas cm
            INNER JOIN pacientes_service.citasmedicas c ON c.id_cita = cm.id_cita
            LEFT JOIN pacientes_service.medicos m ON m.id_medico = c.id_medico
            WHERE cm.fecha BETWEEN @d1 AND @d2
              AND (@esp IS NULL OR @esp = '' OR m.especialidad = @esp);", new { d1, d2, esp = especialidad });

        decimal ingresosCobrados = await _conn.QuerySingleAsync<decimal>(@"
            SELECT IFNULL(SUM(p.monto), 0) 
            FROM pacientes_service.pagos p
            WHERE p.fecha_pago BETWEEN @d1 AND @d2;", new { d1, d2 });

        decimal pendientes = await _conn.QuerySingleAsync<decimal>(@"
            SELECT IFNULL(SUM(cat.precio_base), 0) AS totalPend
            FROM pacientes_service.consultasmedicas cm
            INNER JOIN pacientes_service.citasmedicas c ON c.id_cita = cm.id_cita
            LEFT JOIN pacientes_service.medicos m ON m.id_medico = c.id_medico
            INNER JOIN pacientes_service.procedimientosmedicos pm ON pm.id_consulta = cm.id_consulta
            INNER JOIN pacientes_service.catalogoprocedimientos cat ON cat.id_procedimiento_catalogo = pm.id_procedimiento_catalogo
            LEFT JOIN pacientes_service.facturacion f ON f.id_consulta = cm.id_consulta
            WHERE c.estado = 'confirmada'
              AND cm.fecha BETWEEN @d1 AND @d2
              AND (@esp IS NULL OR @esp = '' OR m.especialidad = @esp)
              AND (f.id_factura IS NULL OR f.estado_pago <> 'pagado');", new { d1, d2, esp = especialidad });

        return Ok(new
        {
            rango = new { desde = d1, hasta = d2, especialidad },
            citas = new
            {
                confirmadas = cConf,
                reprogramadas = cRep,
                canceladas = cCanc,
                programadas = cConf + cRep
            },
            consultas = new { total = cons.total, conProcedimientos = cons.conProc },
            pacientesAtendidos = pacientesAt,
            ingresos = new { cobradosQ = ingresosCobrados, pendientesQ = pendientes }
        });
    }

    [HttpGet("series/citas")]
    public async Task<IActionResult> GetSeriesCitas([FromQuery] string? desde, [FromQuery] string? hasta, [FromQuery] string? especialidad = null)
    {
        await EnsureOpenAsync();
        var (d1, d2) = Clamp(desde, hasta);

        var data = await _conn.QueryAsync(@"
            SELECT DATE(c.fecha) AS dia, LOWER(c.estado) AS est, COUNT(*) AS cnt
            FROM pacientes_service.citasmedicas c
            LEFT JOIN pacientes_service.medicos m ON m.id_medico = c.id_medico
            WHERE c.fecha BETWEEN @d1 AND @d2
              AND (@esp IS NULL OR @esp = '' OR m.especialidad = @esp)
            GROUP BY DATE(c.fecha), LOWER(c.estado)
            ORDER BY dia;", new { d1, d2, esp = especialidad });

        var dict = new Dictionary<DateTime, (int conf, int rep, int canc)>();
        foreach (var r in data)
        {
            DateTime day = (DateTime)r.dia;
            if (!dict.TryGetValue(day, out var t)) t = (0, 0, 0);
            string e = (string?)r.est ?? "";
            int n = Convert.ToInt32(r.cnt);
            if (e == "confirmada") t.conf += n;
            else if (e == "reprogramada") t.rep += n;
            else if (e == "cancelada") t.canc += n;
            dict[day] = t;
        }
        var result = dict.OrderBy(k => k.Key).Select(k => new {
            fecha = k.Key.ToString("yyyy-MM-dd"),
            confirmada = k.Value.conf,
            reprogramada = k.Value.rep,
            cancelada = k.Value.canc
        });
        return Ok(result);
    }

    [HttpGet("series/ingresos-mensual")]
    public async Task<IActionResult> GetIngresosMensual([FromQuery] string? desde, [FromQuery] string? hasta, [FromQuery] string? especialidad = null)
    {
        await EnsureOpenAsync();
        var (d1, d2) = Clamp(desde, hasta);

        var cobrados = await _conn.QueryAsync(@"
            SELECT DATE_FORMAT(p.fecha_pago, '%Y-%m-01') AS mes, IFNULL(SUM(p.monto),0) total
            FROM pacientes_service.pagos p
            WHERE p.fecha_pago BETWEEN @d1 AND @d2
            GROUP BY DATE_FORMAT(p.fecha_pago, '%Y-%m-01');", new { d1, d2 });

        var pendientes = await _conn.QueryAsync(@"
            SELECT DATE_FORMAT(cm.fecha, '%Y-%m-01') AS mes, IFNULL(SUM(cat.precio_base),0) total
            FROM pacientes_service.consultasmedicas cm
            INNER JOIN pacientes_service.citasmedicas c ON c.id_cita = cm.id_cita
            LEFT JOIN pacientes_service.medicos m ON m.id_medico = c.id_medico
            INNER JOIN pacientes_service.procedimientosmedicos pm ON pm.id_consulta = cm.id_consulta
            INNER JOIN pacientes_service.catalogoprocedimientos cat ON cat.id_procedimiento_catalogo = pm.id_procedimiento_catalogo
            LEFT JOIN pacientes_service.facturacion f ON f.id_consulta = cm.id_consulta
            WHERE c.estado = 'confirmada'
              AND cm.fecha BETWEEN @d1 AND @d2
              AND (@esp IS NULL OR @esp = '' OR m.especialidad = @esp)
              AND (f.id_factura IS NULL OR f.estado_pago <> 'pagado')
            GROUP BY DATE_FORMAT(cm.fecha, '%Y-%m-01');", new { d1, d2, esp = especialidad });

        var map = new Dictionary<string, (decimal cob, decimal pend)>();

        foreach (var r in cobrados)
        {
            string m = (string)r.mes;
            var pendVal = map.ContainsKey(m) ? map[m].pend : 0m;
            map[m] = (cob: (decimal)r.total, pend: pendVal);
        }

        foreach (var r in pendientes)
        {
            string m = (string)r.mes;
            var prev = map.ContainsKey(m) ? map[m] : (cob: 0m, pend: 0m);
            map[m] = (cob: prev.cob, pend: (decimal)r.total);
        }

        var result = map
            .OrderBy(k => k.Key)
            .Select(k => new { mes = k.Key[..7], cobrados = k.Value.cob, pendientes = k.Value.pend });

        return Ok(result);
    }

    [HttpGet("top/procedimientos")]
    public async Task<IActionResult> GetTopProcedimientos([FromQuery] string? desde, [FromQuery] string? hasta, [FromQuery] string? especialidad = null, [FromQuery] int top = 10)
    {
        await EnsureOpenAsync();
        var (d1, d2) = Clamp(desde, hasta);

        var rows = await _conn.QueryAsync(@"
            SELECT cat.nombre AS procedimiento,
                   COUNT(*) AS cantidad,
                   IFNULL(SUM(cat.precio_base),0) AS total
            FROM pacientes_service.procedimientosmedicos pm
            INNER JOIN pacientes_service.consultasmedicas cm ON cm.id_consulta = pm.id_consulta
            INNER JOIN pacientes_service.catalogoprocedimientos cat ON cat.id_procedimiento_catalogo = pm.id_procedimiento_catalogo
            INNER JOIN pacientes_service.facturacion f ON f.id_consulta = cm.id_consulta
            INNER JOIN pacientes_service.citasmedicas c ON c.id_cita = cm.id_cita
            LEFT JOIN pacientes_service.medicos m ON m.id_medico = c.id_medico
            WHERE f.estado_pago = 'pagado'
              AND cm.fecha BETWEEN @d1 AND @d2
              AND (@esp IS NULL OR @esp = '' OR m.especialidad = @esp)
            GROUP BY cat.nombre
            ORDER BY total DESC
            LIMIT @top;", new { d1, d2, esp = especialidad, top });

        return Ok(rows);
    }

    [HttpGet("citas-por-especialidad")]
    public async Task<IActionResult> GetCitasPorEspecialidad([FromQuery] string? desde, [FromQuery] string? hasta, [FromQuery] string? especialidad = null)
    {
        await EnsureOpenAsync();
        var (d1, d2) = Clamp(desde, hasta);

        var rows = await _conn.QueryAsync(@"
            SELECT m.especialidad, COUNT(*) AS cantidad
            FROM pacientes_service.citasmedicas c
            LEFT JOIN pacientes_service.medicos m ON m.id_medico = c.id_medico
            WHERE c.fecha BETWEEN @d1 AND @d2
              AND (@esp IS NULL OR @esp = '' OR m.especialidad = @esp)
            GROUP BY m.especialidad
            ORDER BY cantidad DESC;", new { d1, d2, esp = especialidad });

        return Ok(rows);
    }

    [HttpGet("ultimas-recetas")]
    public async Task<IActionResult> GetUltimasRecetas([FromQuery] string? desde, [FromQuery] string? hasta, [FromQuery] string? especialidad = null, [FromQuery] int take = 10)
    {
        await EnsureOpenAsync();
        var (d1, d2) = Clamp(desde, hasta);

        var rows = await _conn.QueryAsync(@"
            SELECT r.id_receta, r.id_consulta, r.medicamento, r.dosis, r.frecuencia, r.duracion, cm.fecha
            FROM pacientes_service.recetasmedicas r
            INNER JOIN pacientes_service.consultasmedicas cm ON cm.id_consulta = r.id_consulta
            INNER JOIN pacientes_service.citasmedicas c ON c.id_cita = cm.id_cita
            LEFT JOIN pacientes_service.medicos m ON m.id_medico = c.id_medico
            WHERE cm.fecha BETWEEN @d1 AND @d2
              AND (@esp IS NULL OR @esp = '' OR m.especialidad = @esp)
            ORDER BY cm.fecha DESC
            LIMIT @take;", new { d1, d2, esp = especialidad, take });

        return Ok(rows);
    }

    [HttpGet("citas-recientes")]
    public async Task<IActionResult> GetCitasRecientes([FromQuery] string? desde, [FromQuery] string? hasta, [FromQuery] string? especialidad = null, [FromQuery] int take = 10)
    {
        await EnsureOpenAsync();
        var (d1, d2) = Clamp(desde, hasta);

        var rows = await _conn.QueryAsync(@"
            SELECT c.id_cita AS id, c.fecha, LOWER(c.estado) AS estado, c.id_paciente, c.id_medico,
                   CONCAT(m.nombres,' ',m.apellidos) AS medico, m.especialidad
            FROM pacientes_service.citasmedicas c
            LEFT JOIN pacientes_service.medicos m ON m.id_medico = c.id_medico
            WHERE c.fecha BETWEEN @d1 AND @d2
              AND (@esp IS NULL OR @esp = '' OR m.especialidad = @esp)
            ORDER BY c.fecha DESC
            LIMIT @take;", new { d1, d2, esp = especialidad, take });

        return Ok(rows);
    }
}
