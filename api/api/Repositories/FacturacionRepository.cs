using Dapper;
using FacturacionAPI.DTOs;
using FacturacionAPI.DTOs.Reportes;
using FacturacionAPI.Models;
using MySql.Data.MySqlClient;
using System.Data;

namespace FacturacionAPI.Repositories
{
  
        public class FacturacionRepository : IFacturacionRepository
        {
            private readonly MySqlConnection _conn;

            public FacturacionRepository(MySqlConnection conn)
            {
                _conn = conn;
            }

            private async Task EnsureOpenAsync()
            {
                if (_conn.State != ConnectionState.Open)
                    await _conn.OpenAsync();
            }

        public async Task<Paciente?> ObtenerPacientePorIdAsync(int id_paciente)
            {
                if (_conn.State != ConnectionState.Open)
                    await _conn.OpenAsync();

                const string sql = @"
                SELECT id_paciente, nombres, apellidos, dpi, fecha_nacimiento, sexo, direccion, telefono, correo, estado_civil
                FROM Pacientes
                WHERE id_paciente = @id;";

                return await _conn.QueryFirstOrDefaultAsync<Paciente>(sql, new { id = id_paciente });
            }

            public async Task<decimal> CalcularTotalConsultaAsync(int id_consulta)
            {
                if (_conn.State != ConnectionState.Open)
                    await _conn.OpenAsync();

                const string sql = @"
                    SELECT COALESCE(SUM(t.precio), 0)
                    FROM ProcedimientosMedicos pm
                    INNER JOIN Tarifas t ON t.id_procedimiento = pm.id_procedimiento
                    WHERE pm.id_consulta = @id;";

                // Dapper devuelve el escalar tipado
                var total = await _conn.ExecuteScalarAsync<decimal>(sql, new { id = id_consulta });
                return total;
            }

            public async Task<bool> FacturaExisteYActivaAsync(int id_factura)
            {
                await EnsureOpenAsync();
                const string sql = @"
                    SELECT COUNT(*) 
                    FROM Facturacion 
                    WHERE id_factura = @id AND estado_pago <> 'cancelado';";
                var c = await _conn.ExecuteScalarAsync<long>(sql, new { id = id_factura });
                return c > 0;
            }

            public async Task<(int id_paciente, decimal monto_total, string estado_pago)> ObtenerResumenFacturaAsync(int id_factura)
            {
                await EnsureOpenAsync();
                const string sql = @"
                    SELECT id_paciente, monto_total, estado_pago
                    FROM Facturacion
                    WHERE id_factura = @id;";
                var row = await _conn.QueryFirstOrDefaultAsync(sql, new { id = id_factura });
                if (row == null) throw new InvalidOperationException("Factura no encontrada.");

                int id_paciente = row.id_paciente;
                decimal monto_total = row.monto_total;
                string estado_pago = row.estado_pago;
                return (id_paciente, monto_total, estado_pago);
            }

            public async Task<int> InsertarPagoAsync(RegistrarPagoRequest req)
            {
                await EnsureOpenAsync();

                const string sql = @"
                    INSERT INTO Pagos (id_factura, monto, fecha_pago, metodo_pago)
                    VALUES (@id_factura, @monto, COALESCE(@fecha_pago, CURRENT_DATE()), @metodo_pago);
                    SELECT LAST_INSERT_ID();";

                var id_pago = await _conn.ExecuteScalarAsync<int>(sql, new
                {
                    req.id_factura,
                    req.monto,
                    fecha_pago = req.fecha_pago,   // puede ser null
                    req.metodo_pago
                });

                return id_pago;
            }

            public async Task<decimal> ObtenerTotalPagadoPorFacturaAsync(int id_factura)
            {
                await EnsureOpenAsync();
                const string sql = @"SELECT COALESCE(SUM(monto), 0) FROM Pagos WHERE id_factura = @id;";
                return await _conn.ExecuteScalarAsync<decimal>(sql, new { id = id_factura });
            }

            public async Task ActualizarEstadoFacturaAsync(int id_factura, string nuevo_estado)
            {
                await EnsureOpenAsync();
                const string sql = @"UPDATE Facturacion SET estado_pago = @estado WHERE id_factura = @id;";
                await _conn.ExecuteAsync(sql, new { estado = nuevo_estado, id = id_factura });
            }

