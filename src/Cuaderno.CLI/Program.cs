// =============================================
// File: src/Cuaderno.CLI/Program.cs
// Target: .NET 8 Console
// Desc: Punto de entrada. Prepara carpeta "Cuaderno/", _config.json y Git.
// =============================================
using System.Text;
using Cuaderno.Core;

try
{
    var cuadernoPath = Path.Combine(Environment.CurrentDirectory, "Cuaderno");

    Bootstrap.EnsureCuadernoReady(cuadernoPath);

    Console.WriteLine($"Cuaderno listo en: {cuadernoPath}");
    Console.WriteLine("- _config.json presente");
    Console.WriteLine("- Repoitorio Git inicializado");
    Console.WriteLine();
    Console.WriteLine("(Por ahora solo bootstrap. A continuación vendrá la TUI y los comandos :new, :o, :w, :q)");
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error en Bootstrap: {ex.Message}");
}

//Mantener la consola Visible cuando se ejecuta con doble click
if (Environment.GetCommandLineArgs().Length == 1)
{
    Console.WriteLine();
    Console.Write("Presiona ENTER para salir...");
    Console.ReadLine();
}