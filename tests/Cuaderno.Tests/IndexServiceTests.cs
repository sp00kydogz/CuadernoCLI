using System.IO;
using System.Text;
using System.Text.Json;
using Cuaderno.Core;
using Cuaderno.Core.Index;
using Xunit;

public class IndexServiceTests
{
    [Fact]
    public void Rebuild_CreatesIndexWithEntries()
    {
        using var dir = new TempDir(); // asume que ya tienes un helper; si no, crea uno
        var root = Path.Combine(dir.Path, "Cuaderno");
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(Path.Combine(root, "Estudios", "Redes"));

        var note = """
        ---
        title: Resumen VLAN
        date: 2025-09-10
        tags: [Cisco, VLAN, Tarea]
        ---
        # VLAN
        Texto de prueba
        """;

        File.WriteAllText(Path.Combine(root, "Estudios/Redes/2025-09-10-Resumen-VLAN.md"), note, Encoding.UTF8);

        var svc = new IndexService(root);
        var idx = svc.Rebuild();
        Assert.NotEmpty(idx.Entradas);
        var e = idx.Entradas[0];
        Assert.Equal("Estudios", e.Categoria);
        Assert.Equal("Redes", e.Subcategoria);
        Assert.Equal("Resumen VLAN", e.Titulo);
        Assert.Contains("VLAN", e.Tags);
        Assert.StartsWith("sha256:", e.Hash);

        // Save and verify JSON
        var outFile = Path.Combine(root, "_indice.json");
        svc.Save(idx, outFile);
        var json = File.ReadAllText(outFile);
        var loaded = JsonSerializer.Deserialize<IndexFile>(json);
        Assert.NotNull(loaded);
        Assert.True(loaded!.Entradas.Count == 1);
    }
}
