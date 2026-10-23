namespace BardsTaleKnockOff;

// Placeholder level generator so there is something to walk around while the
// renderer is being built: a recursive-backtracker maze over the (wrapping)
// grid, with some extra walls knocked out to make loops and a few doors.
static class MazeBuilder
{
  public static Dungeon Build(int seed)
  {
    var rng = new Random(seed);
    var map = new Dungeon();
    map.Fill(Edge.Wall);

    var visited = new bool[Dungeon.Size, Dungeon.Size];
    var stack = new Stack<(int x, int y)>();
    stack.Push((0, 0));
    visited[0, 0] = true;

    while (stack.Count > 0)
    {
      var (x, y) = stack.Peek();
      var options = new List<Dir>();
      foreach (Dir d in Enum.GetValues<Dir>())
      {
        var (dx, dy) = d.Delta();
        if (!visited[Dungeon.Wrap(x + dx), Dungeon.Wrap(y + dy)])
          options.Add(d);
      }

      if (options.Count == 0)
      {
        stack.Pop();
        continue;
      }

      var dir = options[rng.Next(options.Count)];
      var (ddx, ddy) = dir.Delta();
      int nx = Dungeon.Wrap(x + ddx), ny = Dungeon.Wrap(y + ddy);
      map.SetEdge(x, y, dir, rng.NextDouble() < 0.08 ? Edge.Door : Edge.Open);
      visited[nx, ny] = true;
      stack.Push((nx, ny));
    }

    // A perfect maze has exactly one route between any two squares, which is
    // tedious to explore. Remove some walls to create loops and open areas.
    for (int i = 0; i < Dungeon.Size * Dungeon.Size / 6; i++)
    {
      int x = rng.Next(Dungeon.Size), y = rng.Next(Dungeon.Size);
      var dir = (Dir)rng.Next(4);
      if (map.GetEdge(x, y, dir) == Edge.Wall)
        map.SetEdge(x, y, dir, Edge.Open);
    }

    return map;
  }

  // Test map: an open field with trees scattered over about a third of it.
  public static Dungeon Grove(int seed)
  {
    var rng = new Random(seed);
    var map = new Dungeon();
    map.Fill(Edge.Open);
    for (int x = 0; x < Dungeon.Size; x++)
      for (int y = 0; y < Dungeon.Size; y++)
        if ((x, y) != (0, 0) && rng.NextDouble() < 0.3)
          map.SetFeature(x, y, Feature.Tree);
    return map;
  }

  // Test map: an open hall with a 1x1 pillar on every square whose coordinates
  // are both even. Pillars are exactly what the art was drawn for (solid
  // blocks), so this shows whether every wall slot lines up.
  public static Dungeon Pillars()
  {
    var map = new Dungeon();
    map.Fill(Edge.Open);
    for (int x = 0; x < Dungeon.Size - 1; x += 2)
      for (int y = 0; y < Dungeon.Size - 1; y += 2)
        foreach (Dir d in Enum.GetValues<Dir>())
          map.SetEdge(x, y, d, Edge.Wall);
    return map;
  }
}
