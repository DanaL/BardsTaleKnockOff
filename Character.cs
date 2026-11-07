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
enum Role
{
  Warrior = 1 << 0, Rogue = 1 << 1, Hunter = 1 << 2, Conjurer = 1 << 3, Magician = 1 << 4,
  Sorcerer = 1 << 5, Wizard = 1 << 6, Trickster = 1 << 7, Archer = 1 << 8,
  Paladin = 1 << 9
}

record struct InvSlot(int Id, bool Equipped);

class Character
{
  public const int INVENTORY_SLOTS = 12;

  public required string Name { get; init; }
  public required Lineage Lineage { get; init; }
  public required Role Role { get; set; }

  public int HP { get; set; }
  public int BaseHP { get; set; }
  public int MaxHP => BaseHP + int.Max(StatToMod(Con) * Level, Level);
  
  public int MP { get; set; }
  public int BaseMP { get; set; }
  public int MaxMP => BaseMP > 0 ? BaseMP + int.Max(StatToMod(Lore) * Level, Level) : 0;
  
  public string Condition { get; set; } = "";

  public int Str { get; set; }
  public int Dex { get; set; }
  public int Con { get; set; }
  public int Lore { get; set; }
  public int Luck { get; set; }
  public int Gold { get; set; }
  public int XP { get; set; }
  public int Level { get; set; }
  public int AbilityPoints { get; set; } = 0;
  public int CritChance { get; set; } = 0;

  public int ConjurerSpellLevel { get; set; } = 0;
  public int MagicianSpellLevel { get; set; } = 0;
  public int SorcererSpellLevel { get; set; } = 0;
  public int WizardSpellLevel { get; set; } = 0;
  public int TricksterSpellLevel { get; set; } = 0;
  public int PaladinSpellLevel { get; set; } = 0;

  public int Resurrections { get; set; } = 0;
  public bool PermanentlyDead { get; set; } = false;

  public List<TempBuff> Buffs { get; set; } = [];

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
      int score;

      switch (Role)
      {
        case Role.Trickster:
        case Role.Hunter:
          score = 15 + 5 * int.Max(Level - 2, 3);
          break;          
        case Role.Rogue:
          score = 15 + 5 * Level;
          break;          
        default:
          return 0;
      }

      if (Lineage == Lineage.Gnome || Lineage == Lineage.Goblin)
        score += 5;
      if (Dex > 16)
        score += 10;

      return int.Min(99, score);
    }
  }

  public int MaxSpellLevel
  {
    get
    {
      if (Role == Role.Trickster || Role == Role.Paladin)
        return Level >= 9 ? 3 : Level >= 5 ? 2 : 1;
      else
        return int.Clamp((int)Math.Round(Level / 2.0, MidpointRounding.AwayFromZero), 1, 13);
    }
  }
  
  public InvSlot?[] Inventory { get; init; } = new InvSlot?[INVENTORY_SLOTS];

  public string ReceiveDmg(int dmg)
  {
    HP = int.Max(HP - dmg, 0);
    if (HP < 1)
    {
      Condition = "DEAD";
      return $"{Name} is killed!";
    }

    return "";
  }

  public bool InventoryFull => !Inventory.Any(s => s is null);

  public bool AddItem(int id)
  {
    int slot = Array.IndexOf(Inventory, null);
    if (slot < 0)
      return false;
    Inventory[slot] = new(id, false);

    return true;
  }

  public static int StatToMod(int stat) => (int)Math.Floor((stat - 10.0) / 2);

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

  public int MagicPower
  {
    get
    {
      int score = 2 + int.Min(Level / 2, 18) + int.Max(StatToMod(Luck), 1);
      switch (Role)
      {
        case Role.Sorcerer:
        case Role.Wizard:
          score += 3;
          break;
        case Role.Magician:
        case Role.Conjurer:
        case Role.Trickster:        
          score += 2;
          break;
        case Role.Paladin:
          if (Level < 4) score += 1;
          else if (Level < 10) score += 2;
          else if (Level < 18) score += 3;
          else if (Level < 26) score += 4;
          else if (Level < 30) score += 5;
          else score += 6;
          break;
        default:
          score += 1;
          break;
      }
      
      // Eventually items like Luckshields will affect the score
      // foreach (var inv in Inventory.OfType<InvSlot>())
      // {
      //   if (inv.Equipped && )
      // }

      return score;
    }
  }

  public SaveResult RollSavingThrow(int monsterRank, Rng rng)
  {
    int roll = monsterRank + rng.Next(8);    
    int margin = MagicPower - roll;

    if (margin < 0)
      return SaveResult.Failure;
    else if (margin <= 4)
      return SaveResult.Partial;
    else 
      return SaveResult.Success;    
  }

  public void LevelUp(Rng rng)
  {
    int nextLevel = Level + 1;
    int newHP = 0;
    switch (Role)
    {
      case Role.Warrior:
      case Role.Paladin:
      case Role.Archer:
        newHP = rng.Next(4, 17);
        if (nextLevel % 2 == 0)
          AbilityPoints += 1;
        break;
      
      case Role.Hunter:
         newHP = rng.Next(1, 9);
         CritChance += rng.Next(1, 32);
        if (nextLevel % 2 == 0)
          AbilityPoints += 1;
        break;
      case Role.Rogue:
      case Role.Trickster:
        newHP = rng.Next(1, 9);
        if (nextLevel % 2 == 0)
          AbilityPoints += 1;
        break;
      case Role.Magician:
      case Role.Conjurer:
        newHP = rng.Next(1, 5);
        if (nextLevel % 4 == 0)
          AbilityPoints += 1;
        break;
      case Role.Sorcerer:
      case Role.Wizard:
        newHP = rng.Next(1, 7);
        if (nextLevel % 4 == 0)
          AbilityPoints += 1;
        break;
    }

    int hpDelta = MaxHP - HP;
    int mpDelta = MaxMP - MP;

    ++Level;
    
    BaseHP += newHP;
    HP = MaxHP - hpDelta;
    
    if (BaseMP > 0)
    {
      int newMP = rng.Next(1, 5);      
      BaseMP += newMP;
      MP = MaxMP - mpDelta;
    }
  }
}

