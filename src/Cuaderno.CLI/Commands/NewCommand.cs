using System;
using System.IO;
using System.Text;

namespace Cuaderno.Cli.Commands;

public static class NewCommand
{
    public static void Run(string rootPath, string? args)
    {
        if (string.IsNullOrWhiteSpace(args))
        {
            Console.WriteLine("[USO] :new <ruta/categoria/...> \"Título de la nota\"");
            return;
        }

        // Dividir argumentos → ruta + título entre comillas
        var parts = args.Split('"');
        if (parts.Length < 2)
        {
            Console.WriteLine("[ERROR] Debes encerrar el título entre comillas.");
            return;
        }

        var pathPart = parts[0].Trim();
        var title = parts[1].Trim();
        if (string.IsNullOrWhiteSpace(pathPart) || string.IsNullOrWhiteSpace(title))
        {
            Console.WriteLine("[ERROR] Ruta y título son obligatorios.");
            return;
        }

        // Armar ruta destino
        var today = DateTime.Now.ToString("yyyy-MM-dd");
        var safeTitle = title.Replace(' ', '-');
        var fileName = $"{today}-{safeTitle}.md";
        var dirPath = Path.Combine(rootPath, pathPart);
        var filePath = Path.Combine(dirPath, fileName);

        Directory.CreateDirectory(dirPath);

        if (File.Exists(filePath))
        {
            Console.WriteLine($"[WARN] Ya existe: {filePath}");
            return;
        }

        // Front-matter básico
        var sb = new StringBuilder();
        sb.AppendLine("---");
        sb.AppendLine($"title: {title}");
        sb.AppendLine($"date: {today}");
        sb.AppendLine("tags: []");
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine($"# {title}");
        sb.AppendLine();

        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);

        Console.WriteLine($"[OK] Nota creada: {filePath}");
    }
}
