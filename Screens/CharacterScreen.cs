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

class CharacterScreen(App app, int slot) : IScreen, IPartyPickScreen
{
  const int MAIN = 0;
  const int TRANSFER_GOLD = 1;

  int _state = MAIN;
  int _transferToSlot = -1;
  string _transferAmount = "";

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
          int otherSlot = (int)(key.key - SDL_Keycode.SDLK_1);
          if (otherSlot <= app.Roster.Count)
            app.Screens.Replace(new CharacterScreen(app, otherSlot));
          break;
        case SDL_Keycode.SDLK_I:
          app.Screens.Push(new InventoryScreen(app, app.GameState?.Party.Members[slot]!, slot));
          break;
        case SDL_Keycode.SDLK_P:
          app.GameState.Party.PoolGold(slot);
          break;
        case SDL_Keycode.SDLK_T:
          app.Screens.Push(new ChoosePartyMemberScreen(app, this, "Trade gold to whom", slot) { DungeonView = false });
          break;
        case SDL_Keycode.SDLK_ESCAPE:
          app.Screens.Pop();
          break;
      }
    }
    else if (_state == TRANSFER_GOLD)
    {
      switch (key.key)
      {
        case SDL_Keycode.SDLK_0:
        case SDL_Keycode.SDLK_1:
        case SDL_Keycode.SDLK_2:
        case SDL_Keycode.SDLK_3:
        case SDL_Keycode.SDLK_4:
        case SDL_Keycode.SDLK_5:
        case SDL_Keycode.SDLK_6:
        case SDL_Keycode.SDLK_7:
        case SDL_Keycode.SDLK_8:
        case SDL_Keycode.SDLK_9:
          if (_transferAmount.Length < 9)
          {
            int digit = (int)(key.key - SDL_Keycode.SDLK_0);
          _transferAmount += digit;  
          }          
          break;
        case SDL_Keycode.SDLK_ESCAPE:
          _state = MAIN;
          break;
        case SDL_Keycode.SDLK_BACKSPACE:
          if (_transferAmount.Length > 0)
            _transferAmount = _transferAmount[..^1];
          break;
        case SDL_Keycode.SDLK_RETURN:
        case SDL_Keycode.SDLK_KP_ENTER:
          if (int.TryParse(_transferAmount, out int amt))
            TradeGold(amt);
          
          _state = MAIN;
          _transferToSlot = -1;
          _transferAmount = "";
          break;
      }
    }
  }

  void TradeGold(int amount)
  {
    Party p = app.GameState.Party;
    if (p.Members[slot] is not Character c || p.Members[_transferToSlot] is not Character tc)
      return;
    
    amount = int.Min(amount, c.Gold);
    c.Gold -= amount;
    tc.Gold += amount;

    app.Screens.Push(new PopupScreen(app, $"{c.Name} trades {amount} gp to {tc.Name}."));
  }

  public void Draw(GameUI ui)
  {
    string caption = "";
    List<string> lines = [];

    if (app.GameState.Party!.Members[slot] is Character c)
    {
      caption = c.Name;
      if (_state == MAIN)
      {
        lines = CharacterInfo(c);
      }
      else if (_state == TRANSFER_GOLD && app.GameState.Party.Members[_transferToSlot] is Character tc)
      {
        lines = [$"{c.Name} has {c.Gold} gp. Transfer how much to {tc.Name}?", ""];
        lines.Add($"> {_transferAmount}");
      }
    }

    ui.DrawNoBackground(caption);
    ui.DrawTextPanel(lines);
    ui.DrawRoster(app.GameState);
  }

  List<string> CharacterInfo(Character c)
  {
    List<string> lines = [];

    lines.AddRange($"{c.Name}, {c.Lineage} {c.Role}");
    lines.Add($"Str: {c.Str}".PadRight(9) + $"Dex: {c.Dex}");
    lines.Add($"Con: {c.Con}".PadRight(9) + $"Lore: {c.Lore}");
    lines.Add($"Luck: {c.Luck}");
    lines.Add($"Level: {c.Level}".PadRight(12) + $"XP: {c.XP}");

    string hpLine = $"HP: {c.HP} ({c.MaxHP})".PadRight(11);
    if (c.MaxMP > 0)
      hpLine += $"   MP: {c.MP} ({c.MaxMP})";
    lines.Add(hpLine);

    if (c.Inventory[0] is InvSlot inv && inv.Equipped && ItemCatalog.Item(inv.Id).Type == ItemType.Weapon)
    {
      Item weapon = ItemCatalog.Item(inv.Id);
      string s = $"In melee, {c.Name} attacks ";
      s += c.NumOfMeleeAttacks switch
      {
        1 => "once ",
        2 => "twice ",
        _ => $"{c.NumOfMeleeAttacks}X "
      };

      s += $"for {weapon.NumDamageDice}d{weapon.DamageDie}";
      int dmgBonus = weapon.DamageBonus + Character.StatToMod(c.Str);
      if (dmgBonus > 0)
        s += $"+{dmgBonus}";
      else if (dmgBonus < 0)
        s += dmgBonus.ToString();
      s += " dmg";
      if (c.NumOfMeleeAttacks > 1)
        s += "/hit";
      s += ".";

      lines.AddRange("", s);
    }
    if (c.HideInShadows > 0)
      lines.Add($"Hide in shadows: {c.HideInShadows}%");
    if (c.CritChance > 0)
    {
      int adjusted = c.CritChance * 100 / 255;
      string s = $"Critical hit: {adjusted}%";
      if (c.Role == Role.Archer)
        s += " (archers only crit with bows)";
      lines.Add(s);
    }

    lines.AddRange("", $"Gold: {c.Gold}");

    if (c.MaxMP == 0)
    {
      lines.AddRange("", "", "", "");
    }
    else if (c.Role == Role.Trickster)
    {
      lines.AddRange("", "Spells:");      
      lines.Add($"Trickster: {c.TricksterSpellLevel}");
    }
    else if (c.Role == Role.Paladin)
    {
      lines.AddRange("", "Spells:");      
      lines.Add($"Paladin: {c.PaladinSpellLevel}");
    }
    else
    {
      lines.AddRange("", "Spells:");      
      lines.Add($"Conjurer: {c.ConjurerSpellLevel}   Magician: {c.MagicianSpellLevel}");
      lines.Add($"Sorcerer: {c.SorcererSpellLevel}   Wizard: {c.WizardSpellLevel}");
    }
    
    lines.AddRange("", "", "", "", "", "I) View inventory", "P) Pool good");

    if (c.Gold > 0)
      lines.Add("T) Trade gold");

    return lines;
  }

  public void CharacterChosen(int partySlot)
  {
    _state = TRANSFER_GOLD;
    _transferToSlot = partySlot;
    _transferAmount = "";
  }

  public void AbortSelection()
  {
    _state = MAIN;
    while (app.Screens.Top != this)
      app.Screens.Pop();
  }
}