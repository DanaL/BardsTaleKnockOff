using SDL;

namespace BardsTaleKnockOff;

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
