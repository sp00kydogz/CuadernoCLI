// =============================================
// File: src/Cuaderno.CLI/Program.cs
// Target: .NET 8 Console
// Desc: Punto de entrada. Prepara carpeta "Cuaderno/", _config.json y Git.
// =============================================
using System.Text;
using Cuaderno.Core;
using Cuaderno.Cli.Commands;

try
{
    var cuadernoPath = Path.Combine(Environment.CurrentDirectory, "Cuaderno");

    // Asegura que existe la carpeta Cuaderno y _config.json
    Bootstrap.EnsureCuadernoReady(cuadernoPath);

    Console.WriteLine($"📓 Cuaderno listo en: {cuadernoPath}");
    Console.WriteLine("- _config.json presente");
    Console.WriteLine("- Repositorio Git inicializado");
    Console.WriteLine();
    Console.WriteLine("Comandos disponibles: ");
    Console.WriteLine("  🆕 :new <ruta> \"Título\"       → Crear nueva nota");
    Console.WriteLine("  📑 :ls [filtro]               → Listar notas");
    Console.WriteLine("  🔎 :buscar <terminos>         → Buscar notas");
    Console.WriteLine("  📂 :open <n> / :o <n>         → Abrir nota en editor externo");
    Console.WriteLine("  👀 :view <n>                  → Ver nota en consola");
    Console.WriteLine("  🖊️ :append <n>                → Agregar texto a nota");
    Console.WriteLine("  🏷️ :meta <n> <op>             → Editar metadatos (title, date, tags)");
    Console.WriteLine("  🔄 :reindex                   → Regenerar índice");
    Console.WriteLine("  🚪 :salir / :q                → Salir del programa");
    Console.WriteLine();

    // Loop interactivo del CLI
    while (true)
    {
        Console.Write("> ");
        var input = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input))
            continue;

        input = input.Trim();

        // Comando salir
        if (input.Equals(":salir", StringComparison.OrdinalIgnoreCase) ||
            input.Equals(":q", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("👋 Hasta pronto!");
            break;
        }

        // Comando reindex
        else if (input.Equals(":reindex", StringComparison.OrdinalIgnoreCase))
        {
            var exitCode = ReindexCommand.Run(cuadernoPath);
            if (exitCode == 0)
                Console.WriteLine("[OK] Índice regenerado.");
        }

        // Comando nueva nota (placeholder)
        else if (input.StartsWith(":new", StringComparison.OrdinalIgnoreCase))
        {
            var argLine = input[4..].Trim();
            NewCommand.Run(cuadernoPath, argLine);
        }

        // Comando listar notas (placeholder)
        else if (input.StartsWith(":ls", StringComparison.OrdinalIgnoreCase))
        {
            var parts = input.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            var filter = parts.Length > 1 ? parts[1] : null;
            ListCommand.Run(cuadernoPath, filter);
        }
        
        // Comando abrir nota (placeholder)
        else if (input.StartsWith(":o", StringComparison.OrdinalIgnoreCase) ||
                 input.StartsWith(":open", StringComparison.OrdinalIgnoreCase))
        {
            // Si empieza con ":open", corta a partir de 5, si no, de 2
            var argLine = input.StartsWith(":open", StringComparison.OrdinalIgnoreCase)
                ? (input.Length > 5 ? input[5..].Trim() : "")
                : (input.Length > 2 ? input[2..].Trim() : "");

            OpenCommand.Run(cuadernoPath, argLine);
        }

        // Comando buscar notas (placeholder)
        else if (input.StartsWith(":buscar", StringComparison.OrdinalIgnoreCase))
        {
            var argLine = input.Length > 8 ? input[8..].Trim() : "";
            SearchCommand.Run(cuadernoPath, argLine);
        }

        // Comando ver nota (placeholder)
        else if (input.StartsWith(":view", StringComparison.OrdinalIgnoreCase))
        {
            var argLine = input.Length > 5 ? input[5..].Trim() : "";
            ViewCommand.Run(cuadernoPath, argLine);
        }

        // Comando append (placeholder)
        else if (input.StartsWith(":append", StringComparison.OrdinalIgnoreCase))
        {
            var argLine = input.Length > 7 ? input[7..].Trim() : "";
            AppendCommand.Run(cuadernoPath, argLine);
        }

        // Comando meta (placeholder)
        else if (input.StartsWith(":meta", StringComparison.OrdinalIgnoreCase))
        {
            var argLine = input.Length > 5 ? input[5..].Trim() : "";
            MetaCommand.Run(cuadernoPath, argLine, autoReindex: true);
        }

        // Si no coincide con nada
        else
        {
            Console.WriteLine($"[WARN] Comando desconocido: {input}");
        }
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[ERROR] en Bootstrap: {ex.Message}");
}

// Mantener la consola visible cuando se ejecuta con doble click
if (Environment.GetCommandLineArgs().Length == 1)
{
    Console.WriteLine();
    Console.Write("Presiona ENTER para salir...");
    Console.ReadLine();
}
