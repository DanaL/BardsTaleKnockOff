// {BardsTaleKnockoff} - A roguelike computer RPG
// Written in 2026 by Dana Larose <ywg.dana@gmail.com>
//
// To the extent possible under law, the author(s) have dedicated all copyright
// and related and neighboring rights to this software to the public domain
// worldwide. This software is distributed without any warranty.
//
// You should have received a copy of the CC0 Public Domain Dedication along 
// with this software. If not, 
// see <http://creativecommons.org/publicdomain/zero/1.0/>.

using BardsTaleKnockOff;

readonly record struct Rgb(byte R, byte G, byte B);

// Indoors, far things get darker (a light source in the dark: dungeons, night).
// Outdoors, things become see-through and melt into the sky/ground behind them
// (haze on a bright day).
enum DistanceFade { ToBlack, ToBackground }

static class ResourcePath
{
  public static string GetDataFilePath(string filename) => FindResourcePath("data", filename);

  static string FindResourcePath(string folder, string filename)
  {
    string path = string.IsNullOrEmpty(folder)
        ? filename
        : Path.Combine(folder, filename);

    if (File.Exists(path))
    {
      return path;
    }

    // If we are executing as a macOS app bundle, we need to look in the
    // Resources folder
    if (OperatingSystem.IsMacOS())
    {
      string exePath = AppDomain.CurrentDomain.BaseDirectory;

      if (exePath.Contains(".app/Contents/MacOS"))
      {
        string baseDir = exePath.TrimEnd(Path.DirectorySeparatorChar);
        string? contentsDir = Path.GetDirectoryName(baseDir);

        if (contentsDir != null)
        {
          string resourcePath = string.IsNullOrEmpty(folder)
              ? Path.Combine(contentsDir, "Resources", filename)
              : Path.Combine(contentsDir, "Resources", folder, filename);

          if (File.Exists(resourcePath))
          {
            return resourcePath;
          }
        }
      }
    }

    throw new FileNotFoundException(
        $"Could not find {filename} in {folder} directory. " +
        $"Tried: {path} and bundle resources path. " +
        $"Current directory: {Directory.GetCurrentDirectory()}, " +
        $"Base directory: {AppDomain.CurrentDomain.BaseDirectory}"
    );
  }
}

static class MapLoader
{
  public static Dungeon Load(string path)
  {
    Dungeon d = new();
    for (int x = 0; x < Dungeon.Size; x++)
    {
      for (int y = 0; y < Dungeon.Size; y++)
      {
        d.SetTile(x, y, new(Floor.Stone, Ceiling.Stone, Feature.None, Edge.PermWall, Edge.PermWall, Edge.PermWall, Edge.PermWall));
      }
    }

    var lines = File.ReadAllLines(path);
    d.Indoors = lines[0] != "outdoors";
    foreach (string line in lines.Skip(1))
    {
      if (string.IsNullOrWhiteSpace(line))
        continue;

      // Format: x,y:Floor,Ceiling,Feature,EdgeN,EdgeS,EdgeE,EdgeW
      string[] pieces = line.Split(':');
      string[] coords = pieces[0].Split(',');
      int x = int.Parse(coords[0]);
      int y = int.Parse(coords[1]);

      string[] f = pieces[1].Split(',');
      Tile tile = new(
        Enum.Parse<Floor>(f[0], true),
        Enum.Parse<Ceiling>(f[1], true),
        Enum.Parse<Feature>(f[2], true),
        Enum.Parse<Edge>(f[3], true),
        Enum.Parse<Edge>(f[4], true),
        Enum.Parse<Edge>(f[5], true),
        Enum.Parse<Edge>(f[6], true)
      );
      d.SetTile(x, y, tile);
    }

    return d;
  }
}