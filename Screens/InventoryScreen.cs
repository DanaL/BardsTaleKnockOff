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

class InventoryScreen(App app, Character character) : IScreen
{
  string _status = "";
  int _cursor = 0;
  List<(InvSlot, Item)> _items = [];

  static readonly ItemType[] EquipOrder =
    [ItemType.Weapon, ItemType.Armour, ItemType.Helmet, ItemType.Gloves, ItemType.Shield, ItemType.Ring];

  public void OnKey(SDL_KeyboardEvent key)
  {
    _status = "";

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
        ToggleGear();
        break;
      case SDL_Keycode.SDLK_ESCAPE:
        app.Screens.Pop();
        break;
    }
  }

  void ToggleGear()
  {
    var (slot, item) = _items[_cursor];
    if (item.Type == ItemType.Misc)
    {
      _status = "That cannot be equipped.";
    }
    else if (!item.UseableBy.HasFlag(character.Role))
    {
      string r = character.Role.ToString().ToLower().IndefArticle().Capitalize();
      _status = $"{r} cannot equip that.";
    }
    else
    {
      // Check to see if there's already an item of that type equipped
      for (int i = 0; i < _items.Count; i++)
      {
        var (s, other) = _items[i];
        if (other.Type == item.Type && s.Equipped)
        {
          _items[i] = (new(other.Id, false), item);
          break;
        }
      }
      
      _items[_cursor] = (new(item.Id, !slot.Equipped), item);
      RecalcInventory();
    }    
  }

  void RecalcInventory()
  {
    List<InvSlot> equipped = [.. _items.Where(s => s.Item1.Equipped)
                                         .OrderBy(s => Array.IndexOf(EquipOrder, s.Item2.Type))
                                         .Select(s => s.Item1)];
    List<InvSlot> unequipped = [.. _items.Where(s => !s.Item1.Equipped).Select(s => s.Item1)];
    List<InvSlot> newInv = [.. equipped, .. unequipped];

    for (int i = 0; i < character.Inventory.Length; i++)
    {
      if (i < newInv.Count)
        character.Inventory[i] = newInv[i];
      else
        character.Inventory[i] = null;
    }
  }

  void MoveCursor(int delta)
  {
    _cursor += delta;
    if (_cursor < 0)
      _cursor = 0;
    else if (_cursor >= _items.Count)
      _cursor = _items.Count - 1;
  }

  public void Draw(GameUI ui)
  {
    int highlight = -1;
    List<string> lines = [];
    _items = [.. character.Inventory.OfType<InvSlot>().Select(iv => (iv, ItemCatalog.Item(iv.Id)))];

    if (_items.Count == 0)
    {
      lines.Add($"{character.Name} is empty handed.");
    }
    else
    {
      bool anyCannotUse = false;
      foreach (var i in _items.Where(s => s.Item1.Equipped))
      {
        lines.Add($"§{i.Item2.Name}");
      }
      foreach (var i in _items.Where(s => !s.Item1.Equipped))
      {
        string s = $"{i.Item2.Name}";
        if (!i.Item2.UseableBy.HasFlag(character.Role))
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

      while (lines.Count < 18)
        lines.Add("");
      lines.Add("-enter- to equip/unequip item");
      lines.Add("T) trade");
      lines.Add("U) use");
      lines.Add("D) discard");


      highlight = _cursor;
    }


    ui.DrawPicture("");
    ui.DrawTextPanel(lines, highlight);
    ui.DrawRoster(app.GameState!.Party);
  }
}