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

namespace BardsTaleKnockOff;

enum Lineage { Human, Orc, Dwarf, Gnome, Elf, Goblin }

[Flags]
enum Role { Warrior = 1 << 0, Rogue = 1 << 1, Hunter = 1 << 2, Conjurer = 1 << 3, Magician = 1 << 4, 
            Sorcerer = 1 << 5, Wizard = 1 << 6, Trickster = 1 << 7, Archer = 1 << 8, 
            Paladin = 1 << 9 }

record struct InvSlot(int Id, bool Equipped);

class Character
{
  public const int INVENTORY_SLOTS = 12;

  public required string Name { get; init; }
  public required Lineage Lineage { get; init; }
  public required Role Role { get; init; }  
  public int HP { get; set; }
  public int MaxHP { get; set; }
  public int MP { get; set; }
  public int MaxMP { get; set; }
  public string Condition { get; set; } = "";

  public int Str { get; set; }
  public int Dex { get; set; }
  public int Con { get; set; }
  public int Lore { get; set; }
  public int Luck { get; set; }
  public int Gold { get; set; }
  public int XP { get; set; }
  public int Level { get; set; }

  public int ConjurerSpellLevel { get; set; } = 0;
  public int MagicianSpellLevel { get; set; } = 0;
  public int SorcererSpellLevel { get; set; } = 0;
  public int WizardSpellLevel { get; set; } = 0;

  public int Resurrections { get; set; } = 0;
  public bool PermanentlyDead { get; set; } = false;

  public List<TempBuff> Buffs { get; set;} = [];

  // Conditions that take a character out of action. Add STONED etc. here.
  public bool Incapacitated => Condition is "DEAD";
  
  public int THAC0 => Role switch
  {
    Role.Warrior or Role.Paladin or Role.Archer => int.Max(1, 20 - Level),
    Role.Rogue or Role.Trickster => int.Max(1, 20 - Level / 2),
    Role.Hunter => int.Max(1, 17 - Level / 2),
    Role.Conjurer or Role.Magician or Role.Sorcerer or Role.Wizard => int.Max(1, 20 - Level / 3),
    _ => throw new Exception($"Unknown role: {Role}!"),
  };

  public Item? ReadiedWeapon
  {
    get
    {
      if (Inventory[0] is InvSlot slot && slot.Equipped && ItemCatalog.Item(slot.Id) is Item item && item.Type == ItemType.Weapon)
        return item;
      else
        return null;
    }  
  }

  public int AC 
  {
    get
    {
      int ac = 10 - StatToMod(Dex);
      foreach (var inv in Inventory.OfType<InvSlot>())
      {
        if (inv.Equipped)
          ac -= ItemCatalog.Item(inv.Id).ACMod;
      }

      foreach (var buff in Buffs)
      {
        if (buff.Buff == Buff.AirArmour)
          ac -= 2;
        else if (buff.Buff == Buff.SorcShield)
          ac -= 1;
      }

      return ac;
    }
  } 

  public int NumOfMeleeAttacks => Role switch
  {
    Role.Warrior or Role.Paladin or Role.Archer => Level / 5 + 1,
    _ => 1,
  };
  
  public int HideInShadows
  {
    get
    {
      switch (Role)
      {
        case Role.Rogue:
        case Role.Hunter:
        case Role.Trickster:
          int score = 15 + 5 * Level;
          if (Lineage == Lineage.Gnome || Lineage == Lineage.Goblin)
            score += 10;
          if (Dex > 16)
            score += 10;
          return int.Min(99, score);
        default:
          return 0;
      }
    }
  }

  public InvSlot?[] Inventory { get; init; } = new InvSlot?[INVENTORY_SLOTS];

  public bool AddItem(int id)
  {
    int slot = Array.IndexOf(Inventory, null);
    if (slot < 0)
      return false;
    Inventory[slot] = new(id, false);

    return true;
  }

  public static int StatToMod(int stat) => (int) Math.Floor((stat - 10.0) / 2);

  public bool Resurrect(Rng rng)
  {
    if (rng.Next(100) >= Resurrections * 2)
    {
      HP = 1;
      ++Resurrections;
      Condition = "";

      return true;
    }
    else
    {
      PermanentlyDead = true;
      return false;
    }
  }
}

class Party
{
  public const int MAX_PARTY_SIZE = 6;

  // The roster's "S" slot, shown above the six regular members.
  public Character? Special { get; set; }
  public Character?[] Members { get; } = new Character?[MAX_PARTY_SIZE];

  public int Count => Members.Count(m => m is not null);
  public int Alive => Members.Count(m => m is not null && !m.Incapacitated);

  public bool IsFull => Count == MAX_PARTY_SIZE;
  public bool Contains(Character c) => Members.Contains(c);

  // Add to the first empty slot. False if the party is full.
  public bool Add(Character c)
  {
    int slot = Array.IndexOf(Members, null);
    if (slot < 0)
      return false;
    Members[slot] = c;
    return true;
  }

  public void ClearCombatBuffs()
  {
    for (int j = 0; j < Count; j++)
    {
      if (Members[j] is Character c)
        c.Buffs = [.. c.Buffs.Where(b => !b.CombatOnly)];
    }
  }

  public void PoolGold(int slot)
  {
    if (Members[slot] is not Character c)
      return;

    int total = 0;
    foreach (Character other in Members.OfType<Character>())
    {
      total += other.Gold;
      other.Gold = 0;
    }

    c.Gold = total;
  }

  // Remove the member in a slot (0-based) and close the gap, so members
  // always fill slots 1, 2, 3... with no holes.
  public void RemoveAt(int slot)
  {
    for (int i = slot; i < MAX_PARTY_SIZE - 1; i++)
      Members[i] = Members[i + 1];
    Members[MAX_PARTY_SIZE - 1] = null;
  }

  // Shuffle incapacitated members to the back, keeping everyone's relative
  // order otherwise. Empty slots stay at the end.
  public void MoveIncapacitatedToBack()
  {
    List<Character> active = [.. Members.OfType<Character>().Where(c => !c.Incapacitated)];
    List<Character> downed = [.. Members.OfType<Character>().Where(c => c.Incapacitated)];
    List<Character> ordered = [.. active, .. downed];

    for (int i = 0; i < MAX_PARTY_SIZE; i++)
      Members[i] = i < ordered.Count ? ordered[i] : null;
  }
}
