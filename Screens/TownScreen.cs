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

namespace BardsTaleKnockOff;

// Walking around the map. This is where the main adventuring loop lives:
// exploring now; random encounters (push a combat screen, pop it when the
// fight ends), shops and so on later.
class TownScreen(App app) : IScreen
{
  GameState Game => app.Game!;

  public void OnKey(SDL_KeyboardEvent key)
  {
    // Movement uses scancodes (physical key positions) so WASD stays in the
    // same place on any keyboard layout.
    switch (key.scancode)
    {
      case SDL_Scancode.SDL_SCANCODE_UP:
      case SDL_Scancode.SDL_SCANCODE_W:
        Game.TryMove(Game.Facing);
        break;
      case SDL_Scancode.SDL_SCANCODE_DOWN:
      case SDL_Scancode.SDL_SCANCODE_S:
        Game.TryMove(Game.Facing.Opposite());
        break;
      case SDL_Scancode.SDL_SCANCODE_LEFT:
      case SDL_Scancode.SDL_SCANCODE_A:
        Game.Facing = Game.Facing.TurnLeft();
        break;
      case SDL_Scancode.SDL_SCANCODE_RIGHT:
      case SDL_Scancode.SDL_SCANCODE_D:
        Game.Facing = Game.Facing.TurnRight();
        break;
      case SDL_Scancode.SDL_SCANCODE_Q:
        Game.TryMove(Game.Facing.TurnLeft());
        break;
      case SDL_Scancode.SDL_SCANCODE_E:
        Game.TryMove(Game.Facing.TurnRight());
        break;
      case SDL_Scancode.SDL_SCANCODE_1:
      case SDL_Scancode.SDL_SCANCODE_2:
      case SDL_Scancode.SDL_SCANCODE_3:
        Game.ViewRange = key.scancode - SDL_Scancode.SDL_SCANCODE_1 + 1;
        break;
      case SDL_Scancode.SDL_SCANCODE_M:
        Game.ShowMap = !Game.ShowMap;
        break;
      case SDL_Scancode.SDL_SCANCODE_ESCAPE:
        app.Screens.Push(new ConfirmScreen(app, "Abandon this adventure and return to the main menu?", () =>
        {
          app.Game = null;
          app.Screens.Replace(new MainMenuScreen(app));
        }));
        break;
    }
  }

  public void Draw(GameUI ui)
  {
    ui.DrawDungeonView(Game);
    if (Game.ShowMap)
      ui.DrawMinimap(Game);
    else
      ui.DrawTextPanel(Game.Messages);
    ui.DrawRoster(Game.Party);
  }
}
