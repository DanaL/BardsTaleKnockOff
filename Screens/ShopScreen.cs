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

class PurchaseScreen(App app, Character character, List<(Item, int)> inventory) : IScreen
{
  const int VISIBLE_ROWS = 20;

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
    Item item = inventory[_cursor].Item1;
    int count = inventory[_cursor].Item2;
    int price = item.Value * count;
    if (item.Value > character.Gold)
    {
      _status = $"{character.Name} cannot afford that!";
    }
    else if (character.InventoryFull)
    {
      _status = "Their inventory is full!";
    }
    else
    {
      character.Gold -= price;
      character.AddItem(item.Id, count);
      string n = item.Name.ToLower();
      if (count > 1)
        n = $"{n.Pluralize()} ({count})";
      app.Screens.Push(new PopupScreen(app, $"{character.Name} purchases the {n} for {price} gp."));
    }
  }

  void MoveCursor(int delta)
  {
    _status = "";
    _cursor = Math.Clamp(_cursor + delta, 0, inventory.Count - 1);
    // Scroll just enough to keep the selected row in view.
    if (_cursor < _scrollTop)
      _scrollTop = _cursor;
    else if (_cursor >= _scrollTop + VISIBLE_ROWS)
      _scrollTop = _cursor - VISIBLE_ROWS + 1;
    _scrollTop = Math.Clamp(_scrollTop, 0, Math.Max(0, inventory.Count - VISIBLE_ROWS));
  }

  public void Draw(GameUI ui)
  {
    int highlight;
    List<string> lines = [ $"{character.Name} has {character.Gold} gold pieces.", "" ];
    
    if (!character.InventoryFull)
    {
      highlight = _cursor + 2;
      bool anyCannotUse = false;
      foreach (var (item, count) in inventory)
      {
        int price = item.Value * count;
        string s = item.Name;
        if (count > 1)
          s += $" ({count})";
        s += $" - ${price}";

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

    ui.DrawNoBackground("Ye Olde Equipment Shoppe");
    ui.DrawTextPanel(lines, highlight);
    ui.DrawRoster(app.GameState);
  }
}

class ShoppeScreen : IScreen, IInventorySelectionScreen
{
  readonly App _app;
  List<(Item, int)> _inventory = [];

  const int MAIN = 0;
  const int MENU = 1;
  
  int _state = MAIN;

  int _shopperSlot;
  Character? _shopper = null;

  public ShoppeScreen(App app)
  {
    _app = app;

    // The shop always has some items in stock and later will stock items
    // the player sells them
    _inventory.Add((ItemCatalog.Item(ItemCatalog.DAGGER), 1));
    _inventory.Add((ItemCatalog.Item(ItemCatalog.MACE), 1));
    _inventory.Add((ItemCatalog.Item(ItemCatalog.STAFF), 1));
    _inventory.Add((ItemCatalog.Item(ItemCatalog.SHORTSWORD), 1));
    _inventory.Add((ItemCatalog.Item(ItemCatalog.BROADSWORD), 1));
    _inventory.Add((ItemCatalog.Item(ItemCatalog.HALBERD), 1));
    _inventory.Add((ItemCatalog.Item(ItemCatalog.SHORTBOW), 1));
    _inventory.Add((ItemCatalog.Item(ItemCatalog.LONGBOW), 1));
    _inventory.Add((ItemCatalog.Item(ItemCatalog.ARROW), 10));
    _inventory.Add((ItemCatalog.Item(ItemCatalog.ROBES), 1));
    _inventory.Add((ItemCatalog.Item(ItemCatalog.LEATHER_ARMOUR), 1));
    _inventory.Add((ItemCatalog.Item(ItemCatalog.CHAIN_MAIL),1));
    _inventory.Add((ItemCatalog.Item(ItemCatalog.SPLINT_MAIL), 1));
    _inventory.Add((ItemCatalog.Item(ItemCatalog.PLATE_ARMOUR), 1));
    _inventory.Add((ItemCatalog.Item(ItemCatalog.GAUNTLETS), 1));
    _inventory.Add((ItemCatalog.Item(ItemCatalog.LEATHER_GLOVES), 1));
    _inventory.Add((ItemCatalog.Item(ItemCatalog.HELM), 1));
    _inventory.Add((ItemCatalog.Item(ItemCatalog.BUCKLER), 1));
    _inventory.Add((ItemCatalog.Item(ItemCatalog.TOWER_SHIELD), 1));
    _inventory.Add((ItemCatalog.Item(ItemCatalog.TORCH), 1));
  }

  public void OnKey(SDL_KeyboardEvent key)
  {
    if (_state == MAIN)
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
            if (_app.GameState!.Party.Members[slot] is Character c)
            {
              _shopper = c;
              _shopperSlot = slot;
              _state = MENU;
            }
          }          
          break;
        case SDL_Keycode.SDLK_ESCAPE:
        case SDL_Keycode.SDLK_E:
          ExitShop();
          break;
      }
    }
    else if (_state == MENU)
    {
      switch (key.key)
      {
        case SDL_Keycode.SDLK_ESCAPE:
          _state = MAIN;
          break;
        case SDL_Keycode.SDLK_B:
          if (_shopper is not null)
            _app.Screens.Push(new PurchaseScreen(_app, _shopper, _inventory));
          break;
        case SDL_Keycode.SDLK_P:
          _app.GameState.Party.PoolGold(_shopperSlot);
          break;
        case SDL_Keycode.SDLK_S:
          if (_shopper is not null) 
          {
            InventorySelectionOptions opt = new(_app, _shopper, "Sell which item?", true, "sell", InventoryLabel.SellPrice);
            _app.Screens.Push(new InventorySelectionScreen(opt, this));
          }
          break;
      }
    }
  }

  void ExitShop()
  {
     _app.GameState!.Facing = _app.GameState.Facing.Opposite();
    var (dx, dy) = _app.GameState.Facing.Delta();
    _app.GameState.X += dx;
    _app.GameState.Y += dy;
    _app.Screens.Pop();
  }

  public void Draw(GameUI ui)
  {
    List<string> lines = [ "Welcome to Sam's Supplies. We're here for all your adventuring needs!", "" ];

    switch (_state)
    {
      case MAIN:
        lines.AddRange( "Who is shopping?", "", "E) Exit");
        break;
      case MENU:
        if (_shopper is not null)
        {
          lines.AddRange($"{_shopper.Name} has {_shopper.Gold} gp.", "");
          lines.AddRange("B) Buy gear", "S) Sell items", "P) Pool gold");  
        }
        break;
    }
    
    ui.DrawNoBackground("Ye Olde Equipment Shoppe");
    ui.DrawTextPanel(lines);
    ui.DrawRoster(_app.GameState);
  }

  public void ItemSelected(InvSlot slot, Item item)
  {
    if (_shopper is null)
      return;

    int idx = _shopper.Inventory.IndexOf(slot);
    if (idx != -1)
    {
      int sellValue = item.SellValue * slot.Count;
      string n = slot.Count == 1 ? item.Name : $"{item.Name.Pluralize()} ({slot.Count})";
      _shopper.Inventory[idx] = null;      
      _app.Screens.Push(new PopupScreen(_app, $"{_shopper.Name} sells {n.DefArticle()} for {sellValue} gp."));
      _shopper.Gold += sellValue;
    }
  }

  public void AbortSelection() { }
}