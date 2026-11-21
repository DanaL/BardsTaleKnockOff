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
  const int NUM_LEVELS = 4;
  const int MATRIX_SIZE = DungeonLevel.MAP_SIZE * DungeonLevel.MAP_SIZE;

  public static void GenMainDungeon(GameState gs, Rng rng)
  {
    List<DungeonLevel> levels = [];
    int arrivalStairsX = 0, arrivalStairsY = 0;

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

      // Sometimes add fixed encounters
      SetFixedEncounters(lvl, ln, rooms, rng);

      // Add a few areas of darkness
      int numDarkAreas = rng.Next(0, 4);
      for (int j = 0; j < numDarkAreas; j++)
      {
        int rx = rng.Next(DungeonLevel.MAP_SIZE);
        int ry = rng.Next(DungeonLevel.MAP_SIZE);
        MakeDarkArea(lvl, rx, ry, rng.Next(3));
      }

      // Set the arrival (up) stairs
      Feature upStairs = ln > 0 ? Feature.StairsUp : Feature.Special8;
      Tile tile = lvl.TileAt(arrivalStairsX, arrivalStairsY);
      lvl.SetTile(arrivalStairsX, arrivalStairsY, tile with { Feature = upStairs });

      // Add the stairs to the next level
      if (ln == 0)
        (arrivalStairsX, arrivalStairsY) = SetGatedStairs(lvl, 0, 0, rooms, gs, rng);
      else if (ln < NUM_LEVELS - 1)
      {          
        (arrivalStairsX, arrivalStairsY) = SetStairs(lvl, arrivalStairsX, arrivalStairsY, rng);
      }

      levels.Add(lvl);
    }

    gs.Regions["dungeon"] = levels;
  }

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

  static void BlobbedRooms(HashSet<int> hallways, List<Room> rooms, Dictionary<int, HashSet<int>> roomBlobs, Rng rng)
  {
    int half = DungeonLevel.MAP_SIZE / 2;
    List<(int X, int Y)> blobSpots = rng.Next(7) switch
    {
      // three blobs
      0 => [(rng.Next(1, half), half), (1, 1), (half, 1)],
      1 => [(rng.Next(1, half), 1), (1, half), (half, half)],
      2 => [(half, rng.Next(1, half)), (1, 1), (1, half)],
      3 => [(1, rng.Next(1, half)), (half, 1), (half, half)],
      // one in each half
      4 => [(1, rng.Next(1, 8)), (half, rng.Next(1, 8))],
      5 => [(rng.Next(1, 8), 1), (rng.Next(1, 8), half)],
      // one in each quadrant
      6 => [(1, 1), (half, 1), (1, half), (half, half)],
      // one blob roughly in the middle
      _ => [(DungeonLevel.MAP_SIZE / 4 + rng.Next(-2, 3), DungeonLevel.MAP_SIZE / 4 + rng.Next(-2, 3))]
    };

    foreach (var (offsetX, offsetY) in blobSpots)
    {
      List<Room> tweakedRooms = [];
      RoomBlob rb = new(10, 10, rng);
      var (roomTemplates, blobs) = rb.Generate();
      foreach (Room rt in roomTemplates)
      {
        Room r = new(rt.H, rt.W);
        foreach (var sq in rt.Floors.Keys)
        {
          (int X, int Y) floor = (sq.X + offsetX, sq.Y + offsetY);
          r.Floors[floor] = rt.Floors[sq];
          hallways.Remove(floor.Y * DungeonLevel.MAP_SIZE + floor.X);
        }
        foreach (Door d in rt.Exits)
        {
          r.Exits.Add(d with { X = d.X + offsetX, Y = d.Y + offsetY });
        }
        tweakedRooms.Add(r);
      }

      // Now set add at least one door on the exterior of each blob. This is 
      // the simple case. I can imagine navigation puzzles where say only one
      // blob has an exterior door and you teleport from blob to blob to get
      // to a goal
      foreach (var k in blobs.Keys)
      {
        var walls = ExteriorWalls(tweakedRooms, blobs[k], hallways);
        var (roomId, x, y, dir) = walls[rng.Next(walls.Count)];
        SetDoor(tweakedRooms, roomId, x, y, dir);

        // add a second door to a room blob sometimes
        if (rng.Next(3) == 0)
        {
          var (roomId2, x2, y2, dir2) = walls[rng.Next(walls.Count)];
          SetDoor(tweakedRooms, roomId2, x2, y2, dir2);
        }
      }

      rooms.AddRange(tweakedRooms);

      foreach (var kvp in blobs)
      {
        roomBlobs[kvp.Key] = kvp.Value;
      }
    }

    static void SetDoor(List<Room> rooms, int roomId, int x, int y, Dir dir)
    {
      // setting the tile opposite the door to also be a door is handled
      // in BlobbedRoom()'s caller
      Room r = rooms[roomId];
      r.Floors[(x, y)] = dir switch
      {
        Dir.North => r.Floors[(x, y)] with { EdgeN = Edge.StoneDoor },
        Dir.South => r.Floors[(x, y)] with { EdgeS = Edge.StoneDoor },
        Dir.East => r.Floors[(x, y)] with { EdgeE = Edge.StoneDoor },
        _ => r.Floors[(x, y)] with { EdgeW = Edge.StoneDoor },
      };
      r.Exits.Add(new(x, y, dir));
    }
  }

  static List<(int RoomId, int X, int Y, Dir Dir)> ExteriorWalls(List<Room> rooms, HashSet<int> blob, HashSet<int> hallways)
  {
    HashSet<(int RoomId, int X, int Y, Dir Dir)> walls = [];

    foreach (int roomId in blob)
    {
      Room room = rooms[roomId];
      foreach (var (floorx, floory) in room.Floors.Keys)
      {
        foreach (Dir dir in Util.Cardinals)
        {
          var (deltax, deltay) = dir.Delta();
          int otherx = floorx + deltax, othery = floory + deltay;
          if (hallways.Contains(othery * DungeonLevel.MAP_SIZE + otherx))
            walls.Add((roomId, floorx, floory, dir));
        }
      }
    }

    return [.. walls];
  }

  static void FullyConnectLevel(DungeonLevel lvl, HashSet<int> hallways, List<Room> rooms, Rng rng)
  {
    HashSet<int> roomSqs = [];
    foreach (var room in rooms)
    {
      foreach (var (X, Y) in room.Floors.Keys)
        roomSqs.Add(Y * DungeonLevel.MAP_SIZE + X);
    }

    DisjointSet ds = new(MATRIX_SIZE);
    bool[] filled = new bool[MATRIX_SIZE];

    // Hallway squares that are next to a room square, with the direction of
    // the room square. Grouped by hallway region, keyed by the region's root
    // in ds.
    Dictionary<int, List<Door>> roomAdj = [];

    List<int> halls = [.. hallways];
    halls.Shuffle(rng);

    foreach (int start in halls)
    {
      if (filled[start])
        continue;

      List<Door> adj = [];
      Queue<int> q = new();
      q.Enqueue(start);
      filled[start] = true;

      while (q.Count > 0)
      {
        int sq = q.Dequeue();
        int x = sq % DungeonLevel.MAP_SIZE, y = sq / DungeonLevel.MAP_SIZE;
        Tile tile = lvl.TileAt(x, y);

        foreach (Dir dir in Util.Cardinals)
        {
          var (dx, dy) = dir.Delta();
          int nx = DungeonLevel.Wrap(x + dx), ny = DungeonLevel.Wrap(y + dy);
          int nb = ny * DungeonLevel.MAP_SIZE + nx;

          // Note room squares whatever the edge between us is
          if (roomSqs.Contains(nb))
            adj.Add(new Door(x, y, dir));

          Edge edge = tile.EdgeForDir(dir);
          if (edge is not Edge.Open || !hallways.Contains(nb))
            continue;
          if (ds.Find(nb) == ds.Find(sq))
            continue;

          ds.Union(sq, nb);
          filled[nb] = true;
          q.Enqueue(nb);
        }
      }
      
      roomAdj[ds.Find(start)] = adj;
    }

    Dictionary<int, int> regions = [];
    foreach (var sq in hallways)
    {
      int k = ds.Find(sq);
      if (!regions.ContainsKey(k))
        regions[k] = 1;
      else
        regions[k] += 1;
    }
    List<(int Region, int Count)> enclaves = [.. regions.Select(kvp => (kvp.Key, kvp.Value))
                                                        .OrderByDescending(s => s.Value)];

    if (enclaves.Count > 1)
    {
      foreach (var (regionId, _) in enclaves.Skip(1))
      {
        var doorOpts = roomAdj[regionId];
        Door door = doorOpts[rng.Next(doorOpts.Count)];
        var (doorX, doorY, dir) = door;
        Tile tile = lvl.TileAt(door.X, door.Y);
        tile = dir switch
        {
          Dir.North => tile with { EdgeN = Edge.StoneDoor },
          Dir.South => tile with { EdgeS = Edge.StoneDoor },
          Dir.East => tile with { EdgeE = Edge.StoneDoor },
          _ => tile with { EdgeW = Edge.StoneDoor }
        };
        lvl.SetTile(doorX, doorY, tile);

        var (deltaX, deltaY) = dir.Delta();
        int otherX = doorX + deltaX, otherY = doorY + deltaY;
        tile = lvl.TileAt(otherX, otherY);
        tile = dir.Opposite() switch
        {
          Dir.North => tile with { EdgeN = Edge.StoneDoor },
          Dir.South => tile with { EdgeS = Edge.StoneDoor },
          Dir.East => tile with { EdgeE = Edge.StoneDoor },
          _ => tile with { EdgeW = Edge.StoneDoor }
        };
        lvl.SetTile(otherX, otherY, tile);
      }
    }
  }

  static List<Room> Mazify(DungeonLevel lvl, HashSet<int> hallways, List<Room> rooms, Rng rng)
  {
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

    return rooms;
  }

  static void AddClosets(DungeonLevel lvl, List<Room> rooms, Rng rng)
  {
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
      }
    }
  }

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

  // Not pathfinding, just finding the shortest paths from 
  // a particular location
  static (int Sq, int D)[] StepCounts(int x, int y, DungeonLevel lvl)
  {
    int w = DungeonLevel.MAP_SIZE;
    int[] distances = new int[MATRIX_SIZE];
    Array.Fill(distances, int.MaxValue);
    distances[y * w + x] = 0;
    Queue<(int X, int Y)> q = new();
    q.Enqueue((x, y));

    while (q.Count > 0)
    {
      (int cx, int cy) = q.Dequeue();
      int d = distances[cy * w + cx];

      // if the neighbour distance is already less than or equal to distance
      // of square we just reached, we can stop because that other path is 
      // shorter. Otherwise, set neighbour distance in the matrix to new 
      // shortest value and add neighbour to queue
      foreach (Dir dir in new[] { Dir.North, Dir.South, Dir.East, Dir.West })
      {
        if (!lvl.Passable(cx, cy, dir))
          continue;
        var (deltax, deltay) = dir.Delta();
        int nx = DungeonLevel.Wrap(cx + deltax);
        int ny = DungeonLevel.Wrap(cy + deltay);
        int n = w * ny + nx;
        int nd = distances[n];
        if (d + 1 < nd || nd == int.MaxValue)
        {
          distances[n] = d + 1;
          q.Enqueue((nx, ny));
        }
      }
    }

    (int, int)[] result = new (int, int)[MATRIX_SIZE];
    for (int j = 0; j < MATRIX_SIZE; j++)
      result[j] = (j, distances[j]);

    return result;
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

    // By hallways, I mean all tiles that aren't in "official" rooms
    HashSet<int> hallways = [];
    for (int j = 0; j < MATRIX_SIZE; j++)
      hallways.Add(j);

    for (int j = 0; j < MATRIX_SIZE; j++)
      hallways.Add(j);

    // Place some rooms
    List<Room> rooms = [];
    Dictionary<int, HashSet<int>> roomBlobs = [];

    BlobbedRooms(hallways, rooms, roomBlobs, rng);

    Mazify(lvl, hallways, rooms, rng);
    FullyConnectLevel(lvl, hallways, rooms, rng);

    AddClosets(lvl, rooms, rng);

    return (rooms, lvl);
  }

  static (int, int) SetIdolPuzzle(DungeonLevel lvl, int arrivalX, int arrivalY, List<Room> rooms, GameState gs, Rng rng)
  {
    Dictionary<int, int> roomSqs = [];
    for (int j = 0; j < rooms.Count; j++)
    {
      // We only care about rooms with one exit that we can lock up
      if (rooms[j].Exits.Count != 1)
        continue;
      foreach (var (loc, _) in rooms[j].Floors.Where(sq => sq.Value.Feature == Feature.None))
      {
        roomSqs[loc.Y * DungeonLevel.MAP_SIZE + loc.X] = j;
      }
    }

    // I think this will never happen but just in case...
    if (roomSqs.Count == 0)
      return (-1, -1);

    var distances = StepCounts(arrivalX, arrivalY, lvl);
    List<int> distantSqs = [.. distances.OrderByDescending(i => i.D)
                                        .Where(i => roomSqs.ContainsKey(i.Sq))
                                        .Take(MATRIX_SIZE / 3)
                                        .Select(i => i.Sq)];

    var idolSq = distantSqs[rng.Next(distantSqs.Count)];
    int idolX = idolSq % DungeonLevel.MAP_SIZE;
    int idolY = idolSq / DungeonLevel.MAP_SIZE;
    Tile tile = lvl.TileAt(idolX, idolY);

    return (idolX, idolY);
  }

  static (int, int) SetGatedStairs(DungeonLevel lvl, int arrivalX, int arrivalY, List<Room> rooms, GameState gs, Rng rng)
  {
    // First we need to find a room we can lock with a portcullis to place 
    // the stairs in.
    Dictionary<int, int> roomSqs = [];
    for (int j = 0; j < rooms.Count; j++)
    {
      // We only care about rooms with one exit that we can lock up
      if (rooms[j].Exits.Count != 1)
        continue;
      foreach (var (loc, _) in rooms[j].Floors.Where(sq => sq.Value.Feature == Feature.None))
      {
        roomSqs[loc.Y * DungeonLevel.MAP_SIZE + loc.X] = j;
      }
    }

    // I think this will never happen but just in case...
    if (roomSqs.Count == 0)
      return (-1, -1);

    var distances = StepCounts(arrivalX, arrivalY, lvl);
    List<int> distantSqs = [.. distances.OrderByDescending(i => i.D)
                                        .Where(i => roomSqs.ContainsKey(i.Sq))
                                        .Take(MATRIX_SIZE / 3)
                                        .Select(i => i.Sq)];

    var stairsSq = distantSqs[rng.Next(distantSqs.Count)];
    int stairsX = stairsSq % DungeonLevel.MAP_SIZE;
    int stairsY = stairsSq / DungeonLevel.MAP_SIZE;
    Tile tile = lvl.TileAt(stairsX, stairsY);
    lvl.SetTile(stairsX, stairsY, tile with { Feature = Feature.StairsDown });

    int roomId = roomSqs[stairsSq];
    Room portcullisRoom = rooms[roomId];
    var exit = portcullisRoom.Exits[0];
    Util.SetDoor(lvl, exit, Edge.Portcullis);

    var fact = new PortcullisStateFact(exit.X, exit.Y, exit.Dir, false);
    gs.FactDb[fact.Key] = fact;

    // Now place the lever
    foreach (var (X, Y) in portcullisRoom.Floors.Keys)
      roomSqs.Remove(Y * DungeonLevel.MAP_SIZE + Y);
    int leverSq = roomSqs.Keys.ElementAt(rng.Next(roomSqs.Count));
    int leverX = leverSq % DungeonLevel.MAP_SIZE;
    int leverY = leverSq / DungeonLevel.MAP_SIZE;
    Tile leverTile = lvl.TileAt(leverX, leverY) with { Feature = Feature.Lever };
    lvl.SetTile(leverX, leverY, leverTile);

    var leverFact = new LeverFact(leverX, leverY, exit.X, exit.Y);
    gs.FactDb[leverFact.Key] = leverFact;

    // We should add a complication to the lever room. An encounter within,
    // replace doors with illusory walls, etc
    Room leverRoom = rooms[roomSqs[leverSq]];
    ComplicateRoom(lvl, leverRoom, rng);

    return (stairsX, stairsY);
  }

  static void ComplicateRoom(DungeonLevel lvl, Room room, Rng rng)
  {
    // Make all the exits illusory walls
    foreach (var door in room.Exits)
    {
      Util.SetDoor(lvl, door, Edge.IllusoryWall);
    }
  }

  // BT's levels were neatly stacked, by which I mean if the stairs down were
  // at 4, 10 then the arrival stairs/stairs up on the next level would be at
  // 4, 10 as well
  static (int, int) SetStairs(DungeonLevel lvl, int arrivalX, int arrivalY, Rng rng)
  {    
    // For the stairs down, pick a tile that's further away from the 
    // stairs up
    var distances = StepCounts(arrivalX, arrivalY, lvl);
    List<int> distantSqs = [.. distances.OrderByDescending(i => i.D)
                                        .Take(MATRIX_SIZE / 3)
                                        .Select(i => i.Sq)];

    int stairsSq = distantSqs[rng.Next(distantSqs.Count)];
    int stairsX = stairsSq % DungeonLevel.MAP_SIZE;
    int stairsY = stairsSq / DungeonLevel.MAP_SIZE;
    Tile tile = lvl.TileAt(stairsX, stairsY);
    lvl.SetTile(stairsX, stairsY, tile with { Feature = Feature.StairsDown });

    return (stairsX, stairsY);
  }

  static void MakeDarkArea(DungeonLevel lvl, int x, int y, int range)
  {
    Queue<(int X, int Y)> q = new();
    q.Enqueue((x, y));
    HashSet<(int, int)> visited = [];

    while (q.Count > 0)
    {
      var sq = q.Dequeue();
      visited.Add(sq);

      Tile tile = lvl.TileAt(sq.X, sq.Y);
      if (!tile.HasFlag(TileFlags.Darkness))
        lvl.SetTile(sq.X, sq.Y, tile with { Flags = tile.Flags | TileFlags.Darkness });

      foreach (var dir in Util.Cardinals)
      {
        if (tile.EdgeForDir(dir) != Edge.Open)
          continue;
        (int deltax, int deltay) = dir.Delta();
        int nx = sq.X + deltax;
        int ny = sq.Y + deltay;
        if (visited.Contains((nx, ny)) || Util.Distance(x, y, nx, ny) > range)
          continue;
        q.Enqueue((nx, ny));
      }
    }
  }

  static void SetFixedEncounters(DungeonLevel lvl, int levelNum, List<Room> rooms, Rng rng)
  {
    int numFixed = rng.Next(10) switch
    {
      > 8 => 3,
      >= 5 and <= 7 => 2,
      >= 2 and <= 5 => 1,
      _ => 0
    };
    numFixed = int.Min(numFixed, rooms.Count);
    List<int> roomIds = [.. Enumerable.Range(0, rooms.Count)];
    roomIds.Shuffle(rng);

    for (int j = 0; j < numFixed; j++)
    {
      Room room = rooms[roomIds[j]];
      List<Door> exits = [.. room.Exits.Where(sq => lvl.TileAt(sq.X, sq.Y).Feature == Feature.None)];
      if (exits.Count == 0)
        continue;
      Door exit = exits[rng.Next(exits.Count)];
      lvl.Encounters[(exit.X, exit.Y)] = PickEnc(levelNum, rng);
      Tile t = lvl.TileAt(exit.X, exit.Y);
      lvl.SetTile(exit.X, exit.Y, t with { Feature = Feature.Encounter });
    }

    static List<Encounter> PickEnc(int levelNum, Rng rng)
    {
      List<Encounter> enc = [];
      var table = Util.MonsterTable["dungeon"][levelNum];
      Monsters m = table[rng.Next(table.Count)].Item1;

      int numGroups = rng.Next(10) switch
      {
        > 8 => 4,
        >= 7 and <= 8 => 3,
        >= 4 and <= 6 => 2,
        _ => 1
      };

      int groupSize = rng.Next(5, 10);
      int distance = 10;
      for (int j = 0; j < numGroups; j++)
      {
        enc.Add(new(m, groupSize, distance));
        if (j > 0)
          distance += 10;
      }

      return enc;
    }
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
    List<Feature> opts = [Feature.Trap_Dart, Feature.Trap_Blades, Feature.Trap_PoisonedNeedles];

    return opts[rng.Next(opts.Count)];
  }
}

