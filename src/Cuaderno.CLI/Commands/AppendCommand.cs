using System;
using System.IO;
using System.Text;
using System.Text.Json;
using Cuaderno.Core.Index;

namespace Cuaderno.Cli.Commands;

public static class AppendCommand
{
    public static void Run(string rootPath, string? arg)
    {
        if (string.IsNullOrWhiteSpace(arg))
        {
            Console.WriteLine("[USO] :append <número de nota>");
            return;
        }

        if (!int.TryParse(arg, out int indexNum))
        {
            Console.WriteLine("[ERROR] Debes pasar un número válido, ej: :append 2");
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

        Console.WriteLine($"[MODO APPEND] Agregando a: {entry.Ruta}");
        Console.WriteLine("Escribe tu texto. Línea sola con '.' para guardar y salir. Usa ':cancel' para abortar.\n");

        var sb = new StringBuilder();
        // Asegura salto de línea antes de lo nuevo
        sb.AppendLine();

        while (true)
        {
            var line = Console.ReadLine();
            if (line is null) continue;

            if (line == ".")
            {
                try
                {
                    File.AppendAllText(filePath, sb.ToString(), Encoding.UTF8);
                    Console.WriteLine("[OK] Guardado.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] No se pudo guardar: {ex.Message}");
                }
                break;
            }
            if (line.Equals(":cancel", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("[CANCELADO] No se guardaron cambios.");
                break;
            }

            sb.AppendLine(line);
        }
    }
}
