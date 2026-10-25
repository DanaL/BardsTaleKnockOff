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

// The Adventurers' Guild: create characters, choose the party from the
// characters on file, then leave for the town.
class HouseScreen(App app) : IScreen
{
  public void OnKey(SDL_KeyboardEvent key)
  {
    switch (key.key)
    {
      case SDL_Keycode.SDLK_E:
        app.GameState!.Facing = app.GameState.Facing.Opposite();
        var (dx, dy) = app.GameState.Facing.Delta();
        app.GameState.X += dx;
        app.GameState.Y += dy;
        app.Screens.Pop();
        break;
    }
  }

  public void Draw(GameUI ui)
  {
    var lines = new List<string> { "You are in an empty building.", "", "E) Exit" };
    int highlight = -1;

    ui.DrawImage("empty building.png", "Empty building");
    ui.DrawTextPanel(lines, highlight);
    //ui.DrawRoster(Party);
  }
}