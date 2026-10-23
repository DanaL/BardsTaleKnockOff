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
  const int NAME_CHARACTER = 0;
  const int PICK_LINEAGE = 1;
  const int PICK_ROLE = 2;
  const int ROLL_STATS = 3;
  const int DONE = 4;

  string _name = "";
  string _status = "";
  Role _role = Role.Warrior;
  Lineage _lineage = Lineage.Human;
  int _state = NAME_CHARACTER;


  public void OnEnter() => app.StartTextInput();
  public void OnExit() => app.StopTextInput();

  public void OnText(string text)
  {
    if (_state == PICK_LINEAGE)
    {      
      if (text == "1")
      {
        _lineage = Lineage.Human;
        _state = PICK_ROLE;
      }
      else if (text == "2")
      {
        _lineage = Lineage.Orc;
        _state = PICK_ROLE;
      }
      else if (text == "3")
      {
        _lineage = Lineage.Dwarf;
        _state = PICK_ROLE;
      }
      else if (text == "4")
      {
        _lineage = Lineage.Gnome;
        _state = PICK_ROLE;
      }
      else if (text == "5")
      {
        _lineage = Lineage.Elf;
        _state = PICK_ROLE;
      }
      else if (text == "6")
      {
        _lineage = Lineage.Goblin;
        _state = PICK_ROLE;
      }
    }
    else if (_state == PICK_ROLE)
    {
      if (text == "1")
      {
        _role = Role.Warrior;
        _state = ROLL_STATS;
      }
      else if (text == "2")
      {
        _role = Role.Rogue;
        _state = ROLL_STATS;
      }
      else if (text == "3")
      {
        _role = Role.Hunter;
        _state = ROLL_STATS;
      }
      else if (text == "4")
      {
        _role = Role.Conjurer;
        _state = ROLL_STATS;
      }
      else if (text == "5")
      {
        _role = Role.Magician;
        _state = ROLL_STATS;
      }
    }
    else if (_state == NAME_CHARACTER)
    {
      foreach (char ch in text)
      {
        if (_name.Length < MAX_NAME_LENGTH && (char.IsLetterOrDigit(ch) || ch is ' ' or '\'' or '-'))
          _name += ch;
      }

      _status = "";
    }
  }

  public void OnKey(SDL_KeyboardEvent key)
  {
    switch (key.key)
    {
      case SDL_Keycode.SDLK_BACKSPACE:
        if (_state == NAME_CHARACTER && _name.Length > 0)
          _name = _name[..^1];
        break;
      case SDL_Keycode.SDLK_RETURN:
      case SDL_Keycode.SDLK_KP_ENTER:
        _state = _state switch
        {
          NAME_CHARACTER => PICK_LINEAGE,
          PICK_LINEAGE => PICK_ROLE,
          PICK_ROLE => ROLL_STATS,          
          _ => DONE
        };

        if (_state == DONE)
          Finish();
        break;
      case SDL_Keycode.SDLK_ESCAPE:
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

    // Placeholder stats until real character creation exists.
    app.Roster.Add(new Character { Name = name, Role = _role, Lineage = _lineage, AC = 10, HP = 12, MaxHP = 12 });
    app.Screens.Pop();
  }

  public void Draw(GameUI ui)
  {
    List<string> lines = [ "Create a character", "" ];

    if (_state == NAME_CHARACTER)
    {
      lines.AddRange("Name your character:", $"  {_name}_");
    }
    else if (_state == PICK_LINEAGE)
    {
      lines.AddRange(_name, "", "Choose their lineage:");
      lines.Add("1) Human");
      lines.Add("2) Orc");
      lines.Add("3) Dwarf");
      lines.Add("4) Gnome");
      lines.Add("5) Elf");
      lines.Add("6) Goblin");
    }
    else if (_state == PICK_ROLE)
    {
      string lineage = _lineage.ToString().ToLower();
      lines.AddRange($"{_name}, {lineage}", "", "Choose their role:");
      lines.Add("1) Warrior");
      lines.Add("2) Rogue");
      lines.Add("3) Hunter");
      lines.Add("4) Conjurer");
      lines.Add("5) Magician");
    }
    else if (_state == ROLL_STATS)
    {
      string lineage = _lineage.ToString().ToLower();
      string role = _role.ToString().ToLower();
      lines.AddRange($"{_name}, {lineage} {role}", "");

      lines.AddRange("", "(r) reroll or -enter- to accept");
    }

    lines.AddRange("", "Esc to cancel.");
   
    if (_status.Length > 0)
      lines.AddRange(["", _status]);

    ui.DrawPicture("Adventurers' Guild");
    ui.DrawTextPanel(lines);
    ui.DrawRoster(app.Game?.Party);
  }
}
