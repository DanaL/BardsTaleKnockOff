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

class PurchaseScreen(App app, Character character, List<Item> inventory) : IScreen
{
  const int VisibleRows = 20;

  string _status = "";
  int _cursor = 0;
  int _scrollTop = 0;

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
      case SDL_Keycode.SDLK_RETURN:
      case SDL_Keycode.SDLK_KP_ENTER:
        CheckPurchase();
        break;
      case SDL_Keycode.SDLK_ESCAPE:
        app.Screens.Pop();
        break;  
    }    
  }

  void CheckPurchase()
  {
    Item item = inventory[_cursor];
    if (item.Value > character.Gold)
    {
      _status = $"{character.Name} cannot afford that!";
    }
    else
    {
      character.Gold -= item.Value;
      _status = $"{character.Name} purchases the {item.Name.ToLower()}.";
      character.AddItem(item.Id);
    }
  }

  void MoveCursor(int delta)
  {
    _cursor = Math.Clamp(_cursor + delta, 0, inventory.Count - 1);
    // Scroll just enough to keep the selected row in view.
    if (_cursor < _scrollTop)
      _scrollTop = _cursor;
    else if (_cursor >= _scrollTop + VisibleRows)
      _scrollTop = _cursor - VisibleRows + 1;
    _scrollTop = Math.Clamp(_scrollTop, 0, Math.Max(0, inventory.Count - VisibleRows));
  }

  public void Draw(GameUI ui)
  {
    int highlight = -1;
    List<string> lines = [ $"{character.Name} has {character.Gold} gold pieces.", "" ];
    
    if (character.Inventory.Any(i => i is null))
    {
      highlight = _cursor + 2;
      bool anyCannotUse = false;
      foreach (var item in inventory)
      {
        string s = $"{item.Name} - ${item.Value}";

        if (!item.UseableBy.HasFlag(character.Role)) 
        {
          s += " *";
          anyCannotUse = true;
        }

        lines.Add(s);
      }

      if (anyCannotUse)
        lines.AddRange("", "* cannot use");

      if (!string.IsNullOrWhiteSpace(_status))
        lines.AddRange("", _status);
    }
    else
    {
      lines.Add("Your inventory is full!");
      highlight = -1;  
    }

    ui.DrawPicture("Equipment Shop");
    ui.DrawTextPanel(lines, highlight);
    ui.DrawRoster(app.GameState!.Party);
  }
}

class ShopScreen : IScreen
{
  readonly App _app;
  List<Item> _inventory = [];

  public ShopScreen(App app)
  {
    _app = app;

    // The shop always has some items in stock and later will stock items
    // the player sells them
    _inventory.Add(ItemCatalog.Item(ItemCatalog.DAGGER));
    _inventory.Add(ItemCatalog.Item(ItemCatalog.MACE));
    _inventory.Add(ItemCatalog.Item(ItemCatalog.STAFF));
    _inventory.Add(ItemCatalog.Item(ItemCatalog.SHORTSWORD));
    _inventory.Add(ItemCatalog.Item(ItemCatalog.BROADSWORD));
    _inventory.Add(ItemCatalog.Item(ItemCatalog.HALBERD));
    _inventory.Add(ItemCatalog.Item(ItemCatalog.ROBES));
    _inventory.Add(ItemCatalog.Item(ItemCatalog.LEATHER_ARMOUR));
    _inventory.Add(ItemCatalog.Item(ItemCatalog.CHAIN_MAIL));
    _inventory.Add(ItemCatalog.Item(ItemCatalog.SPLINT_MAIL));
    _inventory.Add(ItemCatalog.Item(ItemCatalog.PLATE_ARMOUR));
    _inventory.Add(ItemCatalog.Item(ItemCatalog.GAUNTLETS));
    _inventory.Add(ItemCatalog.Item(ItemCatalog.LEATHER_GLOVES));
    _inventory.Add(ItemCatalog.Item(ItemCatalog.HELM));
    _inventory.Add(ItemCatalog.Item(ItemCatalog.BUCKLER));
    _inventory.Add(ItemCatalog.Item(ItemCatalog.TOWER_SHIELD));
    _inventory.Add(ItemCatalog.Item(ItemCatalog.TORCH));
  }

  public void OnKey(SDL_KeyboardEvent key)
  {
    switch (key.key)
    {
      case SDL_Keycode.SDLK_1:
      case SDL_Keycode.SDLK_2:
      case SDL_Keycode.SDLK_3:
      case SDL_Keycode.SDLK_4:
      case SDL_Keycode.SDLK_5:
      case SDL_Keycode.SDLK_6:
        int slot = (int) (key.key - SDL_Keycode.SDLK_0) - 1;
        if (slot <= _app.GameState!.Party.Count)
        {
          Character? c = _app.GameState!.Party.Members[slot];
          if (c is not null)
            _app.Screens.Push(new PurchaseScreen(_app, c, _inventory));
        }          
        break;
      case SDL_Keycode.SDLK_ESCAPE:
      case SDL_Keycode.SDLK_E:
        _app.GameState!.Facing = _app.GameState.Facing.Opposite();
        var (dx, dy) = _app.GameState.Facing.Delta();
        _app.GameState.X += dx;
        _app.GameState.Y += dy;
        _app.Screens.Pop();
        break;
    }
  }

  public void Draw(GameUI ui)
  {
    List<string> lines = [ "Welcome to Sam's Supplies. We're here for all your adventuring needs!", "", "Who will shop?", "", "E) Exit" ];
    
    ui.DrawPicture("Equipment Shop");
    ui.DrawTextPanel(lines);
    ui.DrawRoster(_app.GameState!.Party);
  }
}