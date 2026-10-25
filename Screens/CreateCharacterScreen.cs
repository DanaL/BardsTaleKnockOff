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

// Pushed on top of the Guild. For now it only asks for a name and makes a
// placeholder character; race, class and rolling stats would go here as
// further steps (each could be a mode of this screen, like the Guild's, or
// its own screen pushed in turn).
//
// Shows how typing works: text input is switched on while this screen is
// open, letters arrive through OnText, and only the editing keys (Enter,
// Backspace, Esc) are handled in OnKey.
class CreateCharacterScreen(App app) : IScreen
{
  const int MAX_NAME_LENGTH = 15;
  const int DONE = 4;

  string _name = "";
  string _status = "";

  public void OnEnter() => app.StartTextInput();
  public void OnExit() => app.StopTextInput();

  public void OnText(string text)
  {    
    foreach (char ch in text)
    {
      if (_name.Length < MAX_NAME_LENGTH && (char.IsLetterOrDigit(ch) || ch is ' ' or '\'' or '-'))
        _name += ch;
    }

    _status = "";
  }

  public void OnKey(SDL_KeyboardEvent key)
  {
    switch (key.key)
    {
      case SDL_Keycode.SDLK_BACKSPACE:
        if (_name.Length > 0)
          _name = _name[..^1];
        break;
      case SDL_Keycode.SDLK_RETURN:
      case SDL_Keycode.SDLK_KP_ENTER:
        // need to verify name
        app.Screens.Push(new PickLineageScreen(app, _name));
        break;
      case SDL_Keycode.SDLK_ESCAPE:
        while (app.Screens.Top is not GuildScreen)
          app.Screens.Pop();
        break;
    }
  }

  void Finish()
  {
    var name = _name.Trim();
    if (name.Length == 0)
    {
      _status = "Every adventurer needs a name.";
      return;
    }
    if (app.Roster.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
    {
      _status = $"There's already a {name} on file.";
      return;
    }
    
    app.Screens.Pop();
  }

  public void Draw(GameUI ui)
  {
    List<string> lines = [ "Create a character", "", "Name your character:", $"  {_name}_", "", "Esc to cancel." ];
    if (_status.Length > 0)
      lines.AddRange(["", _status]);

    ui.DrawPicture("Adventurers' Guild");
    ui.DrawTextPanel(lines);
    ui.DrawRoster(app.GameState?.Party);
  }
}
