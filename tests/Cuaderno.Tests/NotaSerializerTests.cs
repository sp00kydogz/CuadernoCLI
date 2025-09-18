using System.IO;
using Xunit;
using Cuaderno.Core.Models;
using Cuaderno.Core.Services;
using Cuaderno.Core.Utils;

public class NotaSerializerTests
{
    [Fact]
    public void Save_Then_Load_Roundtrip_Ok()
    {
        var root = Path.Combine(Path.GetTempPath(), "Cuaderno_Test");
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(Path.Combine(root, "materias", "Programacion"));

        var fecha = DateOnly.FromDateTime(System.DateTime.Now);
        var titulo = "Roundtrip YAML";
        var kebab = SlugHelper.ToKebab(titulo);
        var path = PathHelper.NotaPath(root, "Programacion", fecha, kebab);

        var nota = new Nota
        {
            Meta = new NotaFrontMatter
            {
                materia = "Programacion",
                fecha   = fecha.ToString("yyyy-MM-dd"),
                titulo  = titulo,
                tags    = new() { "xunit", "serializer" }
            },
            Cuerpo = "Linea 1\nLinea 2\n"
        };

        NotaSerializer.Save(nota, path);
        Assert.True(File.Exists(path));

        var loaded = NotaSerializer.Load(path);
        Assert.Equal(nota.Meta.titulo, loaded.Meta.titulo);
        Assert.Equal(nota.Meta.materia, loaded.Meta.materia);
        Assert.Equal(nota.Meta.tags.Count, loaded.Meta.tags.Count);
        Assert.Contains("xunit", loaded.Meta.tags);
        Assert.Contains("Linea 2", loaded.Cuerpo);
    }
}
