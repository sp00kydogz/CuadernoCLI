using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Cuaderno.Core.Index;

namespace Cuaderno.Cli.Commands;

public static class SearchCommand
{
    private sealed record Query(
        List<string> Terms,
        string? Tag,
        string? Cat,
        string? Subcat,
        string? DatePrefix
    );

    public static void Run(string rootPath, string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            PrintUsage();
            return;
        }

        var indexPath = Path.Combine(rootPath, "_indice.json");
        if (!File.Exists(indexPath))
        {
            Console.WriteLine("[WARN] No existe _indice.json. Ejecuta :reindex primero.");
            return;
        }

        var json = File.ReadAllText(indexPath);
        var index = JsonSerializer.Deserialize<IndexFile>(json);
        if (index == null || index.Entradas.Count == 0)
        {
            Console.WriteLine("[INFO] No hay entradas en el índice.");
            return;
        }

        var q = Parse(raw);

        // Filtrado base por tag/cat/subcat/fecha
        IEnumerable<IndexEntry> filtered = index.Entradas;

        if (!string.IsNullOrWhiteSpace(q.Tag))
            filtered = filtered.Where(e => e.Tags.Any(t => t.Equals(q.Tag, StringComparison.OrdinalIgnoreCase)));

        if (!string.IsNullOrWhiteSpace(q.Cat))
            filtered = filtered.Where(e => e.Categoria.Equals(q.Cat, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(q.Subcat))
            filtered = filtered.Where(e => (e.Subcategoria ?? "").Equals(q.Subcat, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(q.DatePrefix))
            filtered = filtered.Where(e => (e.Fecha ?? "").StartsWith(q.DatePrefix, StringComparison.OrdinalIgnoreCase));

        // Coincidencia por términos (AND por defecto)
        if (q.Terms.Count > 0)
        {
            filtered = filtered.Where(e => TermsMatchAll(e, q.Terms));
        }

        // Ranking simple: título pesa más, luego tags, luego ruta/categoría/resumen
        var ranked = filtered
            .Select(e => new
            {
                Entry = e,
                Score = Score(e, q.Terms, q.Tag)
            })
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Entry.Modificado)
            .ToList();

        Console.WriteLine($"🔎 Resultados: {ranked.Count} {(ranked.Count == 1 ? "nota" : "notas")}");

        if (ranked.Count == 0) return;

        int i = 1;
        foreach (var r in ranked)
        {
            var e = r.Entry;
            Console.WriteLine(
                $"[{i}] {e.Fecha ?? "????-??-??"}  {e.Titulo,-24} ({e.Categoria}/{e.Subcategoria})  tags: {string.Join(",", e.Tags)}"
            );
            i++;
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine("[USO] :buscar <terminos> [tag:<tag>] [cat:<categoria>] [sub:<subcategoria>] [date:YYYY[-MM]]");
        Console.WriteLine("Ejemplos:");
        Console.WriteLine("  :buscar vlan");
        Console.WriteLine("  :buscar \"resumen cisco\"");
        Console.WriteLine("  :buscar vlan tag:Cisco");
        Console.WriteLine("  :buscar redes cat:Estudios");
        Console.WriteLine("  :buscar routing date:2025-09");
    }

    private static Query Parse(string raw)
    {
        // Extrae comillas dobles como una sola unidad
        var tokens = Tokenize(raw);

        string? tag = null, cat = null, sub = null, date = null;
        var terms = new List<string>();

        foreach (var t in tokens)
        {
            if (t.StartsWith("tag:", StringComparison.OrdinalIgnoreCase))
                tag = t[4..];
            else if (t.StartsWith("cat:", StringComparison.OrdinalIgnoreCase))
                cat = t[4..];
            else if (t.StartsWith("sub:", StringComparison.OrdinalIgnoreCase))
                sub = t[4..];
            else if (t.StartsWith("date:", StringComparison.OrdinalIgnoreCase))
                date = t[5..];
            else
                terms.Add(t);
        }

        return new Query(terms, tag, cat, sub, date);
    }

    private static List<string> Tokenize(string s)
    {
        // Soporta frases entre comillas y palabras sueltas
        var rx = new Regex("\"([^\"]+)\"|(\\S+)", RegexOptions.Compiled);
        var list = new List<string>();
        foreach (Match m in rx.Matches(s))
        {
            if (m.Groups[1].Success) list.Add(m.Groups[1].Value);
            else if (m.Groups[2].Success) list.Add(m.Groups[2].Value);
        }
        return list;
    }

    private static bool TermsMatchAll(IndexEntry e, List<string> terms)
    {
        foreach (var term in terms)
        {
            if (!Matches(e, term)) return false;
        }
        return true;
    }

    private static bool Matches(IndexEntry e, string term)
    {
        bool In(string? x) => !string.IsNullOrEmpty(x) &&
                              x.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;

        if (In(e.Titulo)) return true;
        if (In(e.Ruta)) return true;
        if (In(e.Categoria)) return true;
        if (In(e.Subcategoria)) return true;
        if (In(e.Fecha)) return true;
        if (In(e.Resumen)) return true;
        if (e.Tags.Any(t => t.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)) return true;

        return false;
    }

    private static int Score(IndexEntry e, List<string> terms, string? explicitTag)
    {
        int score = 0;

        foreach (var term in terms)
        {
            if (!string.IsNullOrEmpty(e.Titulo) &&
                e.Titulo.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0) score += 30;

            if (e.Tags.Any(t => t.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)) score += 20;

            if (!string.IsNullOrEmpty(e.Resumen) &&
                e.Resumen.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0) score += 10;

            if (!string.IsNullOrEmpty(e.Ruta) &&
                e.Ruta.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0) score += 6;

            if (!string.IsNullOrEmpty(e.Categoria) &&
                e.Categoria.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0) score += 4;

            if (!string.IsNullOrEmpty(e.Subcategoria) &&
                e.Subcategoria!.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0) score += 4;
        }

        if (!string.IsNullOrEmpty(explicitTag) &&
            e.Tags.Any(t => t.Equals(explicitTag, StringComparison.OrdinalIgnoreCase)))
            score += 15;

        // Un plus leve por recencia (última modificación)
        // (mientras más reciente, más suma, tope 10)
        var days = Math.Clamp((int)(DateTimeOffset.UtcNow - e.Modificado).TotalDays, 0, 365);
        score += (10 - Math.Min(10, days / 36)); // 0..10 aprox

        return score;
    }
}
