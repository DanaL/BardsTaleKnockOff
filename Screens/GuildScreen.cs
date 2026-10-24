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

// The Adventurers' Guild: create characters, choose the party from the
// characters on file, then leave for the town.
class GuildScreen(App app) : IScreen
{
  // What the next key press means. This is screen state: it only matters
  // while the Guild is open, so it lives here rather than in the game.
  enum Mode { Menu, ChoosingToAdd, ChoosingToRemove }

  Mode _mode = Mode.Menu;
  string _status = "";

  // While adding: the characters who could join (on file, not already in the
  // party) in alphabetical order, which one is selected, and which is the
  // first of the rows currently scrolled into view.
  const int VisibleRows = 10;
  List<Character> _choices = [];
  int _cursor;
  int _scrollTop;

  Party Party => app.Game!.Party;

  List<Character> AddableCharacters() =>
    [.. app.Roster
      .Where(c => !Party.Contains(c))
      .OrderBy(c => c.Name, StringComparer.CurrentCultureIgnoreCase)];

  public void OnKey(SDL_KeyboardEvent key)
  {
    switch (_mode)
    {
      case Mode.Menu:
        OnMenuKey(key);
        break;
      case Mode.ChoosingToAdd:
        OnAddKey(key);
        break;
      case Mode.ChoosingToRemove:
        OnRemoveKey(key);
        break;
    }
  }

  void OnMenuKey(SDL_KeyboardEvent key)
  {
    _status = "";
    switch (key.key)
    {
      case SDL_Keycode.SDLK_C:
        app.Screens.Push(new CreateCharacterScreen(app));
        break;
      case SDL_Keycode.SDLK_A:
        if (Party.IsFull)
          _status = "Your party is full.";
        else if (app.Roster.Count == 0)
          _status = "You have not created any characters yet!";
        else if (!app.Roster.Any(c => !Party.Contains(c)))
          _status = "Everyone on file is already in the party.";
        else
        {
          _mode = Mode.ChoosingToAdd;
          _choices = AddableCharacters();
          _cursor = 0;
          _scrollTop = 0;
        }
        break;
      case SDL_Keycode.SDLK_R:
        if (Party.Count == 0)
          _status = "There's nobody in the party to remove.";
        else
          _mode = Mode.ChoosingToRemove;
        break;
      case SDL_Keycode.SDLK_L:
        if (Party.Count == 0)
        {
          _status = "You can't go adventuring alone. Add someone to your party first.";
        }
        else
        {
          app.Game!.Say("You leave the Guild and step out into the town.");
          app.Screens.Replace(new AdventureScreen(app));
        }
        break;
    }
  }

  // Up/Down (and PageUp/PageDown) move the selection, Enter adds the selected
  // character, Esc goes back to the menu. After adding, the list stays open
  // so you can pick several people in a row.
  void OnAddKey(SDL_KeyboardEvent key)
  {
    switch (key.key)
    {
      case SDL_Keycode.SDLK_UP:
        MoveCursor(-1);
        break;
      case SDL_Keycode.SDLK_DOWN:
        MoveCursor(1);
        break;
      case SDL_Keycode.SDLK_PAGEUP:
        MoveCursor(-VisibleRows);
        break;
      case SDL_Keycode.SDLK_PAGEDOWN:
        MoveCursor(VisibleRows);
        break;
      case SDL_Keycode.SDLK_RETURN:
      case SDL_Keycode.SDLK_KP_ENTER:
        var c = _choices[_cursor];
        Party.Add(c);
        _status = $"{c.Name} joins the party.";
        _choices = AddableCharacters();
        if (Party.IsFull || _choices.Count == 0)
          _mode = Mode.Menu;
        else
          MoveCursor(0);  // keep the cursor and scroll position valid
        break;
      case SDL_Keycode.SDLK_ESCAPE:
        _mode = Mode.Menu;
        _status = "";
        break;
    }
  }

  void MoveCursor(int delta)
  {
    _cursor = Math.Clamp(_cursor + delta, 0, _choices.Count - 1);
    // Scroll just enough to keep the selected row in view.
    if (_cursor < _scrollTop)
      _scrollTop = _cursor;
    else if (_cursor >= _scrollTop + VisibleRows)
      _scrollTop = _cursor - VisibleRows + 1;
    _scrollTop = Math.Clamp(_scrollTop, 0, Math.Max(0, _choices.Count - VisibleRows));
  }

  // Party members are numbered 1-6, as in the roster at the bottom.
  void OnRemoveKey(SDL_KeyboardEvent key)
  {
    _mode = Mode.Menu;
    if (key.key == SDL_Keycode.SDLK_ESCAPE)
      return;

    int slot = (int)key.key - '1';
    if (slot < 0 || slot >= Party.MaxMembers || Party.Members[slot] is null)
    {
      _status = "There's nobody in that slot.";
      return;
    }
    Party.RemoveAt(slot);
  }

  public void Draw(GameUI ui)
  {
    var lines = new List<string> { "Welcome to the Adventurers' Guild.", "" };
    int highlight = -1;

    lines.AddRange(_mode switch
    {
      Mode.ChoosingToAdd => ["Add whom to the party?", "Up/Down to choose, Enter to add, Esc when done.", ""],
      Mode.ChoosingToRemove => ["Remove whom from the party?"],
      _ => ["C) Create a character", "A) Add to party", "R) Remove from party", "L) Leave the Guild"]
    });

    if (_mode == Mode.ChoosingToAdd)
    {
      lines.Add(_scrollTop > 0 ? "  (more above)" : "");
      int end = Math.Min(_scrollTop + VisibleRows, _choices.Count);
      for (int i = _scrollTop; i < end; i++)
      {
        if (i == _cursor)
          highlight = lines.Count;
        lines.Add($"  {_choices[i].Name}, {_choices[i].Role}");
      }
      lines.Add(end < _choices.Count ? "  (more below)" : "");
    }

    if (_status.Length > 0)
      lines.AddRange(["", _status]);

    ui.DrawPicture("Adventurers' Guild");
    ui.DrawTextPanel(lines, highlight);
    ui.DrawRoster(Party);
  }
}
