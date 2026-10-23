using SDL;

namespace BardsTaleKnockOff;

class MainMenuScreen(App app, IEnumerable<string>? notes = null) : IScreen
{
  // Extra lines shown under the menu, e.g. "load isn't written yet" or
  // warnings about the command line.
  readonly List<string> _notes = [.. notes ?? []];

  public void OnKey(SDL_KeyboardEvent key)
  {
    switch (key.key)
    {
      case SDL_Keycode.SDLK_B:
        app.NewGame();
        app.Screens.Replace(new GuildScreen(app));
        break;
      case SDL_Keycode.SDLK_L:
        _notes.Clear();
        _notes.Add("Loading saved games isn't written yet.");
        break;
      case SDL_Keycode.SDLK_C:
        app.Screens.Push(new CreditsScreen(app));
        break;
      case SDL_Keycode.SDLK_Q:
        app.Screens.Clear();
        break;
    }
  }

  public void Draw(GameUI ui)
  {
    ui.DrawPicture("Bard's Tale Knock-Off");
    ui.DrawTextPanel([
      "Welcome to Dana's Bard's Tale Knockoff!",
      "",
      "B) Begin new Adventure!",
      " - you will be taken to the Adventurers' Guild where you can create new characters.",
      "",
      "L) Load saved game",
      "C) Credits",
      "Q) Quit",
      "",
      .. _notes
    ]);
    ui.DrawRoster(null);
  }
}
