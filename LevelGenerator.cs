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

using SDL;
using static SDL.SDL3;

namespace BardsTaleKnockOff;

// Implementation of disjoint-set for generating mazes
class DisjointSet
{
  readonly int[] _nodes;

  public DisjointSet(int numTiles)
  {
    _nodes = new int[numTiles];
    Array.Fill(_nodes, -1);
  } 

  public void Union(int a, int b)
  {
    int ra = Find(a);
    int rb = Find(b);

    if (ra == rb)
      return;
    
    if (_nodes[ra] > _nodes[rb])
      (ra, rb) = (rb, ra);
    _nodes[ra] += _nodes[rb];
    _nodes[rb] = ra;
  }

  public int Find(int n)
  {
    if (_nodes[n] < 0)
      return n;
    else
    {
      _nodes[n] = Find(_nodes[n]);
      return _nodes[n];
    }
  }  
}

class LevelGenerator
{
  const int MATRIX_SIZE = DungeonLevel.MAP_SIZE * DungeonLevel.MAP_SIZE;

  static bool InBounds(int n) => n >= 0 && n < MATRIX_SIZE;

  static List<int> DisjointNeighbours(int n, DisjointSet ds)
  {
    List<int> neighbours = [];
    
    int other = n - DungeonLevel.MAP_SIZE;
    if (InBounds(other) && ds.Find(n) != ds.Find(other))
      neighbours.Add(other);
    
    other = n + DungeonLevel.MAP_SIZE;
    if (InBounds(other) && ds.Find(n) != ds.Find(other))
      neighbours.Add(other);

    int rowN = n / DungeonLevel.MAP_SIZE;
    
    other = n - 1;
    if (InBounds(other) && other / DungeonLevel.MAP_SIZE == rowN && ds.Find(n) != ds.Find(other))
      neighbours.Add(other);

    other = n + 1;
    if (InBounds(other) && other / DungeonLevel.MAP_SIZE == rowN && ds.Find(n) != ds.Find(other))
      neighbours.Add(other);

    return neighbours; 
  }

  static bool AnyDisjointNeighbours(int n, DisjointSet ds)
  {    
    int other = n - DungeonLevel.MAP_SIZE;
    if (InBounds(other) && ds.Find(n) != ds.Find(other))
      return true;
    
    other = n + DungeonLevel.MAP_SIZE;
    if (InBounds(other) && ds.Find(n) != ds.Find(other))
      return true;

    int rowN = n / DungeonLevel.MAP_SIZE;
    
    other = n - 1;
    if (InBounds(other) && other / DungeonLevel.MAP_SIZE == rowN && ds.Find(n) != ds.Find(other))
      return true;

    other = n + 1;
    if (InBounds(other) && other / DungeonLevel.MAP_SIZE == rowN && ds.Find(n) != ds.Find(other))
      return true;

    return false; 
  }

  static void CarveWall(DungeonLevel lvl, int a, int b)
  {
    int rowA = a / DungeonLevel.MAP_SIZE, colA = a % DungeonLevel.MAP_SIZE;
    int rowB = b / DungeonLevel.MAP_SIZE, colB = b % DungeonLevel.MAP_SIZE;

    // Which walls to remove?
    if (colA < colB)
    {
      lvl.SetTile(colA, rowA, lvl.TileAt(colA, rowA) with { EdgeE = Edge.Open });
      lvl.SetTile(colB, rowB, lvl.TileAt(colB, rowB) with { EdgeW = Edge.Open });
    }
    else if (colA > colB)
    {
      lvl.SetTile(colA, rowA, lvl.TileAt(colA, rowA) with { EdgeW = Edge.Open });
      lvl.SetTile(colB, rowB, lvl.TileAt(colB, rowB) with { EdgeE = Edge.Open });
    }
    else if (rowA < rowB)
    {
      lvl.SetTile(colA, rowA, lvl.TileAt(colA, rowA) with { EdgeS = Edge.Open });
      lvl.SetTile(colB, rowB, lvl.TileAt(colB, rowB) with { EdgeN = Edge.Open });
    }
    else
    {
      lvl.SetTile(colA, rowA, lvl.TileAt(colA, rowA) with { EdgeN = Edge.Open });
      lvl.SetTile(colB, rowB, lvl.TileAt(colB, rowB) with { EdgeS = Edge.Open });
    }
  }

  static void MazifyPass(DungeonLevel lvl, List<int> nodes, DisjointSet ds, Rng rng)
  {
    nodes.Shuffle(rng);
    foreach (int n in nodes)
    {
      var neighbours = DisjointNeighbours(n, ds);
      if (neighbours.Count > 0)
      {
        int neighbour = neighbours[rng.Next(neighbours.Count)];
        CarveWall(lvl, n, neighbour);
        ds.Union(n, neighbour);
      }
    }
  }

  static void GetDisjointNodes(List<int> nodes, DisjointSet ds)
  {
    nodes.Clear();
    for (int j = 0; j < MATRIX_SIZE; j++)
    {
      if (AnyDisjointNeighbours(j, ds))
        nodes.Add(j);
    }
  }

  static void Mazify(DungeonLevel lvl, Rng rng)
  {
    DisjointSet ds = new(MATRIX_SIZE);
    List<int> nodes = [];
    GetDisjointNodes(nodes, ds);

    do
    {
      MazifyPass(lvl, nodes, ds, rng);
      GetDisjointNodes(nodes, ds);
    }
    while (nodes.Count > 0);
    
        
    SaveMapImage(lvl, "level.png");
  }

