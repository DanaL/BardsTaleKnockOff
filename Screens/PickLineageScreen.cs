using SDL;

namespace BardsTaleKnockOff;

class PickLineageScreen(App app, string name) : IScreen
{
  string _name = name;

  public void OnKey(SDL_KeyboardEvent key)
  {
    Lineage lineage = Lineage.Human;

    switch (key.key)
    {
      case SDL_Keycode.SDLK_1:
        lineage = Lineage.Human;
        break;
      case SDL_Keycode.SDLK_2:
        lineage = Lineage.Orc;
        break;
      case SDL_Keycode.SDLK_3:
        lineage = Lineage.Dwarf;
        break;
      case SDL_Keycode.SDLK_4:
        lineage = Lineage.Gnome;
        break;
      case SDL_Keycode.SDLK_5:
        lineage = Lineage.Elf;
        break;
      case SDL_Keycode.SDLK_6:
        lineage = Lineage.Goblin;
        break;
      case SDL_Keycode.SDLK_ESCAPE:
        while (app.Screens.Top is not GuildScreen)
          app.Screens.Pop();
        return;
    }

    app.Screens.Push(new PickRoleScreen(app, _name, lineage));
  }

  public void Draw(GameUI ui)
  {
    List<string> lines = [ "Create a character", "" ];

    lines.AddRange(_name, "", "Choose their lineage:");
    lines.Add("1) Human");
    lines.Add("2) Orc");
    lines.Add("3) Dwarf");
    lines.Add("4) Gnome");
    lines.Add("5) Elf");
    lines.Add("6) Goblin");

    lines.AddRange("", "Esc to cancel.");
   
    ui.DrawPicture("Adventurers' Guild");
    ui.DrawTextPanel(lines);
    ui.DrawRoster(app.Game?.Party);
  }
}