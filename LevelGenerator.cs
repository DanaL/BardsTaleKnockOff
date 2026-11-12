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

record struct Door(int X, int Y, Dir Dir);

class Room(int h, int w)
{
  public int H { get; set; } = h;
  public int W { get; set; } = w;

  public Dictionary<(int X, int Y), Tile> Floors = [];

  // Realistically, there'll only be at most 2 or 3 doors  I'm sure
  public List<Door> Exits { get; set; } = [];
}

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
  const int NUM_LEVELS = 3;
  const int MATRIX_SIZE = DungeonLevel.MAP_SIZE * DungeonLevel.MAP_SIZE;

  static List<Room> _roomTemplates = [
    new Room(2, 2) { Exits = [ new(0, 1, Dir.South) ],
       Floors = { [(0,0)] = new Tile(Floor.Stone, Ceiling.Stone, Feature.None, Edge.Wall, Edge.Open, Edge.Open, Edge.Wall),
                  [(1,0)] = new Tile(Floor.Stone, Ceiling.Stone, Feature.None, Edge.Wall, Edge.Open, Edge.Wall, Edge.Open),
                  [(0,1)] = new Tile(Floor.Stone, Ceiling.Stone, Feature.None, Edge.Open, Edge.StoneDoor, Edge.Open, Edge.Wall),
                  [(1,1)] = new Tile(Floor.Stone, Ceiling.Stone, Feature.None, Edge.Open, Edge.Wall, Edge.Wall, Edge.Open) },
        },
    new Room(2, 2) { Exits = [ new(0, 0, Dir.North) ],
       Floors = { [(0,0)] = new Tile(Floor.Stone, Ceiling.Stone, Feature.None, Edge.StoneDoor, Edge.Open, Edge.Open, Edge.Wall),
                  [(1,0)] = new Tile(Floor.Stone, Ceiling.Stone, Feature.None, Edge.Wall, Edge.Open, Edge.Wall, Edge.Open),
                  [(0,1)] = new Tile(Floor.Stone, Ceiling.Stone, Feature.None, Edge.Open, Edge.Wall, Edge.Open, Edge.Wall),
                  [(1,1)] = new Tile(Floor.Stone, Ceiling.Stone, Feature.None, Edge.Open, Edge.Wall, Edge.Wall, Edge.Open) },
        },
    new Room(2, 2) { Exits = [ new(0, 0, Dir.West) ],
       Floors = { [(0,0)] = new Tile(Floor.Stone, Ceiling.Stone, Feature.None, Edge.Wall, Edge.Open, Edge.Open, Edge.StoneDoor),
                  [(1,0)] = new Tile(Floor.Stone, Ceiling.Stone, Feature.None, Edge.Wall, Edge.Open, Edge.Wall, Edge.Open),
                  [(0,1)] = new Tile(Floor.Stone, Ceiling.Stone, Feature.None, Edge.Open, Edge.Wall, Edge.Open, Edge.Wall),
                  [(1,1)] = new Tile(Floor.Stone, Ceiling.Stone, Feature.None, Edge.Open, Edge.Wall, Edge.Wall, Edge.Open) },
        },
    new Room(2, 2) { Exits = [ new(1, 1, Dir.East) ],
       Floors = { [(0,0)] = new Tile(Floor.Stone, Ceiling.Stone, Feature.None, Edge.Wall, Edge.Open, Edge.Open, Edge.Wall),
                  [(1,0)] = new Tile(Floor.Stone, Ceiling.Stone, Feature.None, Edge.Wall, Edge.Open, Edge.Wall, Edge.Open),
                  [(0,1)] = new Tile(Floor.Stone, Ceiling.Stone, Feature.None, Edge.Open, Edge.Wall, Edge.Open, Edge.Wall),
                  [(1,1)] = new Tile(Floor.Stone, Ceiling.Stone, Feature.None, Edge.Open, Edge.Wall, Edge.StoneDoor, Edge.Open) },
        },
    new Room(3, 3) { Exits = [ new(0, 1, Dir.West), new (2, 1, Dir.East)],
      Floors = { [(0,0)] = new Tile(Floor.Stone, Ceiling.Stone, Feature.None, Edge.Wall, Edge.Open, Edge.Open, Edge.Wall),
                 [(1,0)] = new Tile(Floor.Stone, Ceiling.Stone, Feature.None, Edge.Wall, Edge.Open, Edge.Open, Edge.Open),
                 [(2,0)] = new Tile(Floor.Stone, Ceiling.Stone, Feature.None, Edge.Wall, Edge.Open, Edge.Wall, Edge.Open),

                 [(0,1)] = new Tile(Floor.Stone, Ceiling.Stone, Feature.None, Edge.Open, Edge.Open, Edge.Open, Edge.StoneDoor),
                 [(1,1)] = new Tile(Floor.Stone, Ceiling.Stone, Feature.None, Edge.Open, Edge.Open, Edge.Open, Edge.Open),
                 [(2,1)] = new Tile(Floor.Stone, Ceiling.Stone, Feature.None, Edge.Open, Edge.Open, Edge.StoneDoor, Edge.Open),

                 [(0,2)] = new Tile(Floor.Stone, Ceiling.Stone, Feature.None, Edge.Open, Edge.Wall, Edge.Open, Edge.Wall),
                 [(1,2)] = new Tile(Floor.Stone, Ceiling.Stone, Feature.None, Edge.Open, Edge.Wall, Edge.Open, Edge.Open),
                 [(2,2)] = new Tile(Floor.Stone, Ceiling.Stone, Feature.None, Edge.Open, Edge.Wall, Edge.Wall, Edge.Open),
                 }
        },
    new Room(3, 3) { Exits = [ new(0, 0, Dir.North), new (2, 2, Dir.South)],
      Floors = { [(0,0)] = new Tile(Floor.Stone, Ceiling.Stone, Feature.None, Edge.StoneDoor, Edge.Open, Edge.Open, Edge.Wall),
                 [(1,0)] = new Tile(Floor.Stone, Ceiling.Stone, Feature.None, Edge.Wall, Edge.Open, Edge.Open, Edge.Open),
                 [(2,0)] = new Tile(Floor.Stone, Ceiling.Stone, Feature.None, Edge.Wall, Edge.Open, Edge.Wall, Edge.Open),

                 [(0,1)] = new Tile(Floor.Stone, Ceiling.Stone, Feature.None, Edge.Open, Edge.Open, Edge.Open, Edge.Wall),
                 [(1,1)] = new Tile(Floor.Stone, Ceiling.Stone, Feature.None, Edge.Open, Edge.Open, Edge.Open, Edge.Open),
                 [(2,1)] = new Tile(Floor.Stone, Ceiling.Stone, Feature.None, Edge.Open, Edge.Open, Edge.Wall, Edge.Open),

                 [(0,2)] = new Tile(Floor.Stone, Ceiling.Stone, Feature.None, Edge.Open, Edge.Wall, Edge.Open, Edge.Wall),
                 [(1,2)] = new Tile(Floor.Stone, Ceiling.Stone, Feature.None, Edge.Open, Edge.Wall, Edge.Open, Edge.Open),
                 [(2,2)] = new Tile(Floor.Stone, Ceiling.Stone, Feature.None, Edge.Open, Edge.StoneDoor, Edge.Wall, Edge.Open),
                 }
        },
    new Room(2, 3) { Exits = [ new(0, 0, Dir.West) ],
      Floors = { [(0,0)] = new Tile(Floor.Stone, Ceiling.Stone, Feature.None, Edge.Wall, Edge.Open, Edge.Open, Edge.StoneDoor),
                 [(1,0)] = new Tile(Floor.Stone, Ceiling.Stone, Feature.None, Edge.Wall, Edge.Open, Edge.Open, Edge.Open),
                 [(2,0)] = new Tile(Floor.Stone, Ceiling.Stone, Feature.None, Edge.Wall, Edge.Open, Edge.Wall, Edge.Open),
                 [(0,1)] = new Tile(Floor.Stone, Ceiling.Stone, Feature.None, Edge.Open, Edge.Wall, Edge.Open, Edge.Wall),
                 [(1,1)] = new Tile(Floor.Stone, Ceiling.Stone, Feature.None, Edge.Open, Edge.Wall, Edge.Open, Edge.Open),
                 [(2,1)] = new Tile(Floor.Stone, Ceiling.Stone, Feature.None, Edge.Open, Edge.Wall, Edge.Wall, Edge.Open) },
      },
    new Room(2, 3) { Exits = [ new(2, 1, Dir.East) ],
      Floors = { [(0,0)] = new Tile(Floor.Stone, Ceiling.Stone, Feature.None, Edge.Wall, Edge.Open, Edge.Open, Edge.Wall),
                 [(1,0)] = new Tile(Floor.Stone, Ceiling.Stone, Feature.None, Edge.Wall, Edge.Open, Edge.Open, Edge.Open),
                 [(2,0)] = new Tile(Floor.Stone, Ceiling.Stone, Feature.None, Edge.Wall, Edge.Open, Edge.Wall, Edge.Open),
                 [(0,1)] = new Tile(Floor.Stone, Ceiling.Stone, Feature.None, Edge.Open, Edge.Wall, Edge.Open, Edge.Wall),
                 [(1,1)] = new Tile(Floor.Stone, Ceiling.Stone, Feature.None, Edge.Open, Edge.Wall, Edge.Open, Edge.Open),
                 [(2,1)] = new Tile(Floor.Stone, Ceiling.Stone, Feature.None, Edge.Open, Edge.Wall, Edge.StoneDoor, Edge.Open) },
      },
    
  ];

  static List<int> DisjointNeighbours(int n, DisjointSet ds, HashSet<int> hallways)
  {
    List<int> neighbours = [];

    int other = n - DungeonLevel.MAP_SIZE;
    if (hallways.Contains(other) && ds.Find(n) != ds.Find(other))
      neighbours.Add(other);

    other = n + DungeonLevel.MAP_SIZE;
    if (hallways.Contains(other) && ds.Find(n) != ds.Find(other))
      neighbours.Add(other);

    int rowN = n / DungeonLevel.MAP_SIZE;

    other = n - 1;
    if (hallways.Contains(other) && other / DungeonLevel.MAP_SIZE == rowN && ds.Find(n) != ds.Find(other))
      neighbours.Add(other);

    other = n + 1;
    if (hallways.Contains(other) && other / DungeonLevel.MAP_SIZE == rowN && ds.Find(n) != ds.Find(other))
      neighbours.Add(other);

    return neighbours;
  }

  static bool AnyDisjointNeighbours(int n, DisjointSet ds, HashSet<int> hallways)
  {
    int other = n - DungeonLevel.MAP_SIZE;
    if (hallways.Contains(other) && ds.Find(n) != ds.Find(other))
      return true;

    other = n + DungeonLevel.MAP_SIZE;
    if (hallways.Contains(other) && ds.Find(n) != ds.Find(other))
      return true;

    int rowN = n / DungeonLevel.MAP_SIZE;

    other = n - 1;
    if (hallways.Contains(other) && other / DungeonLevel.MAP_SIZE == rowN && ds.Find(n) != ds.Find(other))
      return true;

    other = n + 1;
    if (hallways.Contains(other) && other / DungeonLevel.MAP_SIZE == rowN && ds.Find(n) != ds.Find(other))
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

  static void MazifyPass(DungeonLevel lvl, List<int> nodes, DisjointSet ds, HashSet<int> hallways, Rng rng)
  {
    nodes.Shuffle(rng);
    foreach (int n in nodes)
    {
      var neighbours = DisjointNeighbours(n, ds, hallways);
      if (neighbours.Count > 0)
      {
        int neighbour = neighbours[rng.Next(neighbours.Count)];
        CarveWall(lvl, n, neighbour);
        ds.Union(n, neighbour);
      }
    }
  }

  static void GetDisjointNodes(List<int> nodes, DisjointSet ds, HashSet<int> hallways)
  {
    nodes.Clear();
    for (int j = 0; j < MATRIX_SIZE; j++)
    {
      if (!hallways.Contains(j))
        continue;
      if (AnyDisjointNeighbours(j, ds, hallways))
        nodes.Add(j);
    }
  }

  static void Mazify(DungeonLevel lvl, Rng rng)
  {
    // By hallways, I mean all tiles that aren't in "official" rooms
    HashSet<int> hallways = [];
    for (int j = 0; j < MATRIX_SIZE; j++)
      hallways.Add(j);

    // Place some rooms
    List<Room> rooms = [];
    HashSet<(int, int)> roomSqs = [];
    for (int j = 0; j < 10; j++)
    {
      var template = _roomTemplates[rng.Next(_roomTemplates.Count)];
      Room? r = PickRoom(template, roomSqs, hallways, rng);
      if (r is not null)
        rooms.Add(r);
    }

    DisjointSet ds = new(MATRIX_SIZE);
    List<int> nodes = [];
    GetDisjointNodes(nodes, ds, hallways);

    do
    {
      MazifyPass(lvl, nodes, ds, hallways, rng);
      GetDisjointNodes(nodes, ds, hallways);
    }
    while (nodes.Count > 0);

    // Draw the rooms
    foreach (Room room in rooms)
    {
      foreach (var k in room.Floors.Keys)
      {
        lvl.SetTile(k.X, k.Y, room.Floors[k]);
      }
    }

    // We do the doors in a separate loop so that we don't overwrite
    // doors when rooms abut each other
    foreach (Room room in rooms)
    {
      foreach (var exit in room.Exits)
      {
        var (x, y, dir) = exit;
        var (dx, dy) = dir.Delta();
        var other = lvl.TileAt(x + dx, y + dy);
        var tweaked = dir switch
        {
          Dir.North => other with { EdgeS = Edge.StoneDoor },
          Dir.South => other with { EdgeN = Edge.StoneDoor },
          Dir.East => other with { EdgeW = Edge.StoneDoor },
          _ => other with { EdgeE = Edge.StoneDoor }
        };
        lvl.SetTile(x + dx, y + dy, tweaked);
      }
    }

    // Make a handful of closets (1x1 rooms)
    List<(int X, int Y, Tile Tile)> closets = [];
    for (int x = 0; x < DungeonLevel.MAP_SIZE; x++)
    {
      for (int y = 0; y < DungeonLevel.MAP_SIZE; y++)
      {
        Tile tile = lvl.TileAt(x, y);
        if (IsCloset(tile))
        closets.Add((x, y, tile));
      }
    }
    int n = int.Min(5, closets.Count);
    if (n > 0)
    {
      int numClosets = rng.Next(n);
      for (int j = 0; j < numClosets; j++)
      {
        int i = rng.Next(closets.Count);
        var closet = closets[i];
        MakeCloset(closet.X, closet.Y, closet.Tile, lvl);
        closets.RemoveAt(i);
      }
    }
  
    static void MakeCloset(int x, int y, Tile tile, DungeonLevel lvl)
    {
      if (tile.EdgeE == Edge.Open) {
        Tile tweaked = tile with { EdgeE = Edge.StoneDoor };
        Tile other = lvl.TileAt(x + 1, y);
        other = other with { EdgeW = Edge.StoneDoor };
        lvl.SetTile(x, y, tweaked);
        lvl.SetTile(x + 1, y, other);
      }
      else if (tile.EdgeW == Edge.Open) {
        Tile tweaked = tile with { EdgeW = Edge.StoneDoor };
        Tile other = lvl.TileAt(x - 1, y);
        other = other with { EdgeE = Edge.StoneDoor };
        lvl.SetTile(x, y, tweaked);
        lvl.SetTile(x - 1, y, other);
      }
      else if (tile.EdgeN == Edge.Open) {
        Tile tweaked = tile with { EdgeN = Edge.StoneDoor };
        Tile other = lvl.TileAt(x, y - 1);
        other = other with { EdgeS = Edge.StoneDoor };
        lvl.SetTile(x, y, tweaked);
        lvl.SetTile(x, y - 1, other);
      }
      else if (tile.EdgeS == Edge.Open) {
        Tile tweaked = tile with { EdgeS = Edge.StoneDoor };
        Tile other = lvl.TileAt(x, y + 1);
        other = other with { EdgeN = Edge.StoneDoor };
        lvl.SetTile(x, y, tweaked);
        lvl.SetTile(x, y + 1, other);
      }
    }

    static bool IsCloset(Tile tile)
    {
      int walls = 0;
      int open = 0;

      if (tile.EdgeE == Edge.Wall) ++walls;
      if (tile.EdgeE == Edge.Open) ++open;
      if (tile.EdgeW == Edge.Wall) ++walls;
      if (tile.EdgeW == Edge.Open) ++open;
      if (tile.EdgeN == Edge.Wall) ++walls;
      if (tile.EdgeN == Edge.Open) ++open;
      if (tile.EdgeS == Edge.Wall) ++walls;
      if (tile.EdgeS == Edge.Open) ++open;

      return walls == 3 && open == 1;
    }
  }

  static Room? PickRoom(Room template, HashSet<(int, int)> roomSqs, HashSet<int> hallways, Rng rng)
  {
    int roomX, roomY;
    int tries = 0;

    // Try to find a place for the room
    while (true)
    {
      ++tries;

      roomX = rng.Next(0, DungeonLevel.MAP_SIZE - template.W);
      roomY = rng.Next(0, DungeonLevel.MAP_SIZE - template.H);

      // make sure we don't overlap with any other rooms
      bool overlap = false;
      foreach (var k in template.Floors.Keys)
      {
        int actualX = roomX + k.X;
        int actualY = roomY + k.Y;

        if (roomSqs.Contains((actualX, actualY)))
        {
          overlap = true;
          break;
        }
      }

      if (!overlap)
        break;

      if (tries > 25)
        return null;
    }

    Room room = new(template.H, template.W);
    foreach (var k in template.Floors.Keys)
    {
      int actualX = roomX + k.X;
      int actualY = roomY + k.Y;
      room.Floors[(actualX, actualY)] = template.Floors[k];
      roomSqs.Add((actualX, actualY));

      hallways.Remove(actualY * DungeonLevel.MAP_SIZE + actualX);
    }

    foreach (var exit in template.Exits)
      room.Exits.Add(exit with { X = exit.X + roomX, Y = exit.Y + roomY });

    return room;
  }

  static DungeonLevel GenLevel()
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

    Mazify(lvl, rng);

    return lvl;
  }

  public static void GenMainDungeon(GameState gs)
  {
    List<DungeonLevel> levels = [];

    for (int j = 0; j < NUM_LEVELS; j++)
    {
      levels.Add(GenLevel());
    }

    var arrivalTile = levels[0].TileAt(0, 0);
    levels[0].SetTile(0, 0, arrivalTile with { Feature = Feature.Special8 });
    gs.Regions["dungeon"] = levels;
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