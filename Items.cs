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

namespace BardsTaleKnockOff;

// Bard's Tale didn't really have unique items and such so this probably 
// suffices. I don't need to track individual objects like in a roguelike
// because swords can't become rusty, buffed, etc. At least not yet. So for
// the early stages of this project I'll just have a big lookup table.

enum ItemType { Misc, Weapon, Armour, Gloves, Shield, Helmet, Ring, Bow, Ammunition, Quest }

record Item
{
  public required int Id { get; init; }
  public required string Name { get; init; }
  public required ItemType Type { get; init; }
  public required int Value { get; init; }
  public required Role UseableBy { get; init; }
  public int ACMod { get; init; } = 0;
  public int AttackBonus { get; init; } = 0;
  public int NumDamageDice { get; init; } = 0;
  public int DamageDie { get; init; } = 0;
  public int DamageBonus { get; init; } = 0;
  public bool Useable { get; init; } = false;
  public bool Stackable { get; init; } = false;

  public int SellValue => int.Max(Value / 4, 1);
}

class ItemCatalog
{
  public const int UNIQUE_ITEM_ID_SEED = 10_000;

  // Quest/unique items that are created per campaign
  public static Dictionary<int, Item> UniqueItems { get; private set; } = [];
  static int _nextUniqueId = UNIQUE_ITEM_ID_SEED;

  public static void LoadUniqueItems(Dictionary<int, Item>? items)
  {
    UniqueItems = items ?? [];
    _nextUniqueId = UniqueItems.Count == 0 ? UNIQUE_ITEM_ID_SEED : UniqueItems.Keys.Max() + 1;
  }

  public static Item AddUniqueItem(Item template)
  {
    Item item = template with { Id = _nextUniqueId++ };
    UniqueItems[item.Id] = item;

    return item;
  }

  public const int TORCH = 0;

  public const int BROADSWORD = 1;
  public const int SHORTSWORD = 2;
  public const int DAGGER = 5;
  public const int HALBERD = 6;
  public const int STAFF = 7;
  public const int MACE = 18;
  public const int WARAXE = 22;

  public const int ROBES = 8;
  public const int LEATHER_ARMOUR = 9;
  public const int CHAIN_MAIL = 10;
  public const int SPLINT_MAIL = 11;
  public const int PLATE_ARMOUR = 12;

  public const int GAUNTLETS = 13;
  public const int LEATHER_GLOVES = 14;
  
  public const int HELM = 15;
  
  public const int BUCKLER = 16;
  public const int TOWER_SHIELD = 17;
  
  public const int SHORTBOW = 19;
  public const int LONGBOW = 20;

  public const int ARROW = 21;

  public const int CORRODED_BROADSWORD = 23;
  public const int CORRODED_SHORTSWORD = 24;
  public const int CORRODED_DAGGER = 25;
  public const int CORRODED_HALBERD = 26;
  public const int CORRODED_MACE = 27;
  public const int CORRODED_WARAXE = 28;
  public const int CORRODED_CHAIN_MAIL = 29;
  public const int CORRODED_SPLINT_MAIL = 30;
  public const int CORRODED_PLATE_ARMOUR = 31;
  public const int CORRODED_GAUNTLETS = 32;
  public const int CORRODED_HELM = 33;
  public const int CORRODED_BUCKLER = 34;
  public const int CORRODED_TOWER_SHIELD = 35;

  public const int WOLF_FGN = 36;

  const Role ALL = Role.Warrior | Role.Rogue | Role.Hunter | Role.Conjurer | Role.Magician
                      | Role.Sorcerer | Role.Wizard | Role.Trickster | Role.Archer | Role.Paladin;
  const Role NONCASTER = Role.Warrior | Role.Rogue | Role.Hunter | Role.Trickster | Role.Archer | Role.Paladin;
  const Role FIGHTER = Role.Warrior | Role.Archer | Role.Paladin;

