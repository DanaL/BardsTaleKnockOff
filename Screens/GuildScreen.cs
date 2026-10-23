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

  Party Party => app.Game!.Party;

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
          _mode = Mode.ChoosingToAdd;
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
          app.Screens.Replace(new TownScreen(app));
        }
        break;
    }
  }

  void OnAddKey(SDL_KeyboardEvent key)
  {
    _mode = Mode.Menu;
    if (key.key == SDL_Keycode.SDLK_ESCAPE)
      return;

    int index = (int)key.key - '1';
    if (index < 0 || index >= app.Roster.Count)
    {
      _status = "There's nobody by that letter.";
      return;
    }
    var c = app.Roster[index];
    if (Party.Contains(c))
      _status = $"{c.Name} is already in the party.";
    else
      Party.Add(c);
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
    
    lines.AddRange(_mode switch
    {
      Mode.ChoosingToAdd => ["Add whom to the party?"],
      Mode.ChoosingToRemove => ["Remove whom from the party?"],
      _ => ["C) Create a character", "A) Add to party", "R) Remove from party", "L) Leave the Guild"]
    });

    if (_mode == Mode.ChoosingToAdd)
    {
      for (int i = 0; i < app.Roster.Count; i++)
      {
        var c = app.Roster[i];
        var inParty = Party.Contains(c) ? " (in party)" : "";
        lines.Add($"  {(char)('0' + i + 1)}) {c.Name}, {c.ClassName}{inParty}");
      }
      lines.Add("");
    }

    if (_status.Length > 0)
      lines.AddRange(["", _status]);

    ui.DrawPicture("Adventurers' Guild");
    ui.DrawTextPanel(lines);
    ui.DrawRoster(Party);
  }
}
