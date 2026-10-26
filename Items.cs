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
  const Role ALL = Role.Warrior | Role.Rogue | Role.Hunter | Role.Conjurer | Role.Magician
                      | Role.Sorcerer | Role.Wizard | Role.Trickster | Role.Archer | Role.Paladin;
  const Role NONCASTER = Role.Warrior | Role.Rogue | Role.Hunter | Role.Trickster | Role.Archer | Role.Paladin;
  const Role FIGHTER = Role.Warrior | Role.Archer | Role.Paladin;

  static Item ItemByNumber(int num) => num switch
  {
    2 => new() { Id = num, Name = "Broadsword", Type = ItemType.Weapon, Value = 80, DamageDie = 4, NumDamageDice = 2, UseableBy = FIGHTER | Role.Hunter },
    3 => new() { Id = num, Name = "Shortsword", Type = ItemType.Weapon, Value = 30, DamageDie = 6, NumDamageDice = 1, UseableBy = Role.Warrior | Role.Paladin | Role.Archer | Role.Hunter | Role.Rogue | Role.Trickster },
    5 => new() { Id = num, Name = "Dagger", Type = ItemType.Weapon, Value = 20, DamageDie = 4, NumDamageDice = 1, UseableBy = ALL },
    6 => new() { Id = num, Name = "Halbard", Type = ItemType.Weapon, Value = 200, DamageDie = 16, NumDamageDice = 1, UseableBy = FIGHTER | Role.Hunter },
    7 => new() { Id = num, Name = "Staff", Type = ItemType.Weapon, Value = 20, DamageDie = 6, NumDamageDice = 1, UseableBy = ALL },
    8 => new() { Id = num, Name = "Robes", Type = ItemType.Armour, Value = 40, ACMod = 1, UseableBy = ALL },
    9 => new() { Id = num, Name = "Leather Armour", Type = ItemType.Armour, Value = 70, ACMod = 2, UseableBy = NONCASTER },
    10 => new() { Id = num, Name = "Chain Mail", Type = ItemType.Armour, Value = 150, ACMod = 3, UseableBy = FIGHTER },
    11 => new() { Id = num, Name = "Splint Mail", Type = ItemType.Armour, Value = 300, ACMod = 4, UseableBy = FIGHTER },
    12 => new() { Id = num, Name = "Plate Armour", Type = ItemType.Armour, Value = 700, ACMod = 5, UseableBy = FIGHTER },
    13 => new() { Id = num, Name = "Gauntlets", Type = ItemType.Gloves, Value = 40, ACMod = 1, UseableBy = FIGHTER },
    14 => new() { Id = num, Name = "Leather Gloves", Type = ItemType.Gloves, Value = 80, ACMod = 1, UseableBy = NONCASTER },
    15 => new() { Id = num, Name = "Helm", Type = ItemType.Helmet, Value = 50, ACMod = 1, UseableBy = FIGHTER },
    16 => new() { Id = num, Name = "Buckler", Type = ItemType.Shield, Value = 40, ACMod = 1, UseableBy = NONCASTER },
    17 => new() { Id = num, Name = "Tower Shield", Type = ItemType.Shield, Value = 100, ACMod = 2, UseableBy = FIGHTER },
    18 => new() { Id = num, Name = "Mace", Type = ItemType.Weapon, Value = 60, DamageDie = 8, NumDamageDice = 1, UseableBy = NONCASTER },
    _ => new() {  Id = num, Name = "Torch", Type = ItemType.Misc, Value = 5, UseableBy = ALL }
  };
}