  public static Item Item(int id) => id switch
  {
    TORCH => new() {  Id = TORCH, Name = "Torch", Type = ItemType.Misc, Value = 5, UseableBy = ALL, Useable = true },
    BROADSWORD => new() { Id = BROADSWORD, Name = "Broadsword", Type = ItemType.Weapon, Value = 80, DamageDie = 4, NumDamageDice = 2, UseableBy = FIGHTER | Role.Hunter },
    SHORTSWORD => new() { Id = SHORTSWORD, Name = "Shortsword", Type = ItemType.Weapon, Value = 30, DamageDie = 6, NumDamageDice = 1, UseableBy = Role.Warrior | Role.Paladin | Role.Archer | Role.Hunter | Role.Rogue | Role.Trickster },
    WARAXE => new() { Id = WARAXE, Name = "War axe", Type = ItemType.Weapon, Value = 70, DamageDie = 8, NumDamageDice = 1, UseableBy = FIGHTER | Role.Hunter },
    DAGGER => new() { Id = DAGGER, Name = "Dagger", Type = ItemType.Weapon, Value = 20, DamageDie = 4, NumDamageDice = 1, UseableBy = ALL },
    HALBERD => new() { Id = HALBERD, Name = "Halberd", Type = ItemType.Weapon, Value = 200, DamageDie = 16, NumDamageDice = 1, UseableBy = FIGHTER | Role.Hunter },
    STAFF => new() { Id = STAFF, Name = "Staff", Type = ItemType.Weapon, Value = 30, DamageDie = 6, NumDamageDice = 1, UseableBy = ALL },
    ROBES => new() { Id = ROBES, Name = "Robes", Type = ItemType.Armour, Value = 40, ACMod = 1, UseableBy = ALL },
    LEATHER_ARMOUR => new() { Id = LEATHER_ARMOUR, Name = "Leather Armour", Type = ItemType.Armour, Value = 70, ACMod = 2, UseableBy = NONCASTER },
    CHAIN_MAIL => new() { Id = CHAIN_MAIL, Name = "Chain Mail", Type = ItemType.Armour, Value = 150, ACMod = 3, UseableBy = FIGHTER | Role.Hunter },
    SPLINT_MAIL => new() { Id = SPLINT_MAIL, Name = "Splint Mail", Type = ItemType.Armour, Value = 300, ACMod = 4, UseableBy = FIGHTER },
    PLATE_ARMOUR => new() { Id = PLATE_ARMOUR, Name = "Plate Armour", Type = ItemType.Armour, Value = 700, ACMod = 5, UseableBy = FIGHTER },
    GAUNTLETS => new() { Id = GAUNTLETS, Name = "Gauntlets", Type = ItemType.Gloves, Value = 40, ACMod = 1, UseableBy = FIGHTER },
    LEATHER_GLOVES => new() { Id = LEATHER_GLOVES, Name = "Leather Gloves", Type = ItemType.Gloves, Value = 80, ACMod = 1, UseableBy = NONCASTER },
    HELM => new() { Id = HELM, Name = "Helm", Type = ItemType.Helmet, Value = 50, ACMod = 1, UseableBy = FIGHTER },
    BUCKLER => new() { Id = BUCKLER, Name = "Buckler", Type = ItemType.Shield, Value = 40, ACMod = 1, UseableBy = NONCASTER },
    TOWER_SHIELD => new() { Id = TOWER_SHIELD, Name = "Tower Shield", Type = ItemType.Shield, Value = 100, ACMod = 2, UseableBy = FIGHTER },
    MACE => new() { Id = MACE, Name = "Mace", Type = ItemType.Weapon, Value = 60, DamageDie = 8, NumDamageDice = 1, UseableBy = NONCASTER },
    SHORTBOW => new() { Id = SHORTBOW, Name = "Shortbow", Type = ItemType.Bow, Value = 50, DamageDie = 6, NumDamageDice = 1, UseableBy = NONCASTER },
    LONGBOW => new() { Id = LONGBOW, Name = "Longbow", Type = ItemType.Bow, Value = 100, DamageDie = 8, NumDamageDice = 1, UseableBy = Role.Warrior | Role.Archer },
    ARROW => new() { Id = ARROW, Name = "Arrow", Type = ItemType.Ammunition, Value = 2, UseableBy = NONCASTER, Useable = true, Stackable = true },
    WOLF_FGN => new() { Id = WOLF_FGN, Name = "Wolf Figurine", Type = ItemType.Misc, Value = 150, UseableBy = ALL, Useable = true, Stackable = false },

    CORRODED_BROADSWORD => new() { Id = CORRODED_BROADSWORD, Name = "Corroded Broadsword", AttackBonus = -1, Type = ItemType.Weapon, Value = 20, DamageDie = 4, NumDamageDice = 1, UseableBy = FIGHTER | Role.Hunter },
    CORRODED_SHORTSWORD => new() { Id = CORRODED_SHORTSWORD, Name = "Corroded Shortsword", AttackBonus = -1, Type = ItemType.Weapon, Value = 8, DamageDie = 3, NumDamageDice = 1, UseableBy = Role.Warrior | Role.Paladin | Role.Archer | Role.Hunter | Role.Rogue | Role.Trickster },
    CORRODED_WARAXE => new() { Id = CORRODED_WARAXE, Name = "Corroded War axe", AttackBonus = -1, Type = ItemType.Weapon, Value = 16, DamageDie = 4, NumDamageDice = 1, UseableBy = FIGHTER | Role.Hunter },
    CORRODED_DAGGER => new() { Id = CORRODED_DAGGER, Name = "Corroded Dagger", AttackBonus = -1, Type = ItemType.Weapon, Value = 5, DamageDie = 2, NumDamageDice = 1, UseableBy = ALL },
    CORRODED_HALBERD => new() { Id = CORRODED_HALBERD, Name = "Corroded Halberd", AttackBonus = -1, Type = ItemType.Weapon, Value = 50, DamageDie = 8, NumDamageDice = 1, UseableBy = FIGHTER | Role.Hunter },
    CORRODED_CHAIN_MAIL => new() { Id = CORRODED_CHAIN_MAIL, Name = "Corroded Chain Mail", Type = ItemType.Armour, Value = 35, ACMod = 2, UseableBy = FIGHTER | Role.Hunter },
    CORRODED_SPLINT_MAIL => new() { Id = CORRODED_SPLINT_MAIL, Name = "Corroded Splint Mail", Type = ItemType.Armour, Value = 75, ACMod = 3, UseableBy = FIGHTER },
    CORRODED_PLATE_ARMOUR => new() { Id = CORRODED_PLATE_ARMOUR, Name = "Corroded Plate Armour", Type = ItemType.Armour, Value = 125, ACMod = 4, UseableBy = FIGHTER },
    CORRODED_GAUNTLETS => new() { Id = CORRODED_GAUNTLETS, Name = "Corroded Gauntlets", Type = ItemType.Gloves, Value = 10, ACMod = 0, UseableBy = FIGHTER },
    CORRODED_HELM => new() { Id = CORRODED_HELM, Name = "Corroded Helm", Type = ItemType.Helmet, Value = 12, ACMod = 0, UseableBy = FIGHTER },
    CORRODED_BUCKLER => new() { Id = CORRODED_BUCKLER, Name = "Corroded Buckler", Type = ItemType.Shield, Value = 10, ACMod = 0, UseableBy = NONCASTER },
    CORRODED_TOWER_SHIELD => new() { Id = CORRODED_TOWER_SHIELD, Name = "Corroded Tower Shield", Type = ItemType.Shield, Value = 25, ACMod = 1, UseableBy = FIGHTER },
    CORRODED_MACE => new() { Id = CORRODED_MACE, Name = "Corroded Mace", AttackBonus = -1, Type = ItemType.Weapon, Value = 15, DamageDie = 4, NumDamageDice = 1, UseableBy = NONCASTER },

    _ when UniqueItems.TryGetValue(id, out var item) => item,
    _ => throw new Exception($"Item {id} is not defined!")
  };
  
