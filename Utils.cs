// {BardsTaleKnockoff} - A computer RPG
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

enum Buff { Defending, Hiding }
record struct TempBuff(Buff Buff, ulong ExpiresOn, bool CombatOnly);

readonly record struct Rgb(byte R, byte G, byte B);

// Indoors, far things get darker (a light source in the dark: dungeons, night).
// Outdoors, things become see-through and melt into the sky/ground behind them
// (haze on a bright day).
enum DistanceFade { ToBlack, ToBackground }

static class Util
{
  const string GameFolder = "DanasBardsTale";
  public static double ToDouble(string s) => double.Parse(s, System.Globalization.CultureInfo.InvariantCulture);

  // This shows up in a couple places and I suspect it will eventually get
  // more complicated  
  // Maybe eventually items or having particular classes in your party
  // or some items will increase your chance to flee?  
  public static bool TryToFlee(Rng rng)
  {
    const int ODDS_TO_FLEE = 75; 
    return rng.Next(100) < ODDS_TO_FLEE;    
  }

  public static string SavePath
  {
    get
    {
      try
      {
        string basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string gamePath = Path.Combine(basePath, GameFolder, "Saves");

        // CreateDirectory is safe to call even if directory exists
        Directory.CreateDirectory(gamePath);

        return gamePath;
      }
      catch (Exception ex)
      {
        throw new Exception($"Failed to initialize save directory: {ex.Message}", ex);
      }
    }
  }
}

static class ListUtils
{
  public static void Shuffle<T>(this IList<T> list, Rng rng)
  {
    int n = list.Count;
    while (n > 1)
    {
      int k = rng.Next(n);
      n--;
      (list[n], list[k]) = (list[k], list[n]);
    }
  }
}

static class StringUtils
{
  public static string DefArticle(this string s) => $"the {s}";

  public static string IndefArticle(this string s) => s[0] switch
  {
    'A' or 'E' or 'I' or 'O' or 'U' or 'Y' or
    'a' or 'e' or 'i' or 'o' or 'u' or 'y' => $"an {s}",
    >= '0' and <= '9' => s,
    _ => $"a {s}"
  };

  public static string Capitalize(this string s)
  {
    if (s != "" && char.IsLetter(s[0]))
      return $"{char.ToUpper(s[0])}{s[1..]}";
    else
      return s;
  }

  public static string Pluralize(this string s)
  {
    if (s.Contains(" of "))
    {
      int space = s.IndexOf(' ');
      s = s[..space] + 's' + s[space..];

      return s;
    }
    else if (s == "elf")
      return "elves";
    else if (s.EndsWith('s') || s.EndsWith('x') || s.EndsWith("ch"))
      return s + "es";
    else
      return s + "s";
  }
}

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