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

class CharacterScreen(App app, int slot) : IScreen
{
  public void OnKey(SDL_KeyboardEvent key)
  {
    switch (key.key)
    {
      case SDL_Keycode.SDLK_1:
      case SDL_Keycode.SDLK_2:
      case SDL_Keycode.SDLK_3:
      case SDL_Keycode.SDLK_4:
      case SDL_Keycode.SDLK_5:
      case SDL_Keycode.SDLK_6:
        int otherSlot = (int) (key.key - SDL_Keycode.SDLK_0);
        if (otherSlot <= app.Roster.Count)
          app.Screens.Replace(new CharacterScreen(app, otherSlot));
        break;
      case SDL_Keycode.SDLK_I:
        app.Screens.Push(new InventoryScreen(app, app.Roster[slot - 1]));
        break;
      case SDL_Keycode.SDLK_ESCAPE:
        app.Screens.Pop();
        break;
    }
  }

  public void Draw(GameUI ui)
  {
    Character c = app.Roster[slot - 1];

    List<string> lines = [ $"{c.Name}, {c.Lineage} {c.Role}" ];
    lines.Add($"Str: {c.Str}".PadRight(9) + $"Dex: {c.Dex}");
    lines.Add($"Con: {c.Con}".PadRight(9) + $"Lore: {c.Lore}");
    lines.Add($"Luck: {c.Luck}");
    lines.Add($"Level: {c.Level}".PadRight(12) + $"XP: {c.XP}");
    
    string hpLine = $"HP: {c.HP} ({c.MaxHP})".PadRight(11);
    if (c.MaxMP > 0)
      hpLine += $"MP: {c.MP} ({c.MaxMP})";
    lines.Add(hpLine);

    lines.Add($"Gold: {c.Gold}");
    
    lines.AddRange("", "", "", "", "", "", "I) View inventory");

    int highlight = -1;

    ui.DrawPicture("");
    ui.DrawTextPanel(lines, highlight);
    ui.DrawRoster(app.GameState.Party);
  }
}