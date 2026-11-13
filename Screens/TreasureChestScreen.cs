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

class TreasureChestScreen : IScreen
{
  readonly App _app;
  readonly Trap _trap;
  HashSet<SDL_Scancode> _disarmOpts = [];
  HashSet<SDL_Scancode> _trzpOpts = [];
  
  List<string> _msgBuffer = [];

  const int MENU = 0;
  const int TRIGGERED = 1;
  const int LOOT = 2;
  const int END = 3;

  int _state = MENU;
  int _gold, _items, _xp;

  // This is really only used right now for the 'tutorial' dungeon but maybe
  // I'll find a use for it in the main dungeon
  public Action? OnLoot { get; set; } = null;

  static readonly Dictionary<string, List<List<int>>> _tables = new()
  {
    [ "wiztower" ] = [[ ItemCatalog.TORCH, ItemCatalog.TORCH, ItemCatalog.DAGGER , ItemCatalog.SHORTBOW, ItemCatalog.BROADSWORD, ItemCatalog.WARAXE, ItemCatalog.HALBERD, ItemCatalog.ARROW ]]
  };

  public TreasureChestScreen(App app, int gold, int items, int xp)
  {
    _app = app;
    _gold = gold;
    _items = items;
    _xp = xp;

    List<Trap> options = [ Trap.Darts, Trap.Darts, Trap.Darts, Trap.PoisonNeedle, Trap.Blades ];    
    _trap = options[_app.Rng.Next(options.Count)];
  }

  public void OnKey(SDL_KeyboardEvent key)
  {
    if (_state == MENU)
    {
      switch (key.scancode)
      {
        case SDL_Scancode.SDL_SCANCODE_I:
        case SDL_Scancode.SDL_SCANCODE_ESCAPE:
          if (_xp > 0)
            Ignore();
          else
            _app.Screens.Pop();
          break;
        case SDL_Scancode.SDL_SCANCODE_O:
          Triggered();
          break;
        case SDL_Scancode.SDL_SCANCODE_1:
        case SDL_Scancode.SDL_SCANCODE_2:
        case SDL_Scancode.SDL_SCANCODE_3:
        case SDL_Scancode.SDL_SCANCODE_4:
        case SDL_Scancode.SDL_SCANCODE_5:
        case SDL_Scancode.SDL_SCANCODE_6:
          int slot = key.scancode - SDL_Scancode.SDL_SCANCODE_1;
          if (_trzpOpts.Contains(key.scancode))
            Trzp(slot);
          else if (_disarmOpts.Contains(key.scancode))
            Disarm(slot);
          break;
      }  
    }
    else if (_state == TRIGGERED)
    {
      _state = LOOT;
      _msgBuffer.Clear();
      Loot();
    }
    else if (_state == END)
    {
      _app.Screens.Pop();
    }
  }

  void Ignore()
  {
    int xpPerChar = _xp / _app.GameState.Party.Alive;

    _msgBuffer.Add("You ignore the treasure chest.");
    _msgBuffer.Add($"Each character {xpPerChar} xp");
    _msgBuffer.AddRange("", " - press any key to continue - ");
  
    for (int j = 0; j < Party.MAX_PARTY_SIZE; j++)
    {
      if (_app.GameState.Party.Members[j] is Character c && !c.Incapacitated) 
        c.XP += xpPerChar;      
    }

    _state = END;
  }

  void Disarm(int charSlot)
  {
    if (_app.GameState.Party.Members[charSlot] is Character c)
    {
      int roll = _app.Rng.Next(100) + 1;
      if (roll < c.DisarmTrap)
      {
        _msgBuffer.Add($"{c.Name} disarms the chest!");
        _state = LOOT;
        Loot();  
      }
      else
      {
        Triggered();
      }
    }
  }

  void Trzp(int charSlot)
  {
    if (_app.GameState.Party.Members[charSlot] is Character c)
    {
      c.MP -= 2;
      _msgBuffer.Add($"{c.Name} casts Trap Zap on the chest.");
      _state = LOOT;
      Loot();
    }    
  }

