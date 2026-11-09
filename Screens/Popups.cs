// Some Other Tale - A computer RPG
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

class PopupScreen(App app, string text) : IScreen
{
  public bool IsOverlay => true;

  public void OnKey(SDL_KeyboardEvent key) => app.Screens.Pop();
  public void Draw(GameUI ui) => ui.DrawPrompt([text]);
}

// A yes/no question drawn on top of the current screen. Y runs onYes; N or
// Esc just closes the question.
class ConfirmScreen(App app, string question, Action onYes) : IScreen
{
  public bool IsOverlay => true;

  public void OnKey(SDL_KeyboardEvent key)
  {
    switch (key.key)
    {
      case SDL_Keycode.SDLK_Y:
        app.Screens.Pop();
        onYes();
        break;
      case SDL_Keycode.SDLK_N:
      case SDL_Keycode.SDLK_ESCAPE:
        app.Screens.Pop();
        break;
    }
  }

  public void Draw(GameUI ui) => ui.DrawPrompt([question, "", "Y) Yes    N) No"]);
}