record struct SharedWall(int A, int B, List<(int X, int Y)> Sqs, Dir DirFromA);
record struct Box(int X, int Y, int H, int W);
class RoomBlob(int height, int width, Rng rng)
{
  const int MAX_ROOM = 3;

  List<Box> _boxes = [];
  List<Room> _rooms = [];

  public (List<Room>, Dictionary<int, HashSet<int>>) Generate()
  {
    _boxes = Partition(0, height, 0, width, rng);

    foreach (var box in _boxes)
    {
      Room r = new(box.H, box.W);
      for (int y = box.Y; y < box.Y + box.H; y++)
      {
        for (int x = box.X; x < box.X + box.W; x++)
        {
          Edge n = y == box.Y ? Edge.Wall : Edge.Open;
          Edge s = y == box.Y + box.H - 1 ? Edge.Wall : Edge.Open;
          Edge e = x == box.X + box.W - 1 ? Edge.Wall : Edge.Open;
          Edge w = x == box.X ? Edge.Wall : Edge.Open;
          r.Floors[(x, y)] = new Tile(Floor.Stone, Ceiling.Stone, Feature.None, n, s, e, w);
        }
      }

      _rooms.Add(r);
    }

    var sharedWalls = SharedWalls();
    sharedWalls.Shuffle(rng);

    DisjointSet ds = new(_boxes.Count);
    foreach (var wall in sharedWalls)
    {
      if (ds.Find(wall.A) == ds.Find(wall.B))
        continue;
      var (x, y) = wall.Sqs[rng.Next(wall.Sqs.Count)];
      AddDoor(wall.A, wall.B, x, y, wall.DirFromA);
      ds.Union(wall.A, wall.B);
    }

    Dictionary<int, HashSet<int>> blobs = [];
    for (int j = 0; j < _rooms.Count; j++)
    {
      int id = ds.Find(j);
      if (!blobs.ContainsKey(id))
        blobs[id] = [j];
      else
        blobs[id].Add(j);
    }

    return (_rooms, blobs);
  }

