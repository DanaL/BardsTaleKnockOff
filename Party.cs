// {BardsTaleKnockoff} - A roguelike computer RPG
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
enum Role { Warrior, Rogue, Hunter, Conjurer, Magician }

// Placeholder character model
class Character
{
  public required string Name { get; init; }
  public required Lineage Lineage { get; init; }
  public required Role Role { get; init; }
  public int AC { get; set; }
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

  public static int StatToMod(int stat) => (int) Math.Floor((stat - 10.0) / 2);
}

class Party
{
  public const int MaxMembers = 6;

  // The roster's "S" slot, shown above the six regular members.
  public Character? Special { get; set; }
  public Character?[] Members { get; } = new Character?[MaxMembers];

  public int Count => Members.Count(m => m is not null);
  public bool IsFull => Count == MaxMembers;
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

  // Remove the member in a slot (0-based) and close the gap, so members
  // always fill slots 1, 2, 3... with no holes.
  public void RemoveAt(int slot)
  {
    for (int i = slot; i < MaxMembers - 1; i++)
      Members[i] = Members[i + 1];
    Members[MaxMembers - 1] = null;
  }

  //public static List<Character> SampleRoster() => [.. Sample().Members.OfType<Character>()];

  // public static Party Sample()
  // {
  //   var party = new Party();
  //   party.Members[0] = new() { Name = "Thorvald", ClassName = "Warrior", AC = 4, HP = 28, MaxHP = 28 };
  //   party.Members[1] = new() { Name = "Elspeth", ClassName = "Paladin", AC = 3, HP = 19, MaxHP = 24 };
  //   party.Members[2] = new() { Name = "Pip", ClassName = "Rogue", AC = 7, HP = 12, MaxHP = 15, Condition = "Poisoned" };
  //   party.Members[3] = new() { Name = "Melisande", ClassName = "Bard", AC = 8, HP = 14, MaxHP = 14 };
  //   party.Members[4] = new() { Name = "Aldric the Wise", ClassName = "Conjurer", AC = 10, HP = 9, MaxHP = 9, MP = 11, MaxMP = 16 };

  //   return party;
  // }
}
