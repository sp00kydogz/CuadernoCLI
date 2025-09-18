// =============================================
// File: src/Cuaderno.Core/Utils/PathHelper.cs
// Desc: Construye rutas y nombres de archivo
// =============================================

namespace Cuaderno.Core.Utils;

public static class PathHelper
{
    public static string MateriaDir(string root, string materia)
        => System.IO.Path.Combine(root, "materias", materia);

    public static string NotaFilename(DateOnly fecha, string tituloKebab)
        => string.Format("{0:yyyy-MM-dd}-{1}.md", fecha, tituloKebab);

    public static string NotaPath(string root, string materia, DateOnly fecha, string tituloKebab)
    {
        var dir = MateriaDir(root, materia);
        Directory.CreateDirectory(dir);
        return System.IO.Path.Combine(dir, NotaFilename(fecha, tituloKebab));
    }
}