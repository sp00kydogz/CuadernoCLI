using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Cuaderno.Core.Index;
// using Cuaderno.Core.Notes; // si necesitas tu modelo/loader de YAML

namespace Cuaderno.Core;

public sealed class IndexService
{
    private static readonly string[] NoteExtensions = [".md", ".txt"];
    private static readonly Regex FrontMatterRegex = new(
        pattern: "^---\\r?\\n(?<yaml>[\\s\\S]*?)\\r?\\n---\\r?\\n",
        RegexOptions.Compiled);

    private readonly string _rootPath;
    private readonly JsonSerializerOptions _jsonOpts = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public IndexService(string rootPath)
    {
        _rootPath = rootPath;
    }

    public IndexFile Rebuild(bool includeSummary = true)
    {
        var indice = new IndexFile
        {
            GeneradoEn = DateTimeOffset.UtcNow,
            Raiz = Path.GetFileName(_rootPath)
        };

        foreach (var file in EnumerateNotes(_rootPath))
        {
            var rel = Path.GetRelativePath(_rootPath, file);
            var content = File.ReadAllText(file, Encoding.UTF8);

            // Extraer front matter
            string? yaml = null;
            var m = FrontMatterRegex.Match(content);
            if (m.Success) yaml = m.Groups["yaml"].Value;

            var meta = ParseYamlLight(yaml); // título, fecha, tags

            // Derivar categoría/subcategoría desde la ruta
            var parts = rel.Replace('\\','/').Split('/');
            var categoria = parts.Length > 1 ? parts[0] : "";
            var subcat = parts.Length > 2 ? parts[1] : null;

            var entry = new IndexEntry
            {
                Id       = rel.ToLowerInvariant().Replace('\\','/'),
                Ruta     = rel.Replace('\\','/'),
                Titulo   = meta.Title ?? DeriveTitleFromFile(rel),
                Categoria = categoria,
                Subcategoria = subcat,
                Fecha    = meta.Date,
                Tags     = meta.Tags ?? new List<string>(),
                Modificado = File.GetLastWriteTimeUtc(file),
                Hash     = "sha256:" + ComputeSha256(content),
                Resumen  = includeSummary ? DeriveSummary(content, yaml != null) : null
            };

            indice.Entradas.Add(entry);
        }

        // Orden sugerido: más reciente primero
        indice.Entradas = indice.Entradas
            .OrderByDescending(e => e.Modificado)
            .ToList();

        return indice;
    }

    public void Save(IndexFile index, string? customPath = null)
    {
        var outPath = customPath ?? Path.Combine(_rootPath, "_indice.json");
        var json = JsonSerializer.Serialize(index, _jsonOpts);
        File.WriteAllText(outPath, json, Encoding.UTF8);
    }

    private static IEnumerable<string> EnumerateNotes(string root)
    {
        return Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
            .Where(p =>
            {
                var name = Path.GetFileName(p);
                if (name.StartsWith("_")) return false; // ignora archivos sistema
                var ext = Path.GetExtension(p).ToLowerInvariant();
                if (!NoteExtensions.Contains(ext)) return false;
                // ignora carpetas ocultas o .git
                if (p.Split(Path.DirectorySeparatorChar).Any(seg => seg.StartsWith(".") || seg.Equals(".git"))) return false;
                return true;
            });
    }

    private static string ComputeSha256(string content)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string DeriveTitleFromFile(string relPath)
    {
        var file = Path.GetFileNameWithoutExtension(relPath);
        // quita prefijos de fecha "YYYY-MM-DD-"
        var t = Regex.Replace(file, "^[0-9]{4}-[0-9]{2}-[0-9]{2}[-_ ]", "");
        // reemplaza separadores por espacios
        t = t.Replace('-', ' ').Replace('_', ' ');
        // capitaliza simple
        return char.ToUpper(t[0]) + t[1..];
    }

    private static string? DeriveSummary(string content, bool hadYaml)
    {
        var body = hadYaml ? FrontMatterRegex.Replace(content, "") : content;
        var text = StripMarkdown(body).Trim();
        if (string.IsNullOrWhiteSpace(text)) return null;
        var words = text.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
        var n = Math.Min(12, words.Length);
        return string.Join(' ', words.Take(n));
    }

    private static string StripMarkdown(string s)
    {
        // súper light: quita encabezados, énfasis y enlaces
        s = Regex.Replace(s, "^#{1,6}\\s*", "", RegexOptions.Multiline);
        s = Regex.Replace(s, @"(\*|_){1,3}(.+?)\1", "$2");
        s = Regex.Replace(s, @"!\[[^\]]*\]\([^)]+\)", "");         // imágenes
        s = Regex.Replace(s, @"\[[^\]]+\]\(([^)]+)\)", "$1");      // enlaces
        return s;
    }

    // Parser mínimo para front matter que esperas:
    // title: string
    // date:  yyyy-MM-dd
    // tags:  [a, b, c] o "a, b, c"
    private static (string? Title, string? Date, List<string>? Tags) ParseYamlLight(string? yaml)
    {
        if (string.IsNullOrWhiteSpace(yaml)) return (null, null, null);

        string? title = null;
        string? date  = null;
        List<string>? tags = null;

        foreach (var raw in yaml.Split('\n'))
        {
            var line = raw.Trim();
            if (line.StartsWith("title:"))
                title = line["title:".Length..].Trim().Trim('"');
            else if (line.StartsWith("date:"))
                date = line["date:".Length..].Trim();
            else if (line.StartsWith("tags:"))
            {
                var v = line["tags:".Length..].Trim();
                if (v.StartsWith("[") && v.EndsWith("]"))
                {
                    v = v.Trim('[', ']');
                    tags = v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(x => x.Trim().Trim('"')).ToList();
                }
                else
                {
                    tags = v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(x => x.Trim()).ToList();
                }
            }
        }
        return (title, date, tags);
    }
}