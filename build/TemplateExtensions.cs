using System;
using System.Linq;
using Fallout.Common.IO;
using Scriban;
using Serilog;

internal static class TemplateExtensions
{
    public static void RenderToFileIfNotEmpty(this Template template, AbsolutePath targetPath, object model, params string[] foldersToExclude)
    {
        if (foldersToExclude.Any(f => targetPath.ToString().Contains(f, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        targetPath.Parent.CreateDirectory();

        string content = template.Render(model);

        if (!string.IsNullOrWhiteSpace(content))
        {
            Log.Information("Rendering to {target}", targetPath);

            targetPath.WriteAllText(content);
        }
        else
        {
            Log.Information("Skipping writing to {target} as the content is empty", targetPath);
        }
    }
}
