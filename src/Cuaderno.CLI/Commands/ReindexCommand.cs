using System;
using Cuaderno.Core;

namespace Cuaderno.Cli.Commands;

public static class ReindexCommand
{
    public static int Run(string rootPath)
    {
        try
        {
            var svc = new IndexService(rootPath);
            var idx = svc.Rebuild(includeSummary: true);
            svc.Save(idx);
            Console.WriteLine($"[OK] Índice regenerado: {idx.Entradas.Count} entradas.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("[ERROR] No se pudo reindexar:");
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }
}
