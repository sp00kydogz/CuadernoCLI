// =============================================
// File: src/Cuaderno.CLI/Program.cs
// Target: .NET 8 Console (top-level statements)
// Desc: Punto de entrada. Prepara carpeta de datos del Cuaderno, imprime ayuda y maneja el loop de comandos.
// =============================================
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using Cuaderno.Core;
using Cuaderno.Cli.Commands;

try
{
    // ✅ Resolver ruta de datos flexible (env var -> config junto al exe -> Mis Documentos\Cuaderno)
    var cuadernoPath = ResolveDataDir();

    // ✅ Asegurar estructura básica del Cuaderno
    Bootstrap.EnsureCuadernoReady(cuadernoPath);

    Console.WriteLine($"📓 Cuaderno listo en: {cuadernoPath}");
    Console.WriteLine("- _config.json presente");
    Console.WriteLine("- Repositorio Git inicializado");
    Console.WriteLine();

    // 🧾 Ayuda con iconitos
    Console.WriteLine("Comandos disponibles:");
    Console.WriteLine("  🆕 :new <ruta> \"Título\"       → Crear nueva nota");
    Console.WriteLine("  📑 :ls [filtro]               → Listar notas (cat, sub, tags:<t>, YYYY[-MM])");
    Console.WriteLine("  🔎 :buscar <términos> [...]    → Buscar (tag:, cat:, sub:, date:YYYY[-MM])");
    Console.WriteLine("  📂 :open <n> / :o <n>         → Abrir nota en editor externo");
    Console.WriteLine("  👀 :view <n>                  → Ver nota en consola");
    Console.WriteLine("  🖊️ :append <n>                → Agregar texto al final de la nota");
    Console.WriteLine("  🏷️ :meta <n> <op>             → Editar metadatos (title, date, tags, +tag, -tag)");
    Console.WriteLine("  🔄 :reindex                   → Regenerar índice");
    Console.WriteLine("  🚪 :salir / :q                → Salir del programa");
    Console.WriteLine();

    // 🔁 Loop interactivo del CLI
    while (true)
    {
        Console.Write("> ");
        var input = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input))
            continue;

        input = input.Trim();

        // Salir
        if (input.Equals(":salir", StringComparison.OrdinalIgnoreCase) ||
            input.Equals(":q", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("👋 Hasta pronto!");
            break;
        }

        // Reindex
        else if (input.Equals(":reindex", StringComparison.OrdinalIgnoreCase))
        {
            var exitCode = ReindexCommand.Run(cuadernoPath);
            if (exitCode == 0)
                Console.WriteLine("[OK] Índice regenerado.");
        }

        // Nueva nota
        else if (input.StartsWith(":new", StringComparison.OrdinalIgnoreCase))
        {
            var argLineNew = input.Length > 4 ? input[4..].Trim() : "";
            NewCommand.Run(cuadernoPath, argLineNew);
        }

        // Listar (con filtro opcional)
        else if (input.StartsWith(":ls", StringComparison.OrdinalIgnoreCase))
        {
            var argLineLs = input.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            var filter = argLineLs.Length > 1 ? argLineLs[1] : null;
            ListCommand.Run(cuadernoPath, filter);
        }

        // Buscar
        else if (input.StartsWith(":buscar", StringComparison.OrdinalIgnoreCase))
        {
            var argLineSearch = input.Length > 8 ? input[8..].Trim() : "";
            SearchCommand.Run(cuadernoPath, argLineSearch);
        }

        // Ver
        else if (input.StartsWith(":view", StringComparison.OrdinalIgnoreCase))
        {
            var argLineView = input.Length > 5 ? input[5..].Trim() : "";
            ViewCommand.Run(cuadernoPath, argLineView);
        }

        // Append (modo escribir dentro del CLI)
        else if (input.StartsWith(":append", StringComparison.OrdinalIgnoreCase))
        {
            var argLineAppend = input.Length > 7 ? input[7..].Trim() : "";
            AppendCommand.Run(cuadernoPath, argLineAppend);
        }

        // Abrir en editor externo (alias :o y :open)
        else if (input.StartsWith(":o", StringComparison.OrdinalIgnoreCase) ||
                 input.StartsWith(":open", StringComparison.OrdinalIgnoreCase))
        {
            // Si es :open corta desde 5, si es :o corta desde 2
            var argLineOpen = input.StartsWith(":open", StringComparison.OrdinalIgnoreCase)
                ? (input.Length > 5 ? input[5..].Trim() : "")
                : (input.Length > 2 ? input[2..].Trim() : "");

            OpenCommand.Run(cuadernoPath, argLineOpen);
        }

        // Meta (editar front-matter)
        else if (input.StartsWith(":meta", StringComparison.OrdinalIgnoreCase))
        {
            var argLineMeta = input.Length > 5 ? input[5..].Trim() : "";
            MetaCommand.Run(cuadernoPath, argLineMeta, autoReindex: true);
        }

        // Comando desconocido
        else
        {
            Console.WriteLine($"[WARN] Comando desconocido: {input}");
        }
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[ERROR] en Bootstrap/Inicio: {ex.Message}");
}

// Mantener la consola visible cuando se ejecuta con doble click
if (Environment.GetCommandLineArgs().Length == 1)
{
    Console.WriteLine();
    Console.Write("Presiona ENTER para salir...");
    Console.ReadLine();
}

// =====================
// Funciones auxiliares
// =====================
static string ResolveDataDir()
{
    // 1) Variable de entorno (mayor prioridad)
    var env = Environment.GetEnvironmentVariable("CUADERNO_DIR");
    if (!string.IsNullOrWhiteSpace(env))
        return Environment.ExpandEnvironmentVariables(env);

    // 2) Config JSON junto al ejecutable
    var exeDir = AppContext.BaseDirectory;
    var cfgPath = Path.Combine(exeDir, "cuaderno.config.json");
    if (File.Exists(cfgPath))
    {
        try
        {
            var json = File.ReadAllText(cfgPath);
            var root = JsonDocument.Parse(json).RootElement;
            if (root.TryGetProperty("dataDir", out var dd))
            {
                var val = dd.GetString();
                if (!string.IsNullOrWhiteSpace(val))
                    return Environment.ExpandEnvironmentVariables(val);
            }
        }
        catch
        {
            // Ignorar errores de parseo y seguir con default
        }
    }

    // 3) Default: Mis Documentos\Cuaderno
    var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    return Path.Combine(docs, "Cuaderno");
}
