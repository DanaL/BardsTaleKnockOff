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

class ChoosePartyMemberScreen(App app, IPartyPickScreen src, string prompt) : IScreen
{
  public string Image { get; set; } = "";
  public string Caption { get; set; } = "";

  public void OnKey(SDL_KeyboardEvent key)
  {
    if (key.scancode == SDL_Scancode.SDL_SCANCODE_ESCAPE)
    {
      src.AbortSelection();
      app.Screens.Pop();
    }
    else
    {
      int slot = key.scancode - SDL_Scancode.SDL_SCANCODE_1;
      if (slot >= 0 && slot < app.GameState.Party.Count)
      {
        app.Screens.Pop();
        src.CharacterChosen(slot);       
      }
    }
  }

  public void Draw(GameUI ui)
  {
    if (Image != "")
      ui.DrawImage(Image, Caption, null);
    else
      ui.DrawDungeonView(app.GameState);
    ui.DrawTextPanel([$"{prompt}: {1}-{app.GameState.Party.Count}"]);
    ui.DrawRoster(app.GameState.Party);
  }
}