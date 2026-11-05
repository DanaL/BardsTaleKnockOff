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

class GameState
{
  public List<Dungeon> Maps { get; set; } = [];
  public Party Party { get; set; } = new Party();

  public int CurrLevel { get; set; }
  public int X { get; set; }
  public int Y { get; set; }
  public Dir Facing { get; set; } = Dir.North;
  public int ViewRange { get; set; } = 2;
  public ulong Turn { get; set; } = 0;

  public Dungeon CurrMap => Maps[CurrLevel];
  public Tile CurrTile => Maps[CurrLevel].TileAt(X, Y);
  
  // Text for the info panel, oldest first. The panel shows as many of the
  // most recent lines as fit.
  public List<string> Messages { get; } = [];

  const int MaxMessages = 50;

  // I made life difficult for myself by deciding that Turn 0 of the game is 
  // 8:00am T_T One turn is one minute.
  const int TURNS_PER_HOUR = 60;                  
  const ulong TURNS_PER_DAY = TURNS_PER_HOUR * 24;
  const ulong START_OFFSET = 8 * TURNS_PER_HOUR; // Turn 0 is 8:00am

  public Dictionary<string, Fact> FactDb { get; set; } = [];

  public (int, int) CurrTime()
  {
    int normalized = (int)((Turn + START_OFFSET) % TURNS_PER_DAY);
    int hour = normalized / TURNS_PER_HOUR;
    int minute = normalized % TURNS_PER_HOUR;

    return (hour, minute);
  }

  public bool DayTime => CurrTime().Item1 >= 6 && CurrTime().Item1 < 19;

  public void Say(string message)
  {
    Messages.Add(message);
    if (Messages.Count > MaxMessages)
      Messages.RemoveAt(0);
  }

  public void TryMove(Dir dir)
  {
    if (!Maps[CurrLevel].Passable(X, Y, dir))
    {
      Say("Ouch! You can't go that way.");
      return;
    }
    var (dx, dy) = dir.Delta();
    X = Dungeon.Wrap(X + dx);
    Y = Dungeon.Wrap(Y + dy);
  }
}
