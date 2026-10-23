namespace BardsTaleKnockOff;

readonly record struct Rgb(byte R, byte G, byte B);

enum DistanceFade
{
  // Far things get darker (a light source in the dark: dungeons, night).
  ToBlack,
  // Far things become see-through and melt into the sky/ground behind them
  // (haze on a bright day).
  ToBackground
}

// How a level looks. Sheets are file names in the assets folder, and every
// sheet uses the same 640x240 slot layout, so adding variety is a matter of
// dropping in a new sheet and pointing a style (or later, a square or edge) at
// it.
class LevelStyle
{
  public required string FloorSheet { get; init; }
  public string? CeilingSheet { get; init; }
  public required Dictionary<Edge, string> EdgeSheets { get; init; }
  public Dictionary<Feature, string> FeatureSheets { get; init; } = [];

  // Background drawn before anything else. The sky is drawn as horizontal
  // bands from the top of the view down to the horizon; the ground fills
  // everything below the horizon.
  public required Rgb[] SkyBands { get; init; }
  public required Rgb Ground { get; init; }

  public DistanceFade Fade { get; init; } = DistanceFade.ToBlack;
  public int DefaultViewRange { get; init; } = 2;

  static readonly Dictionary<Edge, string> StoneEdges = new()
  {
    [Edge.Wall] = "walls.png",
    [Edge.Door] = "doors.png"
  };

  static readonly Dictionary<Feature, string> OutdoorFeatures = new()
  {
    [Feature.Tree] = "tree.png"
  };

  public static readonly LevelStyle Dungeon = new()
  {
    FloorSheet = "floor.png",
    CeilingSheet = "ceiling.png",
    EdgeSheets = StoneEdges,
    SkyBands = [new(0, 0, 0)],
    Ground = new(0, 0, 0),
    Fade = DistanceFade.ToBlack,
    DefaultViewRange = 2
  };

  public static readonly LevelStyle TownDay = new()
  {
    FloorSheet = "grass.png",
    EdgeSheets = StoneEdges,
    FeatureSheets = OutdoorFeatures,
    SkyBands = [new(64, 104, 184), new(86, 128, 204), new(112, 154, 220), new(146, 184, 232), new(178, 206, 238)],
    Ground = new(122, 84, 56),
    Fade = DistanceFade.ToBackground,
    DefaultViewRange = 3
  };

  public static readonly LevelStyle TownNight = new()
  {
    FloorSheet = "grass.png",
    EdgeSheets = StoneEdges,
    FeatureSheets = OutdoorFeatures,
    SkyBands = [new(4, 6, 20), new(8, 11, 32), new(14, 18, 46), new(22, 28, 62)],
    Ground = new(0, 0, 0),
    Fade = DistanceFade.ToBlack,
    DefaultViewRange = 1
  };
}
