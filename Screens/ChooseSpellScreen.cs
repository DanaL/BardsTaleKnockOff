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

class ChooseSpellScreen(App app, Character c, bool combatSpells, ICastSpellScreen src) : IScreen
{
  public string Image { get; set; } = "";
  public string Caption { get; set; } = "";

  List<SpellInfo> _spells = [];
  int _cursor = 0;
  int _offset = 0;
  int _selection = 0;
  string _filter = "";
  const int _displaySize = 18;

  public void OnEnter() => app.StartTextInput();
  public void OnExit() => app.StopTextInput();

  public void OnText(string text)
  {    
    foreach (char ch in text)
    {
      if (char.IsLetter(ch))
        _filter += (_filter + ch).ToUpper();
    }

    _cursor = 0;
    _selection = 0;
  }

  public void OnKey(SDL_KeyboardEvent key)
  {    
    switch (key.key)
    {
      case SDL_Keycode.SDLK_UP:
        MoveCursor(-1);
        break;
      case SDL_Keycode.SDLK_DOWN:
        MoveCursor(1);
        break;
      case SDL_Keycode.SDLK_BACKSPACE:
        if (_filter.Length > 0)
          _filter = _filter[..^1];
        break;
      case SDL_Keycode.SDLK_RETURN:
      case SDL_Keycode.SDLK_KP_ENTER:
        if (_spells.Count > 0)
        {
          app.Screens.Pop();
          src.SpellChosen(_spells[_selection]);
        }
        break;
      case SDL_Keycode.SDLK_ESCAPE:
        if (_filter.Length > 0)
          _filter = "";
        else
        {
          src.AbortSelection();
          app.Screens.Pop();
        }
        break;
    }
  }

  void MoveCursor(int delta)
  {
    _cursor += delta;
    _selection += delta;
    
    if (_cursor < 0) 
    {
      _cursor = 0;
      _offset = int.Max(0, _offset - 1);
      _selection = 0;
    }
    
    if (_selection >= _spells.Count)
    {
      _cursor -= delta;
      _selection -= delta;

      return;  
    }

    int maxRow = int.Min(_displaySize, _spells.Count);
    if (_cursor >= maxRow) 
    {
      _cursor = maxRow - 1;
      _offset += 1;    
    }
  }

  public void Draw(GameUI ui)
  {
    List<string> lines = [ "Cast which: ", "" ];

    _spells = [.. Spells.SpellsFor(c).Where(s => s.Combat == combatSpells)];
    if (_filter != "")
    {
      _spells = [.. _spells.Where(s => s.Code.StartsWith(_filter))];
    }

    int offsetCursor;
    if (_spells.Count > 0)
    {
      foreach (var spell in _spells.Skip(_offset).Take(_displaySize))
      {
        lines.Add(spell.Code);
      }
      offsetCursor = _cursor + 2;
    }
    else
    {
      lines = [ "No spells with that code." ];
      offsetCursor = -1;
    }
    
    if (Image != "")
      ui.DrawImage(Image, Caption, null);
    else
      ui.DrawDungeonView(app.GameState);
    ui.DrawTextPanel(lines, offsetCursor);
    ui.DrawRoster(app.GameState.Party);
  }
}