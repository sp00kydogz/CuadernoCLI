using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Cuaderno.Core.Index;

namespace Cuaderno.Cli.Commands;

public static class OpenCommand
{
    public static void Run(string rootPath, string? arg)
    {
        if (string.IsNullOrWhiteSpace(arg))
        {
            Console.WriteLine("[USO] :o <número de nota>");
            return;
        }

        if (!int.TryParse(arg, out int indexNum))
        {
            Console.WriteLine("[ERROR] Debes pasar un número válido, ej: :o 1");
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

        try
        {
            Console.WriteLine($"[OK] Abriendo: {filePath}");

            // En Windows abre con Notepad por defecto
            var psi = new ProcessStartInfo
            {
                FileName = "notepad.exe",
                Arguments = $"\"{filePath}\"",
                UseShellExecute = true
            };
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] No se pudo abrir la nota: {ex.Message}");
        }
    }
}
