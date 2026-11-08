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

// This has a ton of duplicated code from the inventory screen and later on I
// need to clean that all up and not DIY

using SDL;

namespace BardsTaleKnockOff;

enum InventoryLabel { None, Price, SellPrice };

record InventorySelectionOptions(App App, Character C, string Prompt, bool VerifyEquipped, string Verb, InventoryLabel Label, bool UseableOnly = false);

class InventorySelectionScreen(InventorySelectionOptions options, IInventorySelectionScreen src) : IScreen
{
  public string Image { get; set; } = "";
  public string Caption { get; set; } = "";

  App _app = options.App;
  Character _character = options.C;
  string _prompt = options.Prompt;
  bool _verify = options.VerifyEquipped;
  string _verb = options.Verb;
  InventoryLabel _label = options.Label;

  string _status = "";
  int _cursor = 0;
  List<(InvSlot, Item)> _items = [];

  public void OnKey(SDL_KeyboardEvent key)
  {
    _status = "";

    switch (key.scancode)
    {
      case SDL_Scancode.SDL_SCANCODE_UP:
        MoveCursor(-1);
        break;
      case SDL_Scancode.SDL_SCANCODE_DOWN:
        MoveCursor(1);
        break;
      case SDL_Scancode.SDL_SCANCODE_KP_ENTER:
      case SDL_Scancode.SDL_SCANCODE_RETURN:
        if (_verify && _items[_cursor].Item1.Equipped)
        {          
          string n = _items[_cursor].Item2.Name.DefArticle();          
          _app.Screens.Push(new ConfirmScreen(_app, $"Really {_verb} {n}?", ItemSelected));
        }
        else
        {
          ItemSelected();
        }
        break;
      case SDL_Scancode.SDL_SCANCODE_ESCAPE:
        _app.Screens.Pop();
        break;
    }
  }
  
  void ItemSelected() 
  { 
    src.ItemSelected(_items[_cursor].Item1, _items[_cursor].Item2);
    GetInv();
    if (_cursor >= _items.Count)
      _cursor = _items.Count - 1;
  }

  void MoveCursor(int delta)
  {
    _cursor += delta;
    if (_cursor < 0)
      _cursor = 0;
    else if (_cursor >= _items.Count)
      _cursor = _items.Count - 1;
  }

  void GetInv()
  {
    bool bow = _character.EquippedBow is not null;

    _items.Clear();
    foreach (var slot in _character.Inventory.OfType<InvSlot>())
    {
      Item item = ItemCatalog.Item(slot.Id);

      if (options.UseableOnly && !item.Useable)
        continue;
      else if (options.UseableOnly && item.Type == ItemType.Ammunition && !bow)
        continue;

      _items.Add((slot, item));
    }    
  }

  public void Draw(GameUI ui)
  {
    int highlight = -1;
    List<string> lines = [ _prompt, "" ];

    GetInv();

    if (_items.Count == 0)
    {
      lines.Add($"{_character.Name} is empty handed.");
    }
    else
    {
      bool anyCannotUse = false;
      foreach (var i in _items.Where(s => s.Item1.Equipped))
      {
        string n = _label switch
        {
          InventoryLabel.Price => $"{i.Item2.Name} ({i.Item2.Value} gp)",
          InventoryLabel.SellPrice => $"{i.Item2.Name} ({i.Item2.SellValue} gp)",
          _ => i.Item2.Name
        };
        lines.Add($"§{n}");
      }
      foreach (var (invslot, item) in _items.Where(s => !s.Item1.Equipped))
      {
        string name = invslot.Count == 1 ? item.Name : $"{item.Name} ({invslot.Count})";
        string s = _label switch
        {
          InventoryLabel.Price => $"{name} ({item.Value} gp)",
          InventoryLabel.SellPrice => $"{name} ({item.SellValue} gp)",
          _ => name
        };
        
        if (!item.UseableBy.HasFlag(_character.Role))
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
      lines.Add("-enter- to sell item");
      
      highlight = _cursor + 2;
    }

    if (Image != "")
      ui.DrawImage(Image, Caption, null);
    else
      ui.DrawDungeonView(_app.GameState);
    
    ui.DrawTextPanel(lines, highlight);
    ui.DrawRoster(_app.GameState);
  }  
}