  void AddDoor(int a, int b, int x, int y, Dir dirFromA)
  {
    var (dx, dy) = dirFromA.Delta();
    var roomA = _rooms[a];
    var roomB = _rooms[b];

    if (dirFromA == Dir.East)
    {
      roomA.Floors[(x, y)] = roomA.Floors[(x, y)] with { EdgeE = Edge.StoneDoor };
      roomA.Exits.Add(new Door(x, y, Dir.East));
      roomB.Floors[(x + dx, y + dy)] = roomB.Floors[(x + dx, y + dy)] with { EdgeW = Edge.StoneDoor };
      roomB.Exits.Add(new Door(x + dx, y + dy, Dir.West));
    }
    else
    {
      roomA.Floors[(x, y)] = roomA.Floors[(x, y)] with { EdgeS = Edge.StoneDoor };
      roomA.Exits.Add(new Door(x, y, Dir.South));
      roomB.Floors[(x + dx, y + dy)] = roomB.Floors[(x + dx, y + dy)] with { EdgeN = Edge.StoneDoor };
      roomB.Exits.Add(new Door(x + dx, y + dy, Dir.North));
    }
  }

  List<SharedWall> SharedWalls()
  {
    List<SharedWall> walls = [];

    for (int j = 0; j < _boxes.Count; j++)
    {
      for (int k = 0; k < _boxes.Count; k++)
      {
        if (j == k)
          continue;
        var a = _boxes[j];
        var b = _boxes[k];

        // is b directly east?
        if (a.X + a.W == b.X)
        {
          int y0 = Math.Max(a.Y, b.Y);
          int y1 = Math.Min(a.Y + a.H, b.Y + b.H);
          if (y0 < y1)
          {
            List<(int, int)> pts = [.. Enumerable.Range(y0, y1 - y0).Select(y => (a.X + a.W - 1, y))];
            walls.Add(new(j, k, pts, Dir.East));
          }
        }

        // is b directly south?
        if (a.Y + a.H == b.Y)
        {
          int x0 = Math.Max(a.X, b.X);
          int x1 = Math.Min(a.X + a.W, b.X + b.W);
          if (x0 < x1)
          {
            List<(int, int)> pts = [.. Enumerable.Range(x0, x1 - x0).Select(x => (x, a.Y + a.H - 1))];
            walls.Add(new(j, k, pts, Dir.South));
          }
        }
      }
    }

    return walls;
  }

  static List<Box> Partition(int top, int bottom, int left, int right, Rng rng)
  {
    int height = bottom - top;
    int width = right - left;

    if (height <= 1 || width <= 1)
      return [];

    bool canSplitX = width > MAX_ROOM;
    bool canSplitY = height > MAX_ROOM;
    if (!canSplitX && !canSplitY)
      return [new(X: left, Y: top, H: height, W: width)];

    List<Box> boxes = [];
    bool splitX = canSplitX && (!canSplitY || rng.Next(2) == 0);
    if (splitX)
    {
      int x = Split(left, right, rng);
      boxes.AddRange(Partition(top, bottom, left, x, rng));
      boxes.AddRange(Partition(top, bottom, x, right, rng));
    }
    else
    {
      int y = Split(top, bottom, rng);
      boxes.AddRange(Partition(top, y, left, right, rng));
      boxes.AddRange(Partition(y, bottom, left, right, rng));
    }

    return boxes;
  }

  static int Split(int a, int b, Rng rng) => int.Clamp((a + b) / 2 + rng.Next(7) - 3, a + 1, b - 1);
}