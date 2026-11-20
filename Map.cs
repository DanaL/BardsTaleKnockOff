// Some Other Tale - A computer RPG
// Written in 2026 by Dana Larose <ywg.dana@gmail.com>
//
// To the extent possible under law, the author(s) have dedicated all copyright
// and related and neighboring rights to this software to the public domain
// worldwide. This software is distributed without any warranty.
//
// You should have received a copy of the CC0 Public Domain Dedication along 
// with this software. If not, 
// see <http://creativecommons.org/publicdomain/zero/1.0/>.

namespace BardsTaleKnockOff;

enum Ceiling : byte { None, Stone }
enum Floor : byte { Stone, Dirt, Water }

enum Edge : byte { Open, PermWall, Wall, StoneDoor, IllusoryWall, HouseWall, HouseDoor, HouseWallGr, HouseDoorGr,
                      Portcullis }

static class EdgeExt
{
  public static string? EdgeSheet(this Edge e) => e switch
  {
    Edge.PermWall => "walls.png",
    Edge.Wall => "walls.png",
    Edge.StoneDoor => "doors.png",
    Edge.IllusoryWall => "walls.png",
    Edge.HouseWall => "house.png",
    Edge.HouseDoor => "house_door.png",
    Edge.HouseWallGr => "house_gr.png",
    Edge.HouseDoorGr => "house_door_gr.png",
    Edge.Portcullis => "portcullis.png",
    _ => null
  };

  public static bool Opaque(this Edge e) => e is not Edge.Open;
}

// Something standing in the middle of a square, drawn as a sprite that fills
// the square (e.g. a tree). Unlike edges, these belong to the square itself.
enum Feature : byte { None, Burgemeester, Encounter, Guild, House, Lever, Shop, Special1, Special2, 
                      Special3, Special4, Special5, Special6, Special7, Special8, Spinner, StairsUp, 
                      StairsDown, Tavern, Teleporter, Temple, TrainingHall, Trap_Blades, Trap_Dart, 
                      Trap_PoisonedNeedles, Tree, Water }

static class FeatureExt
{
  public static bool Blocks(this Feature f) => f is Feature.Tree;

  public static string? FeatureSheet(this Feature f) => f switch
  {
    Feature.Tree => "tree.png",
    Feature.Water => "water.png",
    _ => null
  };
}

enum Dir { North, East, South, West, None }

static class DirExt
{
  public static Dir TurnLeft(this Dir d) => (Dir)(((int)d + 3) % 4);
  public static Dir TurnRight(this Dir d) => (Dir)(((int)d + 1) % 4);
  public static Dir Opposite(this Dir d) => (Dir)(((int)d + 2) % 4);

  public static (int dx, int dy) Delta(this Dir d) => d switch
  {
    Dir.North => (0, -1),
    Dir.East => (1, 0),
    Dir.South => (0, 1),
    _ => (-1, 0)
  };
}

record Encounter(Monsters Monster, int Count, int Distance);

static class EncounterExt
{
  public static MonsterGroup ToMonsterGroup(this Encounter e, Rng rng)
  {
    MonsterGroup mg = new() { Distance = e.Distance };

    for (int j = 0; j < e.Count; j++)
      mg.Monsters.Add(MonsterCatalog.Get(e.Monster, rng));

    return mg;
  }
}

[Flags]
enum TileFlags : ushort
{
  None     = 0b00000000,
  Darkness = 0b00000001,
}

record struct Tile(Floor Floor, Ceiling Ceiling, Feature Feature, Edge EdgeN, Edge EdgeS, Edge EdgeE, Edge EdgeW, TileFlags Flags = TileFlags.None)
{

  public readonly Edge EdgeForDir(Dir dir) => dir switch {
    Dir.North => EdgeN,
    Dir.South => EdgeS,
    Dir.East => EdgeE,
    _ => EdgeW
  };

  public readonly List<Dir> Exits()
  {
    List<Dir> exits = [];

    if (EdgeN == Edge.Open || EdgeN == Edge.StoneDoor)
      exits.Add(Dir.North);
    if (EdgeS == Edge.Open || EdgeS == Edge.StoneDoor)
      exits.Add(Dir.South);
    if (EdgeW == Edge.Open || EdgeW == Edge.StoneDoor)
      exits.Add(Dir.West);
    if (EdgeE == Edge.Open || EdgeE == Edge.StoneDoor)
      exits.Add(Dir.East);
      
    return exits;
  }

  public readonly bool IsStairs() => Feature is Feature.StairsUp or Feature.StairsDown or Feature.Special5 or Feature.Special8;
  public readonly bool IsTrap() => Feature is Feature.Trap_Dart or Feature.Trap_PoisonedNeedles or Feature.Trap_Blades;
  public readonly bool HasFlag(TileFlags flag) => Flags.HasFlag(flag);
}

// Bard's Tale levels are 22x22 and wrap. Technically BT maps were all toruses
class DungeonLevel
{
  public const int MAP_SIZE = 22;
  readonly Tile[,] _tiles = new Tile[MAP_SIZE, MAP_SIZE];
  public Dictionary<(int, int), string> Messages { get; } = [];
  public Dictionary<(int, int), List<Encounter>> Encounters { get; } = [];
  public Dictionary<(int, int), (int, int)> Teleporters { get; } = [];
  
  public static int Wrap(int v) => ((v % MAP_SIZE) + MAP_SIZE) % MAP_SIZE;
  public Tile TileAt(int x, int y) => _tiles[Wrap(x), Wrap(y)];
  public void SetTile(int x, int y, Tile tile) => _tiles[Wrap(x), Wrap(y)] = tile;
  
  // Can the party step from (x, y) through the given side?
  public bool Passable(int x, int y, Dir side)
  {
    var tile = TileAt(x, y);
    Edge edge = tile.EdgeForDir(side);
    
    if (edge is Edge.Wall or Edge.PermWall or Edge.HouseWall or Edge.HouseWallGr or Edge.Portcullis)
      return false;
    
    var (dx, dy) = side.Delta();
    return !TileAt(x + dx, y + dy).Feature.Blocks();
  }

  // Pick a random square to move to from x,y. If for some reason there are no
  // open exits, return x,y and a random dir
  public (int X, int Y, Dir dir) RetreatLocation(int x, int y, Rng rng)
  {
    List<Dir> dirs = [ Dir.North, Dir.South, Dir.East, Dir.West ];
    List<Dir> opts = [];
    
    foreach (Dir d in dirs)
    {
      if (Passable(x, y, d))
        opts.Add(d);     
    }

    if (opts.Count > 0)
    {
      Dir dir = opts[rng.Next(opts.Count)];
      (int deltax, int deltay) = dir.Delta();
      return (x + deltax, y + deltay, dir.Opposite());
    }
    else
    {
      return (x, y, dirs[rng.Next(dirs.Count)]);
    }
  }
}
