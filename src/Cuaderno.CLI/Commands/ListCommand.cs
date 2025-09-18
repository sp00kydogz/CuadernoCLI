using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Cuaderno.Core.Index;

namespace Cuaderno.Cli.Commands;

public static class ListCommand
{
    public static void Run(string rootPath, string? filter = null)
    {
        var indexPath = Path.Combine(rootPath, "_indice.json");
        if (!File.Exists(indexPath))
        {
            Console.WriteLine("[WARN] No existe _indice.json. Ejecuta :reindex primero.");
            return;
        }

        try
        {
            var json = File.ReadAllText(indexPath);
            var index = JsonSerializer.Deserialize<IndexFile>(json);

            if (index == null || index.Entradas.Count == 0)
            {
                Console.WriteLine("[INFO] No hay entradas en el índice.");
                return;
            }

            var entradas = index.Entradas.AsEnumerable();

            // 🔍 Aplicar filtros
            if (!string.IsNullOrWhiteSpace(filter))
            {
                if (filter.StartsWith("tags:", StringComparison.OrdinalIgnoreCase))
                {
                    var tag = filter["tags:".Length..].Trim();
                    entradas = entradas.Where(e => e.Tags.Any(t =>
                        t.Equals(tag, StringComparison.OrdinalIgnoreCase)));
                }
                else if (filter.Length == 7 && filter.Count(c => c == '-') == 1) // ej: "2025-09"
                {
                    entradas = entradas.Where(e => e.Fecha != null && e.Fecha.StartsWith(filter));
                }
                else
                {
                    // Por categoría o subcategoría
                    entradas = entradas.Where(e =>
                        e.Categoria.Equals(filter, StringComparison.OrdinalIgnoreCase) ||
                        (e.Subcategoria != null && e.Subcategoria.Equals(filter, StringComparison.OrdinalIgnoreCase)));
                }
            }

            var lista = entradas.ToList();

            Console.WriteLine($"📑 Listando {lista.Count} notas{(filter != null ? $" filtradas por '{filter}'" : "")}:\n");

            int i = 1;
            foreach (var e in lista)
            {
                Console.WriteLine(
                    $"[{i}] {e.Fecha ?? "????-??-??"}  {e.Titulo,-20} ({e.Categoria}/{e.Subcategoria})   tags: {string.Join(",", e.Tags)}"
                );
                i++;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("[ERROR] No se pudo leer _indice.json:");
            Console.Error.WriteLine(ex.Message);
        }
    }
}