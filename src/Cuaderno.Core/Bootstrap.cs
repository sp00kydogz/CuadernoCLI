// =============================================
// File: src/Cuaderno.Core/Bootstrap.cs
// Desc: Lógica de preparación de carpeta, config y Git.
// Nota: Requiere NuGet LibGit2Sharp y Newtonsoft.Json instalados en Cuaderno.Core
// dotnet add src/Cuaderno.Core package LibGit2Sharp
// dotnet add src/Cuaderno.Core package Newtonsoft.Json
// =============================================
using System.Text;
using LibGit2Sharp;
using Newtonsoft.Json;

namespace Cuaderno.Core;

public static class Bootstrap
{
    public static void EnsureCuadernoReady(string root)
    {
        EnsureDirectory(root);
        var configPath = Path.Combine(root, "_config.json");
        EnsureDefaultConfig(configPath);
        TryEnsureGit(root, configPath);
    }

    private static void EnsureDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        var materiasDir = Path.Combine(path, "materias");
        if (!Directory.Exists(materiasDir))
            Directory.CreateDirectory(materiasDir);

        var adjuntosDir = Path.Combine(path, "adjuntos");
        if (!Directory.Exists(adjuntosDir))
            Directory.CreateDirectory(adjuntosDir);
    }

    private static void EnsureDefaultConfig(string configPath)
    {
        if (File.Exists(configPath)) return;

        var cfg = new DefaultConfig
        {
            autor = "Osito",
            locale = "es-CL",
            materias = new List<string>(),
            plantillas = new Plantillas
            {
                clase = new() { "## Objetivo", "## Desarrollo", "## Tareas" },
                laboratorio = new() { "## Equipo", "## Procedimiento", "## Resultados", "## Conclusiones" },
            },
            git = new GitConfig { auto_commit = true, mensaje = "nota: {archivo}" }
        };

        var json = JsonConvert.SerializeObject(cfg, Formatting.Indented);
        File.WriteAllText(configPath, json, Encoding.UTF8);
    }

    private static void TryEnsureGit(string root, string configPath) {
        try
        {
            var gitDir = Path.Combine(root, ".git");
            if (!Directory.Exists(gitDir)) {
                Repository.Init(root);
                using var repo = new Repository(root);

                Commands.Stage(repo, new[] { configPath });
                var author = new Signature("Cuaderno", "no-reply@cuaderno", DateTimeOffset.Now);
                repo.Commit("chore: bootstrap cuaderno (_config.json)", author, author);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WARN] No se pudo inicializar Git en {root}: {ex.Message}");
        }
    }

    private class DefaultConfig {
        public string autor { get; set; } = "";
        public string locale { get; set; } = "es-CL";
        public List<string> materias { get; set; } = new();
        public Plantillas plantillas { get; set; } = new();
        public GitConfig git { get; set; } = new();
    }

    private class Plantillas {
        public List<string> clase { get; set; } = new();
        public List<string> laboratorio { get; set; } = new();
    }

    private class GitConfig {
        public bool auto_commit { get; set; }
        public string mensaje { get; set; } = "nota: {archivo}";
    }
}