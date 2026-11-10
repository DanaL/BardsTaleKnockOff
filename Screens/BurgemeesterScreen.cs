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

class BurgemeesterScreen : IScreen
{
  readonly App _app;
  List<string> _blurb = [];

  public BurgemeesterScreen(App app) 
  { 
    _app = app;

    if (!_app.GameState.FactDb.ContainsKey("MetBurgemeester"))
    {
      _app.GameState.FactDb.Add("MetBurgemeester", new FlagFact("MetBurgemeester", true));
      _blurb = [ "Ah! New adventurers! I am the Burgemeester of Sarnath and welcome all adventurers and their gold! Particularly in these benighted days. You see, not too long ago, a bunch of cultists arrived in town." , "" 
                    ,"Sure they seemed fine at first, but then they slipped into Underhalls and started stirring up trouble.", "", "Since then, monsters have been spilling out into town and it's hardly safe to walk the streets anymore.",
                    "", "Come to think of it, this is exactly the sort of thing stalwart heroes might look into.", "", "You should stop by from time-to-time and keep my appraised of your progress.", "", "E) Exit to town" ];
    }
    else
    {
      _blurb = [ "Long ago the great Archmages sealed away Arcioch deep in the Underhalls. In better times, that's a fine tourist draw that brings adventurers to own. But when it rains, it pours and now we have more monsters than we'd like.", "", "E) Exit to town" ];
    }
  }

  public void OnKey(SDL_KeyboardEvent key)
  {
    switch (key.scancode)
    {
      case SDL_Scancode.SDL_SCANCODE_ESCAPE:
      case SDL_Scancode.SDL_SCANCODE_E:
        _app.GameState.X -= 1;
        _app.GameState.Facing = _app.GameState.Facing.Opposite();
        _app.Screens.Pop();
        break;      
    }
  }

  public void Draw(GameUI ui)
  {
    ui.DrawNoBackground("Burgemeester");
    ui.DrawTextPanel(_blurb);
    ui.DrawRoster(_app.GameState);
  }
}