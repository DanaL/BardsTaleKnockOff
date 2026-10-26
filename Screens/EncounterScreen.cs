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

using SDL;

namespace BardsTaleKnockOff;

class EncounterScreen : IScreen
{
  readonly App _app;

  // Not actually sure what your chances of successfully fleeing in BT1
  // but it worked most of the time, iirc
  const int ODDS_TO_FLEE = 75;

  const int PRELUDE = 0;
  const int PARTY_SURPRISED = 1;
  const int MONSTERS_SURPRISED = 2;
  const int FAILED_TO_FLEE = 3;
  const int FLED = 4;

  int _state = PRELUDE;
  
  readonly List<List<Monster>> _groups = [];
  readonly string _blurb;

  public EncounterScreen(App app)
  {
    _app = app;

    _groups.Add(MonsterCatalog.GroupForLevel(app.GameState.CurrLevel, app.Rng));
    if (app.GameState.CurrLevel == 0 && app.Rng.Next(4) == 0)
      _groups.Add(MonsterCatalog.GroupForLevel(app.GameState.CurrLevel, app.Rng));

     List<string> summaries = [.. _groups.Select(g => $"{g.Count} {(g.Count == 1 ? g[0].Name : g[0].Name.Pluralize())}")];
    string desc = summaries.Count switch
    {
      0 => "",
      1 => summaries[0],
      2 => $"{summaries[0]} and {summaries[1]}",
      _ => string.Join(", ", summaries.Take(summaries.Count - 1)) + ", and " + summaries[^1]
    };

    _blurb = _app.Rng.Next(4) switch
    {
      0 => $"You find yourself face-to-face with {desc}!",
      1 => $"The sudden scream of battle brings your party to a half. You are confront {desc}!",
      2 => $"You curse as monsters charge you. You face {desc}!",
      _ => $"The denizens of this foul place assault you! You face {desc}!"
    };
  }

  public void OnKey(SDL_KeyboardEvent key)
  {    
    if (_state == PRELUDE && key.scancode == SDL_Scancode.SDL_SCANCODE_R)
    {
      // Maybe eventually items or having particular classes in your party
      // or some items will increase your chance to flee?
      if (_app.Rng.Next(100) < ODDS_TO_FLEE)
      {
        _state = FLED;
      }
      else
      {
        _state = FAILED_TO_FLEE;
      }
    }
    else if (_state == FLED)
    {
      _app.Screens.Pop();
    }
  }

  public void Draw(GameUI ui)
  {
    List<string> lines = [ _blurb, ""];

    if (_state == PRELUDE)
      lines.Add("Will your stalwart band elect to (F)ight for (R)un?");
    else if (_state == FAILED_TO_FLEE)
      lines.AddRange("You were unable to escape!", "", "Press any key to continue");
    else if (_state == FLED)
      lines.AddRange("Discretion is the better part of valour...", "", "Press any key to continue");

    string image = _groups[0][0].Image;
    string monsterName = _groups[0][0].Name;

    ui.DrawImage(image, monsterName);
    ui.DrawTextPanel(lines);
    ui.DrawRoster(_app.GameState.Party);
  }
}