            public async Task<IEnumerable<PagoHistorialItem>> ObtenerHistorialPagosPorPacienteAsync(int id_paciente)
            {
                await EnsureOpenAsync();
                const string sql = @"
                    SELECT 
                        p.id_pago, p.id_factura, p.fecha_pago, p.monto, p.metodo_pago,
                        f.fecha_emision, f.monto_total, f.estado_pago
                    FROM Pagos p
                    INNER JOIN Facturacion f ON f.id_factura = p.id_factura
                    WHERE f.id_paciente = @id
                    ORDER BY p.fecha_pago DESC, p.id_pago DESC;";

                return await _conn.QueryAsync<PagoHistorialItem>(sql, new { id = id_paciente });
            }

            public async Task<bool> ConsultaDePacienteExisteAsync(int id_consulta, int id_paciente)
            {
                await EnsureOpenAsync();
                const string sql = @"
                    SELECT COUNT(*) 
                    FROM ConsultasMedicas 
                    WHERE id_consulta = @id_consulta AND id_paciente = @id_paciente;";
                var c = await _conn.ExecuteScalarAsync<long>(sql, new { id_consulta, id_paciente });
                return c > 0;
            }

            public async Task<int> CrearFacturaAsync(int id_paciente, decimal monto_total, string tipo_pago)
            {
                await EnsureOpenAsync();
                const string sql = @"
                    INSERT INTO Facturacion (id_paciente, fecha_emision, monto_total, estado_pago, tipo_pago)
                    VALUES (@id_paciente, CURRENT_DATE(), @monto_total, 'pendiente', @tipo_pago);
                    SELECT LAST_INSERT_ID();";
                return await _conn.ExecuteScalarAsync<int>(sql, new { id_paciente, monto_total, tipo_pago });
            }

            public async Task<Facturacion?> ObtenerFacturaPorIdAsync(int id_factura)
            {
                await EnsureOpenAsync();
                const string sql = @"
                    SELECT id_factura, id_paciente, fecha_emision, monto_total, estado_pago, tipo_pago
                    FROM Facturacion
                    WHERE id_factura = @id;";
                return await _conn.QueryFirstOrDefaultAsync<Facturacion>(sql, new { id = id_factura });
            }

            public async Task<List<LineaFacturaItem>> ObtenerLineasFacturaPorConsultaAsync(int id_consulta)
            {
                await EnsureOpenAsync();
                const string sql = @"
                    SELECT pm.procedimiento AS procedimiento, COALESCE(t.precio, 0) AS precio
                    FROM ProcedimientosMedicos pm
                    LEFT JOIN Tarifas t ON t.id_procedimiento = pm.id_procedimiento
                    WHERE pm.id_consulta = @id;";
                var rows = await _conn.QueryAsync<LineaFacturaItem>(sql, new { id = id_consulta });
                return rows.ToList();
            }
        // Reportes general
        public async Task<int> ContarPacientesAtendidosAsync(DateTime desde, DateTime hasta, int? id_medico, string? procedimiento)
        {
            await EnsureOpenAsync();
            const string sql = @"
                    SELECT COUNT(DISTINCT cm.id_paciente)
                    FROM ConsultasMedicas cm
                    INNER JOIN Medicos m ON m.id_medico = cm.id_medico
                    LEFT JOIN ProcedimientosMedicos pm ON pm.id_consulta = cm.id_consulta
                    WHERE cm.fecha >= @desde AND cm.fecha < DATE_ADD(@hasta, INTERVAL 1 DAY)
                      AND (@id_medico IS NULL OR cm.id_medico = @id_medico)";
                      //AND (@procedimiento IS NULL OR pm.procedimiento = @procedimiento);";
            return await _conn.ExecuteScalarAsync<int>(sql, new { desde, hasta, id_medico, procedimiento });
        }

      

        public async Task<int> ContarCitasProgramadasAsync(DateTime desde, DateTime hasta, int? id_medico, string? procedimiento)
        {
            await EnsureOpenAsync();
            const string sql = @"
                SELECT COUNT(*)
                FROM CitasMedicas c
                INNER JOIN Medicos m ON m.id_medico = c.id_medico
                WHERE c.fecha >= @desde AND c.fecha < DATE_ADD(@hasta, INTERVAL 1 DAY)
                  AND (@id_medico IS NULL OR c.id_medico = @id_medico)";
                  //AND (@procedimiento IS NULL OR pm.procedimiento = @procedimiento);";
            return await _conn.ExecuteScalarAsync<int>(sql, new { desde, hasta, id_medico, procedimiento });
        }