  void Loot()
  {    
    _msgBuffer.Add("You loot the chest!");
    
    int goldPerChar = int.Max(_gold / _app.GameState.Party.Alive, 1);
    int xpPerChar = _xp / _app.GameState.Party.Alive;

    string s = $"Each character receives {goldPerChar} gp";    
    if (xpPerChar > 0)
      s += $" and {xpPerChar} xp";
    s += '.';
    _msgBuffer.Add(s);
  
    for (int j = 0; j < Party.MAX_PARTY_SIZE; j++)
    {
      if (_app.GameState.Party.Members[j] is Character c && !c.Incapacitated) 
      {
        c.Gold += goldPerChar;
        c.XP += xpPerChar;
      }
    }

    if (_items > 0)
    {
      for (int j = 0; j < _items; j++)
      {
        var table = _tables[_app.GameState.CurrRegion][_app.GameState.CurrLevel];
        int itemId = table[_app.Rng.Next(table.Count)];
        s = GiveItem(itemId);
        if (s != "")
          _msgBuffer.Add(s);
      }  
    } 

    _msgBuffer.AddRange("", " - press any key to continue - ");
    _state = END;

    if (OnLoot is not null)
      OnLoot();
  }

  string GiveItem(int itemId)
  {
    List<Character> party = [];
    for (int j = 0; j < Party.MAX_PARTY_SIZE; j++)
    {
      if (_app.GameState.Party.Members[j] is Character c && !c.InventoryFull)
        party.Add(c);
    }

    if (party.Count == 0)
      return "";
    
    Item item = ItemCatalog.Item(itemId);
    var finder = party[_app.Rng.Next(party.Count)];
    int count = itemId == ItemCatalog.ARROW ? 10 : 1;
    string s = $"{finder.Name} finds ";
    s += count > 1 ? $"{count} {item.Name.Pluralize()}" : item.Name.IndefArticle();
    finder.AddItem(itemId, count);

    return s;
  }

  void Triggered()
  {
    _app.Screens.Push(new PopupScreen(_app, "You trigger a trap on the chest!"));

    switch (_trap)
    {
      case Trap.Darts:
        _msgBuffer.AddRange(Traps.Darts(null, _app.GameState, _app.Rng));        
        break;
      case Trap.PoisonNeedle:
        _msgBuffer.AddRange(Traps.PoisonNeedles(null, _app.GameState, _app.Rng));
        break;
      case Trap.Blades:
        _msgBuffer.AddRange(Traps.Blades(_app.GameState, _app.Rng));
        break;
    }
    
    _msgBuffer.AddRange("", " - press any key to continue - ");

    _state = TRIGGERED;
  }

  public void Draw(GameUI ui)
  {
    List<string> lines = _state switch
    {
      TRIGGERED => _msgBuffer,
      END => _msgBuffer,
      _ => Menu()
    };

    ui.DrawImage("treasurechest.png", "Treasure!", null);
    ui.DrawTextPanel(lines);
    ui.DrawRoster(_app.GameState);
  }

  List<string> Menu()
  {
    List<string> lines = [ "Your band has found a treasure chest!", "", "O) Open it" ];

    _disarmOpts.Clear();
    _disarmOpts.Add(SDL_Scancode.SDL_SCANCODE_O);
    var party = _app.GameState.Party.Members;
    for (int c = 0; c < _app.GameState.Party.Members.Length; c++)
    {
      if (party[c]?.DisarmTrap > 0)
      {
        lines.Add($"{c+1}) Disarm by {party[c]?.Name} ({party[c]?.DisarmTrap}%)");
        _disarmOpts.Add(SDL_Scancode.SDL_SCANCODE_1 + c);
      }
    }

    _trzpOpts.Clear();
    for (int c = 0; c < _app.GameState.Party.Members.Length; c++)
    {
      if (party[c]?.ConjurerSpellLevel > 0 && party[c]?.MP >= 2)
      {
        lines.Add($"{c+1}) Trap Zap by {party[c]?.Name}");
        _trzpOpts.Add(SDL_Scancode.SDL_SCANCODE_1 + c);
      }
    }

    lines.Add("I) Ignore it");

    return lines;
  }
}