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

    var items = character.Inventory.OfType<InvSlot>();
    if (!items.Any())
    {
      lines.Add($"{character.Name} is empty handed.");
    }
    else
    {
      foreach (var i in items)
      {
        var item = ItemCatalog.Item(i.Id);
        lines.Add(item.Name);
      }
      var equipped = character.Inventory.Where(i => i is not null);
    }

    ui.DrawPicture("");
    ui.DrawTextPanel(lines, highlight);
    ui.DrawRoster(app.GameState!.Party);
  }
}