  public static bool Use(Item item, GameState gs, Character user, Queue<string> messages, Rng rng)
  {
    bool consumed = false;

    if (item.Id == TORCH)
    {
      gs.Party.ClearLightSources();
      gs.Party.Buffs.Add(new TempBuff(Buff.Torch, gs.Turn + 2000, false));      
      consumed = true;
    }
    else if (item.Id == WOLF_FGN)
    {
      messages.Enqueue($"{user.Name} summons a wolf!");
      gs.Party.Special = MonsterCatalog.Get(Monsters.Wolf, rng);
      consumed = true;
    }

    return consumed;    
  }

  public static int CorrodedId(int itemId)  => itemId switch 
  {
    BROADSWORD => CORRODED_BROADSWORD,
    SHORTSWORD => CORRODED_SHORTSWORD,
    WARAXE => CORRODED_WARAXE, 
    DAGGER => CORRODED_DAGGER,
    HALBERD => CORRODED_HALBERD,
    CHAIN_MAIL => CORRODED_CHAIN_MAIL,
    SPLINT_MAIL => CORRODED_SPLINT_MAIL,
    PLATE_ARMOUR => CORRODED_PLATE_ARMOUR,
    GAUNTLETS => CORRODED_GAUNTLETS,
    HELM => CORRODED_HELM,
    BUCKLER => CORRODED_BUCKLER,
    TOWER_SHIELD => CORRODED_TOWER_SHIELD,
    MACE => CORRODED_MACE,
    _ => itemId
  };
}