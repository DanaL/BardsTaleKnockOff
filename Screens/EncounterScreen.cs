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
    throw new NotImplementedException();
  }

  public void Draw(GameUI ui)
  {
    List<string> lines = [ _blurb ];

    string image = _groups[0][0].Image;
    string monsterName = _groups[0][0].Name;

    ui.DrawImage(image, monsterName);
    ui.DrawTextPanel(lines);
    ui.DrawRoster(_app.GameState.Party);
  }
}