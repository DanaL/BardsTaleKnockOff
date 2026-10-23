using SDL;

namespace BardsTaleKnockOff;

// Pushed on top of the main menu; any key pops back to it.
class CreditsScreen(App app) : IScreen
{
  public void OnKey(SDL_KeyboardEvent key) => app.Screens.Pop();

  public void Draw(GameUI ui)
  {
    ui.DrawPicture("Credits");
    // TODO: check the art credit against the licence file in the art pack.
    ui.DrawTextPanel([
      "Programming: Dana",
      "",
      "Dungeon and outdoor art: Clint Bellanger (Heroine Dusk), CC-BY 3.0",
      "",
      "m5x7 font: Daniel Linssen",
      "",
      "Inspired by The Bard's Tale (Interplay, 1985)",
      "",
      "",
      "Press any key."
    ]);
    ui.DrawRoster(null);
  }
}
