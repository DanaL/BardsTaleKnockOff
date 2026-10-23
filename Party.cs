namespace BardsTaleKnockOff;

// Placeholder character model
class Character
{
  public required string Name { get; init; }
  public required string ClassName { get; init; }
  public int AC { get; set; }
  public int HP { get; set; }
  public int MaxHP { get; set; }
  public int MP { get; set; }
  public int MaxMP { get; set; }
  public string Condition { get; set; } = "";
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

  // Ready-made characters for the Guild's roster, so there's someone to
  // adventure with before character creation exists (Bard's Tale shipped
  // with a pre-made party too).
  public static List<Character> SampleRoster() => [.. Sample().Members.OfType<Character>()];

  public static Party Sample()
  {
    var party = new Party();
    party.Members[0] = new() { Name = "Thorvald", ClassName = "Warrior", AC = 4, HP = 28, MaxHP = 28 };
    party.Members[1] = new() { Name = "Elspeth", ClassName = "Paladin", AC = 3, HP = 19, MaxHP = 24 };
    party.Members[2] = new() { Name = "Pip", ClassName = "Rogue", AC = 7, HP = 12, MaxHP = 15, Condition = "Poisoned" };
    party.Members[3] = new() { Name = "Melisande", ClassName = "Bard", AC = 8, HP = 14, MaxHP = 14 };
    party.Members[4] = new() { Name = "Aldric the Wise", ClassName = "Conjurer", AC = 10, HP = 9, MaxHP = 9, MP = 11, MaxMP = 16 };
    
    return party;
  }
}
