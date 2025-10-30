using System;
using System.IO;
using System.Text.Json;

using Quill.Common;
using Quill.Core;

namespace Quill.Client;

public static class Program
{
  static void Main(string[] args)
  {
    if (args == null || args.Length == 0)
    {
      Console.WriteLine("Error: No ROM provided.");
      return;
    }

    var romPath = args[0];
    if (!File.Exists(romPath))
    {
      Console.WriteLine($"Error: Could not find ROM file: {romPath}");
      return;
    }

    var rom = LoadROM(romPath);
    var config = LoadConfiguration();
    var savePath = BuildSavePath(romPath);

    var emulator = new Emulator(rom, savePath, config);
    using var quill = new Window(emulator, config);
    quill.Run();
  }

  private static byte[] LoadROM(string romPath) => File.ReadAllBytes(romPath);

  private static Configuration LoadConfiguration()
  {
    var configPath = Path.Join(Directory.GetCurrentDirectory(), "config.json");
    
    if (!File.Exists(configPath))
      return new Configuration();

    #pragma warning disable CA1869
    var config = File.ReadAllText(configPath);
    var options = new JsonSerializerOptions
    {
      PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    #pragma warning restore CA1869

    return JsonSerializer.Deserialize<Configuration>(config, options);
  }
  
  private static string BuildSavePath(string romPath)
  {
    var romName = Path.GetFileNameWithoutExtension(romPath);
    var savesDirectory = Path.Combine(Path.GetDirectoryName(romPath), "saves");
    Directory.CreateDirectory(savesDirectory);
    return Path.Combine(savesDirectory, romName + ".save");
  }
}