        // Ingresos totales aproximados: suma de tarifas por procedimientos realizados en el rango
        public async Task<decimal> ObtenerIngresosTotalesAproxAsync(DateTime desde, DateTime hasta, int? id_medico, string? procedimiento)
        {
            await EnsureOpenAsync();
            const string sql = @"
                SELECT COALESCE(SUM(t.precio), 0) AS total
                FROM ProcedimientosMedicos pm
                INNER JOIN ConsultasMedicas cm ON cm.id_consulta = pm.id_consulta
                INNER JOIN Medicos m ON m.id_medico = cm.id_medico
                INNER JOIN Tarifas t ON t.id_procedimiento = pm.id_procedimiento
                WHERE cm.fecha >= @desde AND cm.fecha < DATE_ADD(@hasta, INTERVAL 1 DAY)
                  AND (@id_medico IS NULL OR cm.id_medico = @id_medico)";
                  //AND (@procedimiento IS NULL OR pm.procedimiento = @procedimiento);";
            return await _conn.ExecuteScalarAsync<decimal>(sql, new { desde, hasta, id_medico, procedimiento });
        }

        public async Task<IEnumerable<IngresoServicioItem>> ObtenerIngresosPorServicioAsync(DateTime desde, DateTime hasta, int? id_medico, string? procedimiento)
        {
            await EnsureOpenAsync();
            const string sql = @"
                SELECT pm.procedimiento AS procedimiento,
                       COUNT(*) AS cantidad,
                       COALESCE(SUM(t.precio), 0) AS total
                FROM ProcedimientosMedicos pm
                INNER JOIN ConsultasMedicas cm ON cm.id_consulta = pm.id_consulta
                INNER JOIN Medicos m ON m.id_medico = cm.id_medico
                INNER JOIN Tarifas t ON t.id_procedimiento = pm.id_procedimiento
                WHERE cm.fecha >= @desde AND cm.fecha < DATE_ADD(@hasta, INTERVAL 1 DAY)
                  AND (@id_medico IS NULL OR cm.id_medico = @id_medico)
                GROUP BY pm.procedimiento
                ORDER BY total DESC;";
                //AND(@procedimiento IS NULL OR pm.procedimiento = @procedimiento)
            return await _conn.QueryAsync<IngresoServicioItem>(sql, new { desde, hasta, id_medico, procedimiento });
        }

        public async Task<IEnumerable<ProductividadItem>> ObtenerProductividadMedicaAsync(DateTime desde, DateTime hasta, int? id_medico, string? procedimiento)
        {
            await EnsureOpenAsync();
            const string sql = @"
                SELECT 
                    m.id_medico,
                    CONCAT(m.nombres, ' ', m.apellidos) AS medico,
                    m.especialidad,
                    COUNT(DISTINCT cm.id_consulta) AS consultas,
                    COALESCE(SUM(t.precio), 0) AS ingresos_aprox
                FROM Medicos m
                INNER JOIN ConsultasMedicas cm ON cm.id_medico = m.id_medico
                LEFT JOIN ProcedimientosMedicos pm ON pm.id_consulta = cm.id_consulta
                LEFT JOIN Tarifas t ON t.id_procedimiento = pm.id_procedimiento
                WHERE cm.fecha >= @desde AND cm.fecha < DATE_ADD(@hasta, INTERVAL 1 DAY)
                  AND (@id_medico IS NULL OR m.id_medico = @id_medico)
                GROUP BY m.id_medico, m.nombres, m.apellidos, m.especialidad
                ORDER BY ingresos_aprox DESC, consultas DESC;";
                //AND(@procedimiento IS NULL OR pm.procedimiento = @procedimiento)
            return await _conn.QueryAsync<ProductividadItem>(sql, new { desde, hasta, id_medico, procedimiento });
        }

        public async Task<int> ContarPacientesAtendidosEnDiaAsync(DateTime dia, int? id_medico, string? procedimiento)
        {
            await EnsureOpenAsync();
            const string sql = @"
                SELECT COUNT(DISTINCT cm.id_paciente)
                FROM ConsultasMedicas cm
                INNER JOIN Medicos m ON m.id_medico = cm.id_medico
                WHERE cm.fecha >= @ini AND cm.fecha < @fin
                  AND (@id_medico IS NULL OR cm.id_medico = @id_medico)";
                  //AND (@procedimiento IS NULL OR pm.procedimiento = @procedimiento);";
            var ini = dia.Date;
            var fin = ini.AddDays(1);
            return await _conn.ExecuteScalarAsync<int>(sql, new { ini, fin, id_medico, procedimiento });
        }

