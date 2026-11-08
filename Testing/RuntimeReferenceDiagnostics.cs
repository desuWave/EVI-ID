using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;

namespace EVI_ID.Testing;

public static class RuntimeReferenceDiagnostics
{
    public static string Run()
    {
        var output = new StringBuilder();

        output.AppendLine("EVI-ID Runtime Reference Diagnostics");
        output.AppendLine();

        string? tpa = AppContext.GetData(
            "TRUSTED_PLATFORM_ASSEMBLIES") as string;

        string[] paths = string.IsNullOrWhiteSpace(tpa)
            ? Array.Empty<string>()
            : tpa.Split(
                Path.PathSeparator,
                StringSplitOptions.RemoveEmptyEntries);

        output.AppendLine(
            $"TPA paths: {paths.Length}");

        output.AppendLine(
            $"TPA files existing: {paths.Count(File.Exists)}");

        output.AppendLine();

        Type[] types =
        {
            typeof(object),
            typeof(Console),
            typeof(Enumerable),
            typeof(System.Collections.Generic.List<>),
            typeof(System.Threading.Tasks.Task)
        };

        foreach (Type type in types)
        {
            Assembly assembly = type.Assembly;

            output.AppendLine(
                $"Assembly: {assembly.GetName().Name}");

            try
            {
                string location = assembly.Location;

                output.AppendLine(
                    $"  Location empty: {string.IsNullOrEmpty(location)}");

                bool exists =
                    !string.IsNullOrEmpty(location) &&
                    File.Exists(location);

                output.AppendLine(
                    $"  File exists: {exists}");

                if (exists)
                {
                    try
                    {
                        MetadataReference.CreateFromFile(location);

                        output.AppendLine(
                            "  MetadataReference: SUCCESS");
                    }
                    catch (Exception ex)
                    {
                        output.AppendLine(
                            $"  MetadataReference: {ex.GetType().Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                output.AppendLine(
                    $"  Location: {ex.GetType().Name}");
            }
        }

        return output.ToString();
    }
}
