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

using BardsTaleKnockOff;

class DeleteCharacterScreen : IScreen
{
  const int VISIBLE_ROWS = 10;
  readonly App _app;
  int _cursor;
  int _scrollTop;
  
  List<Character> _inGuild;
  List<Character> _choices;

  public DeleteCharacterScreen(App app)
  {
    _app = app;
    _inGuild = [.. app.Roster
                  .Where(c => !app.GameState.Party.Contains(c))
                  .OrderBy(c => c.Name, StringComparer.CurrentCultureIgnoreCase)];
    _choices = [.. _inGuild];
  }

  public void OnKey(SDL_KeyboardEvent key)
  {
    switch (key.scancode)
    {
      case SDL_Scancode.SDL_SCANCODE_UP:
        if (_inGuild.Count > 0)
          MoveCursor(-1);
        break;
      case SDL_Scancode.SDL_SCANCODE_DOWN:
        if (_inGuild.Count > 0)
          MoveCursor(1);
        break;
      case SDL_Scancode.SDL_SCANCODE_ESCAPE:
        _app.Screens.Pop();
        break;
      case SDL_Scancode.SDL_SCANCODE_RETURN:
      case SDL_Scancode.SDL_SCANCODE_KP_ENTER:
        if (_inGuild.Count > 0)
        {
          var c = _choices[_cursor];
        _app.Screens.Push(new ConfirmScreen(_app, $"Really delete {c.Name}? This is permanent!", DeleteCharacter));  
        }        
        break;
    }
  }

  void DeleteCharacter()
  {
    var c = _choices[_cursor];
    _app.Roster.Remove(c);
    Serialize.WriteSaveGame(_app);
    _app.Screens.Pop();
    _app.Screens.Push(new PopupScreen(_app, $"{c.Name} has been deleted"));
  }

  void MoveCursor(int delta)
  {
    _cursor = Math.Clamp(_cursor + delta, 0, _choices.Count - 1);
    // Scroll just enough to keep the selected row in view.
    if (_cursor < _scrollTop)
      _scrollTop = _cursor;
    else if (_cursor >= _scrollTop + VISIBLE_ROWS)
      _scrollTop = _cursor - VISIBLE_ROWS + 1;
    _scrollTop = Math.Clamp(_scrollTop, 0, Math.Max(0, _choices.Count - VISIBLE_ROWS));
  }

  public void Draw(GameUI ui)
  {
    List<string> lines = [ "Delete which character?", "" ];
    int highlight = -1;

    lines.Add(_scrollTop > 0 ? "  (more above)" : "");
    int end = Math.Min(_scrollTop + VISIBLE_ROWS, _choices.Count);
    for (int i = _scrollTop; i < end; i++)
    {
      if (i == _cursor)
        highlight = lines.Count;
      lines.Add($"  {_choices[i].Name}, {_choices[i].Role}");
    }
    lines.Add(end < _choices.Count ? "  (more below)" : "");

    if (_inGuild.Count == 0)
    {
      lines.Add("There are no characters currently staying in the Guild.");
    }

    ui.DrawNoBackground("Adventurers' Guild");
    ui.DrawTextPanel(lines, highlight);
    ui.DrawRoster(_app.GameState);
  }
}