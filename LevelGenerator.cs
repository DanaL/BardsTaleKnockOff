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

  static List<Room> Mazify(DungeonLevel lvl, Rng rng)
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
        rooms.Add(MakeCloset(closet.X, closet.Y, closet.Tile, lvl));
        closets.RemoveAt(i);
        roomSqs.Add((closet.X, closet.Y));
      }
    }
  
    return rooms;

    static Room MakeCloset(int x, int y, Tile tile, DungeonLevel lvl)
    {
      Dir doorDir;

      if (tile.EdgeE == Edge.Open) 
      {
        Tile tweaked = tile with { EdgeE = Edge.StoneDoor };
        Tile other = lvl.TileAt(x + 1, y);
        other = other with { EdgeW = Edge.StoneDoor };
        lvl.SetTile(x, y, tweaked);
        lvl.SetTile(x + 1, y, other);
        doorDir = Dir.East;
      }
      else if (tile.EdgeW == Edge.Open) 
      {
        Tile tweaked = tile with { EdgeW = Edge.StoneDoor };
        Tile other = lvl.TileAt(x - 1, y);
        other = other with { EdgeE = Edge.StoneDoor };
        lvl.SetTile(x, y, tweaked);
        lvl.SetTile(x - 1, y, other);
        doorDir = Dir.West;
      }
      else if (tile.EdgeN == Edge.Open) 
      {
        Tile tweaked = tile with { EdgeN = Edge.StoneDoor };
        Tile other = lvl.TileAt(x, y - 1);
        other = other with { EdgeS = Edge.StoneDoor };
        lvl.SetTile(x, y, tweaked);
        lvl.SetTile(x, y - 1, other);
        doorDir = Dir.North;
      }
      else
      {
        Tile tweaked = tile with { EdgeS = Edge.StoneDoor };
        Tile other = lvl.TileAt(x, y + 1);
        other = other with { EdgeN = Edge.StoneDoor };
        lvl.SetTile(x, y, tweaked);
        lvl.SetTile(x, y + 1, other);
        doorDir = Dir.South;
      }

      Room room = new(1, 1);
      room.Floors[(x, y)] = lvl.TileAt(x, y);
      room.Exits.Add(new(x, y, doorDir));

      return room;
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

  static (List<Room>, DungeonLevel) GenLevel(Rng rng)
  {
    DungeonLevel lvl = new();
    for (int x = 0; x < DungeonLevel.MAP_SIZE; x++)
    {
      for (int y = 0; y < DungeonLevel.MAP_SIZE; y++)
      {
        lvl.SetTile(x, y, new Tile(Floor.Stone, Ceiling.Stone, Feature.None, Edge.Wall, Edge.Wall, Edge.Wall, Edge.Wall));
      }
    }

    var rooms = Mazify(lvl, rng);

    return (rooms, lvl);
  }

  public static void GenMainDungeon(GameState gs, Rng rng)
  {
    List<DungeonLevel> levels = [];

    for (int ln = 0; ln < NUM_LEVELS; ln++)
    {
      var (rooms, lvl) = GenLevel(rng);

      List<(int X, int Y)> hallwaySqs = [];
      HashSet<(int, int)> roomSqs = [];      
      foreach (Room r in rooms)
      {
        foreach (var sq in r.Floors.Keys)
          roomSqs.Add(sq);
      }
      for (int x = 0; x < DungeonLevel.MAP_SIZE; x++)
      {
        for (int y = 0; y < DungeonLevel.MAP_SIZE; y++)
        {
          var sq = (x, y);
          if (!roomSqs.Contains(sq))
            hallwaySqs.Add(sq);
        }
      }

      // Add a few random traps in the level's hallways
      int numOfTraps = rng.Next(1, 4);
      for (int k = 0; k < numOfTraps; k++)
      {
        var sq = hallwaySqs[rng.Next(hallwaySqs.Count)];
        hallwaySqs.Remove(sq);
        Tile t = lvl.TileAt(sq.X, sq.Y);
        lvl.SetTile(sq.X, sq.Y, t with { Feature = PickTrap(ln, rng) });
      }

      // Knock out a few walls
      int numToErase = rng.Next(7, 10);
      for (int k = 3; k <= numToErase; k++)
        EraseWall(lvl, hallwaySqs, roomSqs, rng);

      levels.Add(lvl);
    }

    var arrivalTile = levels[0].TileAt(0, 0);
    levels[0].SetTile(0, 0, arrivalTile with { Feature = Feature.Special8 });
    gs.Regions["dungeon"] = levels;
  }

  static void EraseWall(DungeonLevel lvl, List<(int X, int Y)> hallwaySqs, HashSet<(int, int)> roomSqs, Rng rng)
  {
    List<(int, int, Dir)> opts = [];

    // Try a bunch of times
    for (int j = 0; j < 100; j++)
    {
      opts.Clear();
      var sq = hallwaySqs[rng.Next(hallwaySqs.Count)];
      Tile tile = lvl.TileAt(sq.X, sq.Y);
      if (tile.EdgeN == Edge.Wall && !roomSqs.Contains((sq.X, sq.Y - 1)))
        opts.Add((sq.X, sq.Y - 1, Dir.North));
      if (tile.EdgeS == Edge.Wall && !roomSqs.Contains((sq.X, sq.Y + 1)))
        opts.Add((sq.X, sq.Y + 1, Dir.South));
      if (tile.EdgeE == Edge.Wall && !roomSqs.Contains((sq.X + 1, sq.Y)))
        opts.Add((sq.X + 1, sq.Y, Dir.East));
      if (tile.EdgeW == Edge.Wall && !roomSqs.Contains((sq.X - 1, sq.Y)))
        opts.Add((sq.X - 1, sq.Y, Dir.West));
      
      if (opts.Count == 0)
        continue;
            
      (int nx, int ny, Dir dir) = opts[rng.Next(opts.Count)];
      Erase(lvl, tile, sq.X, sq.Y, nx, ny, dir);
      
      break;
    }

    static void Erase(DungeonLevel lvl, Tile tile, int tx, int ty, int nx, int ny, Dir dir)
    {
      Tile other;
      switch (dir)
      {
        case Dir.North:
          tile.EdgeN = Edge.Open;
          other = lvl.TileAt(nx, ny);
          other.EdgeS = Edge.Open;
          break;
        case Dir.South:
          tile.EdgeS = Edge.Open;
          other = lvl.TileAt(nx, ny);
          other.EdgeN = Edge.Open;
          break;
        case Dir.East:
          tile.EdgeE = Edge.Open;
          other = lvl.TileAt(nx, ny);
          other.EdgeW = Edge.Open;
          break;
        default:
          tile.EdgeW = Edge.Open;
          other = lvl.TileAt(nx, ny);
          other.EdgeE = Edge.Open;
          break;
      }

      lvl.SetTile(tx, ty, tile);
      lvl.SetTile(nx, ny, other);
    }
  }

  // Eventually, deeper levels will have meaner traps like Gas Cloud, etc
  static Feature PickTrap(int level, Rng rng)
  {
    List<Feature> opts = [ Feature.Trap_Dart, Feature.Trap_Blades, Feature.Trap_PoisonedNeedles ];

    return opts[rng.Next(opts.Count)];
  }
}