  static DungeonLevel Init()
  {
    DungeonLevel lvl = new();
    for (int x = 0; x < DungeonLevel.MAP_SIZE; x++)
    {
      for (int y = 0; y < DungeonLevel.MAP_SIZE; y++)
      {
        lvl.SetTile(x, y, new Tile(Floor.Stone, Ceiling.Stone, Feature.None, Edge.Wall, Edge.Wall, Edge.Wall, Edge.Wall));
      }
    }

    Rng rng = new(DateTime.UtcNow.GetHashCode());
    //Rng rng = new(7_123_456);
    
    Mazify(lvl, rng);

    return lvl;
  }

  public static void GenLevel()
  {
    var lvl = Init();

    SaveMapImage(lvl, "maze 4.png");
  }

  // Draws the level as a top-down map and saves it as a PNG, north at the top.
  // Each tile draws its own edges just inside its own cell, so a wall shared by
  // two tiles shows as a double line, and a one-sided edge (e.g. wall on one
  // tile but open on its neighbour) shows as a single line on one side.
  // With showGrid off, floors fill their whole cell, so only walls show.
  public static unsafe void SaveMapImage(DungeonLevel lvl, string path, int cell = 24, bool showGrid = false)
  {
    const int Border = 4;
    int size = DungeonLevel.MAP_SIZE * cell + 2 * Border;

    SDL_Surface* surface = SDL_CreateSurface(size, size, SDL_PixelFormat.SDL_PIXELFORMAT_ARGB8888);
    if (surface == null)
      throw new Exception($"Couldn't create map surface: {SDL_GetError()}");
    SDL_Renderer* r = SDL_CreateSoftwareRenderer(surface);
    if (r == null)
    {
      SDL_DestroySurface(surface);
      throw new Exception($"Couldn't create map renderer: {SDL_GetError()}");
    }

    void Fill(byte red, byte green, byte blue, float x, float y, float w, float h)
    {
      SDL_SetRenderDrawColor(r, red, green, blue, 255);
      var rect = new SDL_FRect { x = x, y = y, w = w, h = h };
      SDL_RenderFillRect(r, &rect);
    }

    Fill(0, 0, 0, 0, 0, size, size);

    int t = Math.Max(2, cell / 8); // wall thickness
    for (int y = 0; y < DungeonLevel.MAP_SIZE; y++)
    {
      for (int x = 0; x < DungeonLevel.MAP_SIZE; x++)
      {
        var tile = lvl.TileAt(x, y);
        float cx = Border + x * cell, cy = Border + y * cell;

        // Floor, optionally with a 1px gap so the grid shows.
        int gap = showGrid ? 1 : 0;
        if (tile.Floor == Floor.Dirt)
          Fill(90, 65, 40, cx + gap, cy + gap, cell - 2 * gap, cell - 2 * gap);
        else
          Fill(55, 55, 65, cx + gap, cy + gap, cell - 2 * gap, cell - 2 * gap);

        // Features as a coloured square in the middle of the cell.
        if (tile.Feature != Feature.None)
        {
          var (fr, fg, fb) = tile.Feature switch
          {
            Feature.StairsUp => ((byte)80, (byte)220, (byte)80),
            Feature.StairsDown => ((byte)220, (byte)80, (byte)80),
            Feature.Tree => ((byte)30, (byte)140, (byte)30),
            Feature.Trap_Dart0 => ((byte)230, (byte)120, (byte)0),
            _ => ((byte)90, (byte)140, (byte)230)
          };
          float m = cell / 3f;
          Fill(fr, fg, fb, cx + m, cy + m, cell - 2 * m, cell - 2 * m);
        }

        foreach (var dir in new[] { Dir.North, Dir.East, Dir.South, Dir.West })
        {
          var edge = tile.EdgeForDir(dir);
          if (edge == Edge.Open)
            continue;

          // The strip along this side of the cell.
          var (ex, ey, ew, eh) = dir switch
          {
            Dir.North => (cx, cy, cell, t),
            Dir.South => (cx, cy + cell - t, cell, t),
            Dir.West => (cx, cy, t, cell),
            _ => (cx + cell - t, cy, t, cell)
          };
          bool horizontal = dir is Dir.North or Dir.South;

          switch (edge)
          {
            case Edge.Wall:
              Fill(230, 230, 230, ex, ey, ew, eh);
              break;
            case Edge.PermWall:
              Fill(150, 150, 255, ex, ey, ew, eh);
              break;
            case Edge.StoneDoor:
              // Wall with a gap in the middle third.
              Fill(230, 230, 230, ex, ey, ew, eh);
              if (horizontal)
                Fill(200, 140, 40, ex + cell / 3f, ey, cell / 3f, eh);
              else
                Fill(200, 140, 40, ex, ey + cell / 3f, ew, cell / 3f);
              break;
            case Edge.IllusoryWall:
              // Dashed.
              int dash = Math.Max(2, cell / 6);
              for (int i = 0; i < cell; i += 2 * dash)
              {
                float len = Math.Min(dash, cell - i);
                if (horizontal)
                  Fill(230, 230, 230, ex + i, ey, len, eh);
                else
                  Fill(230, 230, 230, ex, ey + i, ew, len);
              }
              break;
          }
        }
      }
    }

    SDL_RenderPresent(r);
    bool ok = SDL_SavePNG(surface, path);
    string err = ok ? "" : SDL_GetError() ?? "";
    SDL_DestroyRenderer(r);
    SDL_DestroySurface(surface);
    if (!ok)
      throw new Exception($"Couldn't save map image to {path}: {err}");
  }
}