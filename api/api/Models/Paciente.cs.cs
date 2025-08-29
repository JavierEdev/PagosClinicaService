namespace FacturacionAPI.Models
{
    public class Paciente
    {
        public int id_paciente { get; set; }
        public string nombres { get; set; } = string.Empty;
        public string apellidos { get; set; } = string.Empty;
        public string dpi { get; set; } = string.Empty;
        public DateTime fecha_nacimiento { get; set; }
        public string sexo { get; set; } = string.Empty;   // 'M' | 'F'
        public string? direccion { get; set; }
        public string? telefono { get; set; }
        public string? correo { get; set; }
        public string? estado_civil { get; set; }
    }
}
