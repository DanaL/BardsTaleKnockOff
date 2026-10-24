// {BardsTaleKnockoff} - A roguelike computer RPG
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

class MainMenuScreen(App app) : IScreen
{
  // Extra lines shown under the menu, e.g. "load isn't written yet" or
  // warnings about the command line.
  readonly List<string> _notes = [];

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
      "L) Load saved game",
      "C) Credits",
      "Q) Quit",
      "",
      .. _notes
    ]);
    ui.DrawRoster(null);
  }
}
