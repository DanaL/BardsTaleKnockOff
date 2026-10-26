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

using System.ComponentModel.Design.Serialization;

namespace BardsTaleKnockOff;

// Bard's Tale didn't really have unique items and such so this probably 
// suffices. I don't need to track individual objects like in a roguelike
// because swords can't become rusty, buffed, etc. At least not yet. So for
// the early stages of this project I'll just have a big lookup table.

enum ItemType { Misc, Weapon, Armour, Gloves, Shield, Helmet, Ring }

class Item
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
}

class ItemCatalog
{
  public const int TORCH = 0;

  public const int BROADSWORD = 1;
  public const int SHORTSWORD = 2;
  public const int DAGGER = 5;
  public const int HALBERD = 6;
  public const int STAFF = 7;
  public const int MACE = 18;

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
  
  const Role ALL = Role.Warrior | Role.Rogue | Role.Hunter | Role.Conjurer | Role.Magician
                      | Role.Sorcerer | Role.Wizard | Role.Trickster | Role.Archer | Role.Paladin;
  const Role NONCASTER = Role.Warrior | Role.Rogue | Role.Hunter | Role.Trickster | Role.Archer | Role.Paladin;
  const Role FIGHTER = Role.Warrior | Role.Archer | Role.Paladin;

  public static Item Item(int num) => num switch
  {
    TORCH => new() {  Id = TORCH, Name = "Torch", Type = ItemType.Misc, Value = 5, UseableBy = ALL },
    BROADSWORD => new() { Id = BROADSWORD, Name = "Broadsword", Type = ItemType.Weapon, Value = 80, DamageDie = 4, NumDamageDice = 2, UseableBy = FIGHTER | Role.Hunter },
    SHORTSWORD => new() { Id = SHORTSWORD, Name = "Shortsword", Type = ItemType.Weapon, Value = 30, DamageDie = 6, NumDamageDice = 1, UseableBy = Role.Warrior | Role.Paladin | Role.Archer | Role.Hunter | Role.Rogue | Role.Trickster },
    DAGGER => new() { Id = DAGGER, Name = "Dagger", Type = ItemType.Weapon, Value = 20, DamageDie = 4, NumDamageDice = 1, UseableBy = ALL },
    HALBERD => new() { Id = HALBERD, Name = "Halberd", Type = ItemType.Weapon, Value = 200, DamageDie = 16, NumDamageDice = 1, UseableBy = FIGHTER | Role.Hunter },
    STAFF => new() { Id = STAFF, Name = "Staff", Type = ItemType.Weapon, Value = 30, DamageDie = 6, NumDamageDice = 1, UseableBy = ALL },
    ROBES => new() { Id = ROBES, Name = "Robes", Type = ItemType.Armour, Value = 40, ACMod = 1, UseableBy = ALL },
    LEATHER_ARMOUR => new() { Id = LEATHER_ARMOUR, Name = "Leather Armour", Type = ItemType.Armour, Value = 70, ACMod = 2, UseableBy = NONCASTER },
    CHAIN_MAIL => new() { Id = CHAIN_MAIL, Name = "Chain Mail", Type = ItemType.Armour, Value = 150, ACMod = 3, UseableBy = FIGHTER },
    SPLINT_MAIL => new() { Id = SPLINT_MAIL, Name = "Splint Mail", Type = ItemType.Armour, Value = 300, ACMod = 4, UseableBy = FIGHTER },
    PLATE_ARMOUR => new() { Id = PLATE_ARMOUR, Name = "Plate Armour", Type = ItemType.Armour, Value = 700, ACMod = 5, UseableBy = FIGHTER },
    GAUNTLETS => new() { Id = GAUNTLETS, Name = "Gauntlets", Type = ItemType.Gloves, Value = 40, ACMod = 1, UseableBy = FIGHTER },
    LEATHER_GLOVES => new() { Id = LEATHER_GLOVES, Name = "Leather Gloves", Type = ItemType.Gloves, Value = 80, ACMod = 1, UseableBy = NONCASTER },
    HELM => new() { Id = HELM, Name = "Helm", Type = ItemType.Helmet, Value = 50, ACMod = 1, UseableBy = FIGHTER },
    BUCKLER => new() { Id = BUCKLER, Name = "Buckler", Type = ItemType.Shield, Value = 40, ACMod = 1, UseableBy = NONCASTER },
    TOWER_SHIELD => new() { Id = TOWER_SHIELD, Name = "Tower Shield", Type = ItemType.Shield, Value = 100, ACMod = 2, UseableBy = FIGHTER },
    MACE => new() { Id = MACE, Name = "Mace", Type = ItemType.Weapon, Value = 60, DamageDie = 8, NumDamageDice = 1, UseableBy = NONCASTER },
    _ => throw new Exception($"Item {num} is not defined!")
  };
}