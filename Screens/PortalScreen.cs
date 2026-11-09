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

class PortalScreen(App app, string portal) : IScreen
{
  public void OnKey(SDL_KeyboardEvent key)
  {
    switch (key.scancode)
    {
      case SDL_Scancode.SDL_SCANCODE_N:
      case SDL_Scancode.SDL_SCANCODE_ESCAPE:
        SetExit();
        app.Screens.Pop();
        break;
      case SDL_Scancode.SDL_SCANCODE_Y:
        UsePortal();
        app.Screens.Pop();
        break;
    }
  }

  void UsePortal()
  {
    switch (portal)
    {
      case "WizTowerEntrance":
        DungeonLevel wiztower = MapLoader.Load(ResourcePath.GetDataFilePath("wiztower.txt"));
        app.GameState.CurrRegion = "wiztower";
        app.GameState.Regions["wiztower"] = [ wiztower ];

        app.GameState.X = 0;
        app.GameState.Y = 0;
        app.GameState.CurrLevel = 0;
        app.GameState.Facing = Dir.South;
        break;
      case "WizTowerExit":
        app.GameState.CurrRegion = "town";
        app.GameState.X = 1;
        app.GameState.Y = 2;
        app.GameState.CurrLevel = 0;
        app.GameState.Facing = Dir.South;
        break;
    }
  }

  void SetExit()
  {
    switch (portal)
    {
      case "WizTowerEntrance":
        app.GameState.Y += 1;
        app.GameState.Facing = app.GameState.Facing.Opposite();
        break;
    }
  }

  public void Draw(GameUI ui)
  {
    List<string> desc = [];

    switch (portal)
    {
      case "WizTowerEntrance":
        desc.AddRange("The tower interior smells of must and old burlap. Dusty stairs lead upward into darkness.", "", "Take the stairs? (y/n)");
        ui.DrawNoBackground("");
        break;
      case "WizTowerExit":
        desc.AddRange("Steps lead downward to the streets of Sarnath.", "", "Take the stairs? (y/n)");
        ui.DrawDungeonView(app.GameState);
        break;
    }
    
    ui.DrawTextPanel(desc);
    ui.DrawRoster(app.GameState);
  }
}