class XPTable
{
  // XP needed to reach each level, starting at level 2
  static readonly int[] _fighterXP = [2000, 4000, 7000, 10_000, 15_000, 20_000, 30_000, 50_000, 80_000, 110_000, 150_000];
  static readonly int[] _rogueXP = [1750, 3500, 6000, 9_000, 13_000, 17_500, 26_000, 43_000, 60_000, 95_000, 130_000];
  static readonly int[] _magicianXP = [2500, 5000, 9000, 13_000, 18_000, 23_000, 35_000, 60_000, 110_000, 160_000, 220_000];   // Conjurer, Magician
  static readonly int[] _sorcererXP = [7000, 15_000, 25_000, 40_000, 60_000, 80_000, 100_000, 130_000, 170_000, 220_000, 300_000];   // Sorcerer, Wizard

  static int[] _table(Role role) => role switch
  {
    Role.Warrior or Role.Paladin or Role.Archer => _fighterXP,
    Role.Rogue or Role.Hunter or Role.Trickster => _rogueXP,
    Role.Conjurer or Role.Magician => _magicianXP,
    Role.Sorcerer or Role.Wizard => _sorcererXP,
    _ => throw new Exception($"Unknown role: {role}!"),
  };

  static int XPForLevel(Role role, int level) => _table(role)[level - 2];

  public static int Needed(Role role, int level) => role switch
  {
    Role.Warrior or Role.Paladin or Role.Archer or Role.Rogue
      or Role.Hunter or Role.Trickster => level > 12 ? level * 200_000 : XPForLevel(role, level),
    Role.Conjurer or Role.Magician => level > 12 ? level * 220_000 : XPForLevel(role, level),
    Role.Sorcerer or Role.Wizard => level > 12 ? level * 300_000 : XPForLevel(role, level),
    _ => throw new Exception($"Unknown role: {role}!"),
  };
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

  public List<TempBuff> Buffs = [];

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

  public void PoolGold(Character c)
  {
    for (int j = 0; j < Party.MAX_PARTY_SIZE; j++)
    {
      if (Members[j] == c)
      {
        PoolGold(j);
        break;
      }
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

  public void RemoveAt(int slot)
  {
    for (int i = slot; i < MAX_PARTY_SIZE - 1; i++)
      Members[i] = Members[i + 1];
    Members[MAX_PARTY_SIZE - 1] = null;
  }

  public void MoveIncapacitatedToBack()
  {
    List<Character> active = [.. Members.OfType<Character>().Where(c => !c.Incapacitated)];
    List<Character> downed = [.. Members.OfType<Character>().Where(c => c.Incapacitated)];
    List<Character> ordered = [.. active, .. downed];

    for (int i = 0; i < MAX_PARTY_SIZE; i++)
      Members[i] = i < ordered.Count ? ordered[i] : null;
  }
}
