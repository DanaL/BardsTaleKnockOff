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

namespace BardsTaleKnockOff;

enum Ceiling : byte { None, Stone }
enum Floor : byte { Stone, Dirt }

enum Edge : byte { Open, PermWall, Wall, StoneDoor }

static class EdgeExt
{
  public static string? EdgeSheet(this Edge e) => e switch
  {
    Edge.PermWall => "walls.png",
    Edge.Wall => "walls.png",
    Edge.StoneDoor => "doors.png",
    _ => null
  };
}

// Something standing in the middle of a square, drawn as a sprite that fills
// the square (e.g. a tree). Unlike edges, these belong to the square itself.
enum Feature : byte { None, Tree, Guild, House, Shop }

static class FeatureExt
{
  public static bool Blocks(this Feature f) => f == Feature.Tree;

  public static string? FeatureSheet(this Feature f) => f switch
  {
    Feature.Tree => "tree.png",
    _ => null
  };
}

enum Dir { North, East, South, West }

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

record struct Tile(Floor Floor, Ceiling Ceiling, Feature Feature, Edge EdgeN, Edge EdgeS, Edge EdgeE, Edge EdgeW)
{
  public Edge EdgeForDir(Dir dir) => dir switch {
    Dir.North => EdgeN,
    Dir.South => EdgeS,
    Dir.East => EdgeE,
    _ => EdgeW
  };
}

// Bard's Tale levels are 22x22 and wrap. Technically BT maps were all toruses
class Dungeon
{
  public bool Indoors { get; set; } = true;
  public const int Size = 22;
  readonly Tile[,] _tiles = new Tile[Size, Size];

  public static int Wrap(int v) => ((v % Size) + Size) % Size;
  public Tile TileAt(int x, int y) => _tiles[Wrap(x), Wrap(y)];
  public void SetTile(int x, int y, Tile tile) => _tiles[x, y] = tile;

  // Can the party step from (x, y) through the given side?
  public bool Passable(int x, int y, Dir side)
  {
    var tile = _tiles[x, y];
    Edge edge = side switch
    {
      Dir.North => tile.EdgeN,
      Dir.South => tile.EdgeS,
      Dir.East => tile.EdgeE,
      _ => tile.EdgeW
    };
    if (edge == Edge.Wall || edge == Edge.PermWall)
      return false;
    
    var (dx, dy) = side.Delta();
    return !_tiles[x + dx, y + dy].Feature.Blocks();
  }
}
