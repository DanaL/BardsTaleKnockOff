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

namespace BardsTaleKnockOff;

class TPKScreen : IScreen
{
  readonly App _app;

  public TPKScreen(App app)
  {
    _app = app;  

    // This is where the code will go to delete dead characters and/or
    // create a morgue entry so their remains can be found by future 
    // adventurers but I'm not going to implement that at this point in 
    // dev/testing
  }

  public void OnKey(SDL_KeyboardEvent key)
  {
    _app.GameState.Messages.Clear();
    _app.GameState.CurrRegion = "town";
    _app.Screens.Replace(new GuildScreen(_app));
  }

  public void Draw(GameUI ui)
  {
    List<string> lines = [ "Sadly, you have fallen to foul beasts and ill luck. Will you be remembered? Will stories of your deeds be sung in taverns?" ];

    if (_app.GameState.CurrRegion != "town")
      lines.AddRange("", "Your remains will lay mouldering in the depths, or will they...");
    
    lines.AddRange( "",  " - press any key to continue - ");

    ui.DrawNoBackground("Oh noes :(");
    ui.DrawTextPanel(lines);
    ui.DrawRoster(_app.GameState);
  }
}