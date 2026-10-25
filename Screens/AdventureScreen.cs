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

// Walking around the map. This is where the main adventuring loop lives:
// exploring now; random encounters (push a combat screen, pop it when the
// fight ends), shops and so on later.
class AdventureScreen(App app) : IScreen
{
  GameState GS => app.GameState!;

  public void OnKey(SDL_KeyboardEvent key)
  {
    // Movement uses scancodes (physical key positions) so WASD stays in the
    // same place on any keyboard layout.
    switch (key.scancode)
    {
      case SDL_Scancode.SDL_SCANCODE_UP:
      case SDL_Scancode.SDL_SCANCODE_W:
        GS.TryMove(GS.Facing);
        ResolveMove();
        break;
      case SDL_Scancode.SDL_SCANCODE_DOWN:
      case SDL_Scancode.SDL_SCANCODE_S:
        GS.Facing = GS.Facing.Opposite();
        break;
      case SDL_Scancode.SDL_SCANCODE_LEFT:
      case SDL_Scancode.SDL_SCANCODE_A:
        GS.Facing = GS.Facing.TurnLeft();
        break;
      case SDL_Scancode.SDL_SCANCODE_RIGHT:
      case SDL_Scancode.SDL_SCANCODE_D:
        GS.Facing = GS.Facing.TurnRight();
        break;
      case SDL_Scancode.SDL_SCANCODE_L:
        GS.Say($"Loc: {GS.X}, {GS.Y}");
        break;
      case SDL_Scancode.SDL_SCANCODE_1:
      case SDL_Scancode.SDL_SCANCODE_2:
      case SDL_Scancode.SDL_SCANCODE_3:
        GS.ViewRange = key.scancode - SDL_Scancode.SDL_SCANCODE_1 + 1;
        break;
      case SDL_Scancode.SDL_SCANCODE_ESCAPE:
        app.Screens.Push(new ConfirmScreen(app, "Abandon this adventure and return to the main menu?", () =>
        {
          app.GameState = null;
          app.Screens.Replace(new MainMenuScreen(app));
        }));
        break;
    }
  }

  void ResolveMove()
  {
    Tile tile = GS.CurrMap.TileAt(GS.X, GS.Y);
    if (tile.Feature == Feature.Guild)
    {
      // I think eventually visiting the guild will autosave the
      // current characters?
      app.Screens.Replace(new GuildScreen(app));
    }
    else if (tile.Feature == Feature.House)
    {
      app.Screens.Push(new HouseScreen(app));
    }
  }

  public void Draw(GameUI ui)
  {
    ui.DrawDungeonView(GS);
    ui.DrawTextPanel(GS.Messages);
    ui.DrawRoster(GS.Party);
  }
}