        //reportes dashboard
        public async Task<int> ContarCitasConfirmadasEnDiaAsync(DateTime dia, int? id_medico, string? especialidad)
        {
            await EnsureOpenAsync();
            const string sql = @"
                SELECT COUNT(*)
                FROM CitasMedicas c
                INNER JOIN Medicos m ON m.id_medico = c.id_medico
                WHERE c.estado = 'confirmada'
                  AND c.fecha >= @ini AND c.fecha < @fin
                  AND (@id_medico IS NULL OR c.id_medico = @id_medico)
                  AND (@especialidad IS NULL OR m.especialidad = @especialidad);";
            var ini = dia.Date;
            var fin = ini.AddDays(1);
            return await _conn.ExecuteScalarAsync<int>(sql, new { ini, fin, id_medico, especialidad });
        }

        // “Pendientes” de hoy = confirmadas con hora futura respecto a 'ahora'
        public async Task<int> ContarCitasConfirmadasFuturasHoyAsync(DateTime dia, DateTime ahora, int? id_medico, string? especialidad)
        {
            await EnsureOpenAsync();
            var ini = dia.Date;
            var fin = ini.AddDays(1);

            if (ahora.Date != dia.Date)
            {
                // Si la fecha consultada no es hoy:
                // - si es futura: todas confirmadas del día cuentan como “pendientes”
                // - si es pasada: 0 pendientes
                if (dia.Date > DateTime.Now.Date)
                    return await ContarCitasConfirmadasEnDiaAsync(dia, id_medico, especialidad);
                return 0;
            }

            const string sql = @"
                SELECT COUNT(*)
                FROM CitasMedicas c
                INNER JOIN Medicos m ON m.id_medico = c.id_medico
                WHERE c.estado = 'confirmada'
                  AND c.fecha >= @ahora AND c.fecha < @fin
                  AND (@id_medico IS NULL OR c.id_medico = @id_medico)
                  AND (@especialidad IS NULL OR m.especialidad = @especialidad);";
            return await _conn.ExecuteScalarAsync<int>(sql, new { ahora, fin, id_medico, especialidad });
        }

        // Ingresos acumulados del día = suma de Pagos realizados ese día
        public async Task<decimal> SumarIngresosPagosEnDiaAsync(DateTime dia, int? id_medico, string? especialidad)
        {
            await EnsureOpenAsync();
            // Si quieres atribuir por médico/especialidad, enlazamos Pago -> Factura -> Paciente -> (vía consulta)
            // Como no hay id_consulta en Facturacion, tomamos todos los pagos del día. 
            // Si deseas atribución por médico, podemos sumarlos por consultas del día y sus tarifas.
            const string sql = @"
                SELECT COALESCE(SUM(p.monto), 0)
                FROM Pagos p
                INNER JOIN Facturacion f ON f.id_factura = p.id_factura
                WHERE p.fecha_pago >= @ini AND p.fecha_pago < @fin;";
            var ini = dia.Date;
            var fin = ini.AddDays(1);
            return await _conn.ExecuteScalarAsync<decimal>(sql, new { ini, fin });
        }

        public async Task<IEnumerable<IngresoServicioItem>> TopProcedimientosEnDiaAsync(DateTime dia, int top, int? id_medico, string? especialidad)
        {
            await EnsureOpenAsync();
            const string sql = @"
                SELECT pm.procedimiento AS procedimiento,
                       COUNT(*) AS cantidad,
                       COALESCE(SUM(t.precio), 0) AS total
                FROM ProcedimientosMedicos pm
                INNER JOIN ConsultasMedicas cm ON cm.id_consulta = pm.id_consulta
                INNER JOIN Medicos m ON m.id_medico = cm.id_medico
                LEFT JOIN Tarifas t ON t.id_procedimiento = pm.id_procedimiento
                WHERE cm.fecha >= @ini AND cm.fecha < @fin
                  AND (@id_medico IS NULL OR m.id_medico = @id_medico)
                  AND (@especialidad IS NULL OR m.especialidad = @especialidad)
                GROUP BY pm.procedimiento
                ORDER BY cantidad DESC, total DESC
                LIMIT @top;";
            var ini = dia.Date;
            var fin = ini.AddDays(1);
            return await _conn.QueryAsync<IngresoServicioItem>(sql, new { ini, fin, top, id_medico, especialidad });
        }
    }
}
