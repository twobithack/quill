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
    var romPath = args[0];
    var rom = LoadROM(romPath);
    var savePath = BuildSavePath(romPath);
    var config = LoadConfiguration();
    var bios = LoadBIOS();

    var emulator = new Emulator(bios, rom, savePath, config);
    using var quill = new Window(emulator, config);
    quill.Run();
  }

  private static byte[] LoadBIOS()
  {
    var filepath = Path.Join(AppContext.BaseDirectory, "bios.sms");
    return File.Exists(filepath)
         ? File.ReadAllBytes(filepath)
         : [];
  }

  private static byte[] LoadROM(string romPath) => File.Exists(romPath)
                                                 ? File.ReadAllBytes(romPath)
                                                 : [0x00];

  private static Configuration LoadConfiguration()
  {
    var filepath = Path.Join(AppContext.BaseDirectory, "config.json");
    if (!File.Exists(filepath))
      return new Configuration();

    var config = File.ReadAllText(filepath);
    var options = new JsonSerializerOptions
    {
      PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    return JsonSerializer.Deserialize<Configuration>(config, options);
  }
  
  private static string BuildSavePath(string romPath)
  {
    var romName = string.IsNullOrEmpty(romPath)
                ? "_no_rom"
                : Path.GetFileNameWithoutExtension(romPath);
    var savesDirectory = Path.Join(AppContext.BaseDirectory, "saves");
    Directory.CreateDirectory(savesDirectory);
    return Path.Combine(savesDirectory, romName + ".save");
  }
}
