// {BardsTaleKnockoff} - A roguelike computer RPG
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
  public required Party Party { get; set; }

  public int CurrLevel { get; set; }
  public int X { get; set; }
  public int Y { get; set; }
  public Dir Facing { get; set; } = Dir.North;
  public int ViewRange { get; set; } = 2;
  public bool DayTime { get; set;} = true; // eventually to be replaced with time of day

  public Dungeon CurrMap => Maps[CurrLevel];

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
