using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Cuaderno.Core;
using Cuaderno.Core.Index;

namespace Cuaderno.Cli.Commands;

public static class MetaCommand
{
    private static readonly Regex FrontMatterRegex = new(
        pattern: "^---\\r?\\n(?<yaml>[\\s\\S]*?)\\r?\\n---\\r?\\n",
        RegexOptions.Compiled);

    public static void Run(string rootPath, string? rawArgs, bool autoReindex = true)
    {
        if (string.IsNullOrWhiteSpace(rawArgs))
        {
            PrintUsage();
            return;
        }

        // arg0: número; resto: pares clave:valor
        var firstSpace = rawArgs.IndexOf(' ');
        string idxStr, ops;
        if (firstSpace < 0)
        {
            PrintUsage();
            return;
        }
        else
        {
            idxStr = rawArgs[..firstSpace].Trim();
            ops = rawArgs[(firstSpace + 1)..].Trim();
        }

        if (!int.TryParse(idxStr, out int indexNum))
        {
            Console.WriteLine("[ERROR] Debes pasar el número de nota, ej: :meta 2 title:\"Nuevo título\"");
            return;
        }

        var indexPath = Path.Combine(rootPath, "_indice.json");
        if (!File.Exists(indexPath))
        {
            Console.WriteLine("[WARN] No existe _indice.json. Ejecuta :reindex primero.");
            return;
        }

        IndexFile? index;
        try
        {
            index = JsonSerializer.Deserialize<IndexFile>(File.ReadAllText(indexPath));
        }
        catch (Exception ex)
        {
            Console.WriteLine("[ERROR] No se pudo leer _indice.json: " + ex.Message);
            return;
        }
        if (index == null || index.Entradas.Count == 0)
        {
            Console.WriteLine("[INFO] No hay entradas en el índice.");
            return;
        }
        if (indexNum < 1 || indexNum > index.Entradas.Count)
        {
            Console.WriteLine($"[ERROR] Número fuera de rango. (1-{index.Entradas.Count})");
            return;
        }

        var entry = index.Entradas[indexNum - 1];
        var filePath = Path.Combine(rootPath, entry.Ruta);
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"[ERROR] El archivo no existe: {filePath}");
            return;
        }

        string content = File.ReadAllText(filePath, Encoding.UTF8);

        // Extraer YAML actual (si no hay, creamos uno nuevo)
        string yaml = "";
        string body = content;
        var m = FrontMatterRegex.Match(content);
        if (m.Success)
        {
            yaml = m.Groups["yaml"].Value;
            body = FrontMatterRegex.Replace(content, "");
        }

        // Parsear YAML light en diccionario (title, date, tags)
        var meta = ParseYamlLight(yaml);

        // Aplicar operaciones
        ApplyOps(meta, ops);

        // Reescribir archivo con nuevo YAML
        var newYaml = BuildYaml(meta);
        var sb = new StringBuilder();
        sb.AppendLine("---");
        sb.Append(newYaml);
        sb.AppendLine("---");
        sb.AppendLine();
        sb.Append(body.TrimStart('\r','\n')); // preserva el cuerpo

        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        Console.WriteLine($"[OK] Metadatos actualizados en: {entry.Ruta}");

        // Reindex automático opcional
        if (autoReindex)
        {
            try
            {
                var svc = new IndexService(rootPath);
                var idx = svc.Rebuild(includeSummary: true);
                svc.Save(idx);
                Console.WriteLine("[OK] Índice regenerado.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("[WARN] No se pudo reindexar automáticamente: " + ex.Message);
            }
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine("[USO] :meta <n> title:\"Nuevo título\"");
        Console.WriteLine("      :meta <n> date:YYYY-MM-DD");
        Console.WriteLine("      :meta <n> tags:Tag1,Tag2,Tag3   (reemplaza todos)");
        Console.WriteLine("      :meta <n> +tag:NombreTag        (agrega uno)");
        Console.WriteLine("      :meta <n> -tag:NombreTag        (quita uno)");
    }

    // Estructura de metadatos
    private sealed class Meta
    {
        public string? Title { get; set; }
        public string? Date  { get; set; }
        public List<string> Tags { get; set; } = new();
    }

    private static Meta ParseYamlLight(string? yaml)
    {
        var meta = new Meta();
        if (string.IsNullOrWhiteSpace(yaml)) return meta;

        foreach (var raw in yaml.Split('\n'))
        {
            var line = raw.Trim();
            if (line.StartsWith("title:"))
            {
                meta.Title = line["title:".Length..].Trim().Trim('"');
            }
            else if (line.StartsWith("date:"))
            {
                meta.Date = line["date:".Length..].Trim();
            }
            else if (line.StartsWith("tags:"))
            {
                var v = line["tags:".Length..].Trim();
                if (v.StartsWith("[") && v.EndsWith("]"))
                {
                    v = v.Trim('[', ']');
                    meta.Tags = v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                 .Select(x => x.Trim().Trim('"'))
                                 .Where(x => x.Length > 0).ToList();
                }
                else
                {
                    meta.Tags = v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                 .Select(x => x.Trim()).ToList();
                }
            }
        }
        return meta;
    }

    private static void ApplyOps(Meta meta, string ops)
    {
        // tokens separados por espacios, pero soporta title:"con espacios"
        foreach (var token in Tokenize(ops))
        {
            if (token.StartsWith("title:", StringComparison.OrdinalIgnoreCase))
            {
                meta.Title = token[6..].Trim().Trim('"');
            }
            else if (token.StartsWith("date:", StringComparison.OrdinalIgnoreCase))
            {
                meta.Date = token[5..].Trim();
            }
            else if (token.StartsWith("tags:", StringComparison.OrdinalIgnoreCase))
            {
                var list = token[5..].Split(',', StringSplitOptions.RemoveEmptyEntries)
                                     .Select(x => x.Trim()).Where(x => x.Length > 0).ToList();
                meta.Tags = list;
            }
            else if (token.StartsWith("+tag:", StringComparison.OrdinalIgnoreCase))
            {
                var t = token[5..].Trim();
                if (!string.IsNullOrEmpty(t) &&
                    !meta.Tags.Any(x => x.Equals(t, StringComparison.OrdinalIgnoreCase)))
                    meta.Tags.Add(t);
            }
            else if (token.StartsWith("-tag:", StringComparison.OrdinalIgnoreCase))
            {
                var t = token[5..].Trim();
                meta.Tags = meta.Tags.Where(x => !x.Equals(t, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            else
            {
                // Ignorar tokens desconocidos para mantener robustez
            }
        }
    }

    private static string BuildYaml(Meta meta)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"title: {meta.Title ?? ""}".TrimEnd());
        sb.AppendLine($"date: {meta.Date ?? DateTime.Now.ToString("yyyy-MM-dd")}");
        if (meta.Tags.Count == 0)
            sb.AppendLine("tags: []");
        else
            sb.AppendLine("tags: [" + string.Join(", ", meta.Tags) + "]");
        return sb.ToString();
    }

    private static List<string> Tokenize(string s)
    {
        // "frase con espacios" o tokens sin comillas
        var rx = new Regex("\"([^\"]+)\"|(\\S+)", RegexOptions.Compiled);
        var list = new List<string>();
        foreach (Match m in rx.Matches(s))
        {
            if (m.Groups[1].Success) list.Add("title:" + m.Groups[1].Value); // para title:"..."
            else if (m.Groups[2].Success) list.Add(m.Groups[2].Value);
        }

        // Si no venía title:"...", igual necesitamos conservar tokens tal cual
        // Ajuste: detectar explícitamente title: en tokens sin comillas
        // (si venía title:Resumen, se respeta)
        return list;
    }
}
