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

using SDL;
using static SDL.SDL3;

namespace BardsTaleKnockOff;

// Settings for starting a new adventure, mostly from the command line.
class NewGameOptions
{
  public int Seed { get; set; } = Environment.TickCount;
  public int X { get; set; } = 0;
  public int Y { get; set; } = 1;
  public Dir Facing { get; set; } = Dir.North;
  public bool Pillars { get; set; }
  public bool Grove { get; set; }
  public LevelStyle Style { get; set; } = LevelStyle.Dungeon;
  public int? ViewRange { get; set; }
  public bool ShowMap { get; set; }
}

// Everything that lives for the whole run of the program, as opposed to one
// screen. Every screen gets a reference to this.
unsafe class App
{
  readonly SDL_Window* _window;

  public App(SDL_Window* window, NewGameOptions options)
  {
    _window = window;
    Options = options;
  }

  public ScreenStack Screens { get; } = new();
  public NewGameOptions Options { get; }

  // Characters on file at the Adventurers' Guild. Like Bard's Tale's
  // character disk, these exist independently of any one adventure; the
  // party is chosen from them.
  public List<Character> Roster { get; } = [];

  // The adventure in progress, or null before one has begun.
  public GameState? Game { get; set; }

  public Rng Rng { get; set; } = new Rng(DateTime.UtcNow.GetHashCode());

  public void NewGame()
  {
    var o = Options;
    Game = new GameState
    {
      Map = o.Pillars ? MazeBuilder.Pillars()
        : o.Grove ? MazeBuilder.Grove(o.Seed)
        : MazeBuilder.Build(o.Seed),
      Style = o.Style,
      Party = new Party(),
      X = o.X,
      Y = o.Y,
      Facing = o.Facing,
      ViewRange = o.ViewRange ?? o.Style.DefaultViewRange,
      ShowMap = o.ShowMap
    };
    Console.WriteLine($"Seed {o.Seed}");
  }

  // Switch typed-character events (IScreen.OnText) on or off. Leave it off
  // except while the player is typing, or every key press also produces text.
  public void StartTextInput() => SDL_StartTextInput(_window);
  public void StopTextInput() => SDL_StopTextInput(_window);
}
