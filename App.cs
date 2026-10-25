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

using System.Net;
using SDL;
using static SDL.SDL3;

namespace BardsTaleKnockOff;

// Everything that lives for the whole run of the program, as opposed to one
// screen. Every screen gets a reference to this.
unsafe class App(SDL_Window* window)
{
  public ScreenStack Screens { get; } = new();

  // Characters on file at the Adventurers' Guild. Like Bard's Tale's
  // character disk, these exist independently of any one adventure; the
  // party is chosen from them.
  public List<Character> Roster { get; } = [];

  // The adventure in progress, or null before one has begun.
  public GameState? GameState { get; set; }
  public string CampaignName { get; set; } = "";
  
  public Rng Rng { get; set; } = new Rng(DateTime.UtcNow.GetHashCode());

  public void NewGame()
  {
    GameState = new GameState
    {
      Party = new Party(),
      Facing = Dir.North,
      ViewRange = 2
    };

    string townMapPath = ResourcePath.GetDataFilePath("town.txt");
    Dungeon town = MapLoader.Load(townMapPath);
    GameState.Maps.Add(town);
  }

  // Switch typed-character events (IScreen.OnText) on or off. Leave it off
  // except while the player is typing, or every key press also produces text.
  public void StartTextInput() => SDL_StartTextInput(window);
  public void StopTextInput() => SDL_StopTextInput(window);
}
