namespace BardsTaleKnockOff;

class GameState
{
  public required Dungeon Map { get; set; }
  public required LevelStyle Style { get; set; }
  public required Party Party { get; set; }

  public int X { get; set; }
  public int Y { get; set; }
  public Dir Facing { get; set; } = Dir.North;
  public int ViewRange { get; set; } = 2;

  // Show the debug overhead map in the text panel instead of messages.
  public bool ShowMap { get; set; }

  // Text for the info panel, oldest first. The panel shows as many of the
  // most recent lines as fit.
  public List<string> Messages { get; } = [];

  const int MaxMessages = 50;

  public void Say(string message)
  {
    Messages.Add(message);
    if (Messages.Count > MaxMessages)
      Messages.RemoveAt(0);
  }

  public void TryMove(Dir dir)
  {
    if (!Map.CanPass(X, Y, dir))
    {
      Say("Ouch! You can't go that way.");
      return;
    }
    var (dx, dy) = dir.Delta();
    X = Dungeon.Wrap(X + dx);
    Y = Dungeon.Wrap(Y + dy);
  }
}
