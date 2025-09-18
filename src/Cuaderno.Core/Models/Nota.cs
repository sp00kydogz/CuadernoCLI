// =============================================
// File: src/Cuaderno.Core/Models/Nota.cs
// Desc: Modelo de nota + front-matter
// =============================================

namespace Cuaderno.Core.Models;

public class NotaFrontMatter
{
    public string uuid { get; set; } = Guid.NewGuid().ToString();
    public string materia { get; set; } = string.Empty;
    public string fecha { get; set; } = DateOnly.FromDateTime(DateTime.Now).ToString("yyyy-MM-dd");
    public string titulo { get; set; } = string.Empty;
    public List<string> tags { get; set; } = new();
    public List<string> adjuntos { get; set; } = new();
}

public class Nota
{
    public NotaFrontMatter Meta { get; set; } = new();
    public string Cuerpo { get; set; } = string.Empty;
}