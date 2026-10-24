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
        role = Role.Hunter;
        break;
      case SDL_Keycode.SDLK_4:
        role = Role.Conjurer;
        break;
      case SDL_Keycode.SDLK_5:
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
    lines.Add("3) Hunter");
    lines.Add("4) Conjurer");
    lines.Add("5) Magician");
    lines.AddRange("", "Esc to cancel.");
   
    ui.DrawPicture("Adventurers' Guild");
    ui.DrawTextPanel(lines);
    ui.DrawRoster(app.Game?.Party);
  }
}