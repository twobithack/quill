using Quill.Core;
using Quill.UI;
using System.IO;
using System.Text.Json;

namespace Quill;

public static class Program
{
  static void Main(string[] args)
  {
    if (args == null || args.Length == 0)
      return;

    var romPath = args[0];
    var config = LoadConfiguration();

    using var quill = new Client(romPath,
                                 scaleFactor: config.ScaleFactor,
                                 extraScanlines: config.ExtraScanlines);
    quill.Run();
  }

  private static Configuration LoadConfiguration()
  {
    var configPath = Path.Join(Directory.GetCurrentDirectory(), "config.json");
    var configJson = File.ReadAllText(configPath);
    var options = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    return JsonSerializer.Deserialize<Configuration>(configJson, options);
  }
}
