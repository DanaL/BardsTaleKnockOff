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

enum Trap { Darts0, PoisonNeedle0, Blades0, }

class Traps
{
  public static List<string> Blades0(GameState gs, Rng rng)
  {
    List<string> msgs = [ "Blades!" ];

    foreach (Character c in gs.Party.Members.OfType<Character>())
    {
      if (c.Incapacitated)
        continue;

      int dmg = rng.Next(1, 5) + rng.Next(1, 5);
      SaveResult save = c.RollSavingThrow(1, rng);
      if (save == SaveResult.Success) 
      {
        msgs.Add($"{c.Name} avoids the trap!");
        continue;
      }        
      else if (save == SaveResult.Partial)
      {
        dmg /= 2;
      }

      msgs.Add($"{c.Name} is hit for {dmg} hp!");
      string s = c.ReceiveDmg(dmg);
      if (!string.IsNullOrEmpty(s))
        msgs.Add(s);
    }

    return msgs;
  }

  public static List<string> Darts0(Character? victim, GameState gs, Rng rng)
  {
    List<string> msgs = [];    
    Character c;
    if (victim is not null) 
    {
      c = victim;
    }
    else
    {
      List<Character> pcs = [];
      for (int j = 0; j < gs.Party.Members.Length; j++)
      {
        if (gs.Party.Members[j] is Character pc && !pc.Incapacitated)
          pcs.Add(pc);
      }

      if (pcs.Count == 0)
        return msgs;

      c = pcs[rng.Next(pcs.Count)];
    }

    msgs.Add($"A trap! Darts fly at {c.Name}!");

    int dmg = rng.Next(1, 5) + rng.Next(1, 5) + rng.Next(1, 5) + rng.Next(1, 5);
    SaveResult save = c.RollSavingThrow(1, rng);
    if (save == SaveResult.Success) 
    {
      msgs.Add("But they avoid being hit!");
      return msgs;      
    }        
    else if (save == SaveResult.Partial)
    {
      dmg /= 2;
    }

    msgs.Add($"They are hit for {dmg} hp!");
    string s = c.ReceiveDmg(dmg);
    if (!string.IsNullOrEmpty(s))
      msgs.Add(s);

    return msgs;
  }

  public static List<string> PoisonNeedles0(Character? victim, GameState gs, Rng rng)
  {
    List<string> msgs = [];    
    Character c;
    if (victim is not null) 
    {
      c = victim;
    }
    else
    {
      List<Character> pcs = [];
      for (int j = 0; j < gs.Party.Members.Length; j++)
      {
        if (gs.Party.Members[j] is Character pc && !pc.Incapacitated)
          pcs.Add(pc);
      }

      if (pcs.Count == 0)
        return msgs;

      c = pcs[rng.Next(pcs.Count)];
    }

    msgs.Add($"A trap! Poison needles shoot at {c.Name}!");

    int dmg = rng.Next(1, 5) + rng.Next(1, 5) + rng.Next(1, 5) + rng.Next(1, 5);
    SaveResult save = c.RollSavingThrow(1, rng);
    if (save == SaveResult.Success) 
    {
      msgs.Add("But they avoid being hit!");
      return msgs;      
    }        
    else if (save == SaveResult.Partial)
    {
      dmg /= 2;
    }

    msgs.Add($"They are hit for {dmg} hp!");
    msgs.Add($"They are poisoned!");
    c.Condition = "POISONED";
    
    string s = c.ReceiveDmg(dmg);
    if (!string.IsNullOrEmpty(s))
      msgs.Add(s);

    return msgs;
  }
}