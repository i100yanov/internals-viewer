﻿using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace InternalsViewer.UI.App.Helpers;

public static class FileHelpers
{
    public static async Task<T?> ReadFile<T>(string folderPath, string fileName)
    {
        var path = Path.Combine(folderPath, fileName);

        if (File.Exists(path))
        {
            var json = await File.ReadAllTextAsync(path);

            return JsonSerializer.Deserialize<T>(json);
        }

        return default;
    }

    public static async Task SaveFile<T>(string folderPath, string fileName, T content)
    {
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        var fileContent = JsonSerializer.Serialize(content);

        await File.WriteAllTextAsync(Path.Combine(folderPath, fileName), fileContent, Encoding.UTF8);
    }

    public static void DeleteFile(string folderPath, string? filename)
    {
        if (filename != null && File.Exists(Path.Combine(folderPath, filename)))
        {
            File.Delete(Path.Combine(folderPath, filename));
        }
    }
}