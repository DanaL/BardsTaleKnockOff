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

class PickRoleScreen(App app, string name, Lineage lineage) : IScreen
{  
  public void OnKey(SDL_KeyboardEvent key)
  {
    Role role = Role.Warrior;
    switch (key.key)
    {
      case SDL_Keycode.SDLK_1:
        role = Role.Warrior;
        break;
      case SDL_Keycode.SDLK_2:
        role = Role.Rogue;
        break;
      case SDL_Keycode.SDLK_3:
        role = Role.Conjurer;
        break;
      case SDL_Keycode.SDLK_4:
        role = Role.Magician;
        break;
      case SDL_Keycode.SDLK_ESCAPE:
        while (app.Screens.Top is not GuildScreen)
          app.Screens.Pop();
        return;
    }

    app.Screens.Push(new RollStatsScreen(app, name, lineage, role));
  }

  public void Draw(GameUI ui)
  {
    List<string> lines = [ "Create a character", "" ];

    string l = lineage.ToString().ToLower();
    lines.AddRange($"{name}, {l}", "", "Choose their role:");
    lines.Add("1) Warrior");
    lines.Add("2) Rogue");
    lines.Add("3) Conjurer");
    lines.Add("4) Magician");
    lines.AddRange("", "Esc to cancel.");
   
    ui.DrawPicture("Adventurers' Guild");
    ui.DrawTextPanel(lines);
    ui.DrawRoster(app.GameState?.Party);
  }
}