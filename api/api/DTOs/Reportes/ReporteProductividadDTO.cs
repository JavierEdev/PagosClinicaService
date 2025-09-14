namespace api.DTOs.Reportes
{
    public class ReporteProductividadDTO
    {
        public int IdMedico { get; set; }
        public string NombreMedico { get; set; } = "";
        public string? Especialidad { get; set; }

        public int TotalCitas { get; set; }
        public int CitasAtendidas { get; set; }
        public int CitasCanceladas { get; set; }
        public int CitasNoAsistidas { get; set; }

        public int PacientesAtendidos { get; set; }
        public int ProcedimientosRealizados { get; set; }
        public decimal IngresosGenerados { get; set; }
        public double? PromedioSatisfaccion { get; set; }

        // Métrica derivada (citas atendidas / día del rango)
        public double ProductividadCitasDia { get; set; }
    }
}
