namespace BardsTaleKnockOff;

enum Edge : byte
{
  Open,
  Wall,
  Door
}

// Something standing in the middle of a square, drawn as a sprite that fills
// the square (e.g. a tree). Unlike edges, these belong to the square itself.
enum Feature : byte
{
  None,
  Tree
}

static class FeatureExt
{
  public static bool Blocks(this Feature f) => f == Feature.Tree;
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

// Bard's Tale levels are 22x22 and wrap. Technically BT maps were all toruses
class Dungeon
{
  public const int Size = 22;

  readonly Edge[,,] _edges = new Edge[Size, Size, 4];
  readonly Feature[,] _features = new Feature[Size, Size];

  public static int Wrap(int v) => ((v % Size) + Size) % Size;

  public Edge GetEdge(int x, int y, Dir side) => _edges[Wrap(x), Wrap(y), (int)side];

  public void SetEdge(int x, int y, Dir side, Edge edge, bool oneWay = false)
  {
    _edges[Wrap(x), Wrap(y), (int)side] = edge;
    if (!oneWay)
    {
      var (dx, dy) = side.Delta();
      _edges[Wrap(x + dx), Wrap(y + dy), (int)side.Opposite()] = edge;
    }
  }

  public void Fill(Edge edge)
  {
    for (int x = 0; x < Size; x++) {
      for (int y = 0; y < Size; y++) {
        for (int s = 0; s < 4; s++) {
          _edges[x, y, s] = edge;
        }
      }
    }
  }

  public Feature GetFeature(int x, int y) => _features[Wrap(x), Wrap(y)];
  public void SetFeature(int x, int y, Feature f) => _features[Wrap(x), Wrap(y)] = f;

  // Can the party step from (x, y) through the given side?
  public bool CanPass(int x, int y, Dir side)
  {
    if (GetEdge(x, y, side) == Edge.Wall)
      return false;
      
    var (dx, dy) = side.Delta();
    return !GetFeature(x + dx, y + dy).Blocks();
  }
}
