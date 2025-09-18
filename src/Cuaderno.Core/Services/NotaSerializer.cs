// =============================================
// File: src/Cuaderno.Core/Services/NotaSerializer.cs
// Desc: Carga/guarda notas con front-matter YAML usando YamlDotNet
// NuGet: dotnet add src/Cuaderno.Core package YamlDotNet
// =============================================

using System.Text;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Cuaderno.Core.Models;
using System.Diagnostics.Tracing;

namespace Cuaderno.Core.Services;

public static class NotaSerializer
{
    private static readonly Regex FrontMatterRegex = new(
        pattern: @"^---\r?\n(?<yaml>[\s\S]*?)\r?\n---\r?\n",
        RegexOptions.Compiled);

    public static Nota Load(string path) {
        var text = File.ReadAllText(path, Encoding.UTF8);
        var m = FrontMatterRegex.Match(text);
        if (!m.Success) {
            return new Nota
            {
                Meta = new NotaFrontMatter(),
                Cuerpo = text
            };
        }
        var yaml = m.Groups["yaml"].Value;
        var cuerpo = text[m.Length..];

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        var meta = deserializer.Deserialize<NotaFrontMatter>(yaml) ?? new NotaFrontMatter();
        return new Nota
        {
            Meta = meta,
            Cuerpo = cuerpo
        };
    }

    public static void Save(Nota nota, string path) {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults | DefaultValuesHandling.OmitNull)
            .Build();

        var yaml = serializer.Serialize(nota.Meta).TrimEnd();
        var sb = new StringBuilder();
        sb.AppendLine("---");
        sb.AppendLine(yaml);
        sb.AppendLine("---");
        sb.Append(nota.Cuerpo ?? string.Empty);
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }
}