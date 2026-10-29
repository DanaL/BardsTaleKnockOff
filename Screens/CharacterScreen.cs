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

class CharacterScreen(App app, int slot) : IScreen
{
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
        int otherSlot = (int) (key.key - SDL_Keycode.SDLK_1);
        if (otherSlot <= app.Roster.Count)
          app.Screens.Replace(new CharacterScreen(app, otherSlot));
        break;
      case SDL_Keycode.SDLK_I:
        app.Screens.Push(new InventoryScreen(app, app.GameState?.Party.Members[slot]!));
        break;
      case SDL_Keycode.SDLK_P:
        app.GameState.Party.PoolGold(slot);
        break;
      case SDL_Keycode.SDLK_ESCAPE:
        app.Screens.Pop();
        break;
    }
  }

  public void Draw(GameUI ui)
  {
    string caption = "";
    List<string> lines = [];
    if (app.GameState!.Party!.Members[slot] is Character c)
    {
      caption = c.Name;

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
        s += " dmg.";

        lines.AddRange("", s);
      }
      if (c.HideInShadows > 0)
        lines.Add($"Hide in shadows: {c.HideInShadows}%");

      lines.AddRange("", $"Gold: {c.Gold}");

      if (c.MaxMP > 0)
      {
        lines.AddRange("", "Spells:");
        lines.Add($"Conjurer: {c.ConjurerSpellLevel}   Magician: {c.MagicianSpellLevel}");
        lines.Add($"Sorcerer: {c.SorcererSpellLevel}   Wizard: {c.WizardSpellLevel}");
      }
      else
      {
        lines.AddRange("", "", "", "");
      }

      lines.AddRange("", "", "", "", "", "I) View inventory", "P) Pool good");
    }

    ui.DrawPicture(caption);    
    ui.DrawTextPanel(lines);
    ui.DrawRoster(app.GameState?.Party);
  }  
}