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

class InventoryScreen(App app, Character character) : IScreen
{
  public void OnKey(SDL_KeyboardEvent key)
  {
    switch (key.key)
    {
      case SDL_Keycode.SDLK_ESCAPE:
        app.Screens.Pop();
        break;
    }
  }

  public void Draw(GameUI ui)
  {
    int highlight = -1;
    List<string> lines = [];

    int count = character.Inventory.Count(i => i is not null);

    if (count == 0)
    {
      lines.Add($"{character.Name} is empty handed.");
    }

    ui.DrawPicture("");
    ui.DrawTextPanel(lines, highlight);
    ui.DrawRoster(app.GameState!.Party);
  }
}