using System;
using System.Collections.Generic;

namespace Cuaderno.Core.Index;

public sealed class IndexFile
{
    public int Version { get; set; } = 1;
    public DateTimeOffset GeneradoEn { get; set; } = DateTimeOffset.UtcNow;
    public string Raiz { get; set; } = "Cuaderno";
    public List<IndexEntry> Entradas { get; set; } = new();
}

public sealed class IndexEntry
{
    public string Id { get; set; } = "";
    public string Ruta { get; set; } = "";
    public string Titulo { get; set; } = "";
    public string Categoria { get; set; } = "";
    public string? Subcategoria { get; set; }
    public string? Fecha { get; set; }
    public List<string> Tags { get; set; } = new();
    public DateTimeOffset Modificado { get; set; }
    public string Hash { get; set; } = "";
    public string? Resumen { get; set; }
}