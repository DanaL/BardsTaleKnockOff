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

// Use Item not implemented yet...
enum BattleAction { MeleeAttack, Defend, MonsterAction, HideInShadows, CastSpell, Advance }

record struct BattleChoice(int Slot, BattleAction Action,  int Group, int PartySlot, SpellInfo? Spell = null);

enum RoundResult { Continue, Victory }

// The rules of combat. BattleScreen collects the party's choices and paces
// the messages; this class decides what actually happens.
//
// A round is: StartRound() with the party's choices, then ResolveNextAction()
// repeatedly until RoundOver, then EndRound(). Each action is resolved on its
// own so the screen can show its messages before the next one changes the
// party or the monsters.
class Battle(GameState gs, Rng rng, List<List<Monster>> foes)
{
  public List<List<Monster>> Foes { get; private set; } = foes;

  // Messages describing what happened, for the screen to show in order
  public Queue<string> Messages { get; } = new();

  readonly PriorityQueue<BattleChoice, int> _actions = new();
  int _actingGroup = -1;
  int _actingMonster = 0;

  int _xpWon = 0;
  int _goldWon = 0;

  public bool RoundOver => _actions.Count == 0 && _actingGroup < 0;

  public void StartRound(IEnumerable<BattleChoice> choices)
  {
    foreach (var choice in choices)
    {
      if (gs.Party.Members[choice.Slot] is not Character c || c.Incapacitated)
        continue;

      if (choice.Action == BattleAction.Defend)
      {
        c.Buffs.Add(new TempBuff(Buff.Defending, gs.Turn, true));
        Messages.Enqueue($"{c.Name} defends.");
      }
      else if (choice.Action == BattleAction.MeleeAttack || choice.Action == BattleAction.CastSpell)
      {
        _actions.Enqueue(choice, Initiative(c));
      }
      else if (choice.Action == BattleAction.HideInShadows)
      {
        // Hide in shadows should happen before other actions
        _actions.Enqueue(choice, -int.MaxValue);
      }
    }

    int advInitiative = 1000; // groups advancing always happens after everyone else has acted
    for (int n = 0; n < Foes.Count; n++)
    {
      if (n < 2 || Foes[n][0].HasFlag(MonsterFlag.Ranged))
      {
        int roll = rng.Next(1, 24);
        _actions.Enqueue(new(n, BattleAction.MonsterAction, n, -1), -roll);        
      }
      else if (GroupsToPass(n, Foes[n].Count))
      {
        _actions.Enqueue(new(n, BattleAction.Advance, n, -1), advInitiative++);
      }      
    }

    bool GroupsToPass(int n, int size)
    {
      if (Foes[n-1].Count < size)
        return true;

      for (int j = 0; j < n; j++)
      {
        if (Foes[j][0].HasFlag(MonsterFlag.Ranged))
          return true;
      }
      
      return false;
    }
  }

  // Resolve a single attack. When a monster group's turn comes up, each
  // monster in it attacks on its own step. Nothing else acts until the
  // whole group has gone, so the indexes into the group stay valid.
  public void ResolveNextAction()
  {
    if (_actingGroup >= 0)
    {
      List<Monster> group = Foes[_actingGroup];
      if (_actingMonster < group.Count)
      {
        MonsterAction(group[_actingMonster++], _actingGroup);
        return;
      }

      _actingGroup = -1;
    }

    if (_actions.Count == 0)
      return;

    var choice = _actions.Dequeue();
    if (choice.Action == BattleAction.MonsterAction)
    {
      _actingGroup = choice.Group;
      _actingMonster = 0;
    }
    else if (choice.Action == BattleAction.Advance)
    {
      AdvanceGroup(choice.Group);
    }
    else
    {
      if (gs.Party.Members[choice.Slot] is not Character c || c.Incapacitated)
        return;

      switch (choice.Action)
      {
        case BattleAction.MeleeAttack:
          PlayerMeleeAttack(c, choice.Group);
          break;
        case BattleAction.HideInShadows:
          int roll = rng.Next(0, 100) + 1;
          if (roll <= c.HideInShadows)
          {
            Messages.Enqueue($"{c.Name} slips into the shadows!");
            c.Buffs.Add(new TempBuff(Buff.Hiding, ulong.MaxValue, true));
          }
          else
          {
            Messages.Enqueue($"{c.Name} fails to hide!");
            c.Buffs = [.. c.Buffs.Where(b => b.Buff != Buff.Hiding )];
          }
          break;
        case BattleAction.CastSpell:
          if (choice.Spell is SpellInfo spell)
          {
            Character? partyTarget = choice.PartySlot >= 0 ? gs.Party.Members[choice.PartySlot] : null;
            string s = $"{c.Name} casts {spell.Code}";
            if (choice.Spell.Target == SpellTarget.PartyMember && partyTarget is not null)
            {
              s += $" on {partyTarget.Name}";
            }
            else if (choice.Spell.Target == SpellTarget.Monsters && Foes[choice.Group].Count > 0)
            {
              var g = Foes[choice.Group];
              string mn = g.Count == 1 ? g[0].Name.DefArticle() : $"some {g[0].Name.Pluralize()}";
              s += $" on {mn}";
            }
              
            s += ".";
            Messages.Enqueue(s);

            if (c.MP < choice.Spell.Cost) 
            {
              Messages.Enqueue($"But they have insufficient Magic Points!");
            }
            else
            {
              SpellResolver sr = new(Foes, this);
              var msgs = sr.Cast(choice.Spell, c, rng, choice.Group, partyTarget, gs);
              foreach (string m in msgs)
                Messages.Enqueue(m);
            }
          }
          break;
      }
    }
  }

  // Advance the clock, expire buffs, move the fallen to the back and drop
  // any monster groups that have been wiped out.
  public RoundResult EndRound()
  {    
    if (gs.Party.Alive == 0)
    {
      // Uh-oh, TPK
    }

    foreach (Character member in gs.Party.Members.OfType<Character>())
    {
      member.Buffs = [.. member.Buffs.Where(b => b.ExpiresOn > gs.Turn)];
    }
    foreach (var group in Foes)
    {
      foreach (var monster in group)
        monster.Buffs = [.. monster.Buffs.Where(b => b.ExpiresOn > gs.Turn)];
    }

    ++gs.Turn;

    gs.Party.MoveIncapacitatedToBack();

    Foes = [.. Foes.Where(g => g.Count > 0)];

    return Foes.Count == 0 ? RoundResult.Victory : RoundResult.Continue;
  }

  // Share out the XP and gold won among the survivors. Returns each
  // survivor's share.
  // Eventually, I need to add finding the occasional item!
  public (int XP, int Gold) AwardSpoils()
  {
    // If gold given is less than the # of alive characters, I'm giving a
    // little extra gold but who cares...
    int goldPerChar = int.Max(_goldWon / gs.Party.Alive, 1);
    int xpPerChar = int.Max(_xpWon / gs.Party.Alive, 1);

    for (int j = 0; j < gs.Party.Count; j++)
    {
      if (gs.Party.Members[j] is Character c && !c.Incapacitated)
      {
        c.XP += xpPerChar;
        c.Gold += goldPerChar;
      }
    }

    return (xpPerChar, goldPerChar);
  }

  void AdvanceGroup(int groupNum)
  {
    var group = Foes[groupNum];
    string s = group.Count > 1 ? group[0].Name.Pluralize() : group[0].Name;

    (Foes[groupNum], Foes[groupNum - 1]) = (Foes[groupNum - 1], Foes[groupNum]);
    
    Messages.Enqueue($"{s.DefArticle().Capitalize()} advance!");
  }

  void MonsterAction(Monster m, int group)
  {
    if (m.Buffs.Any(b => b.Buff == Buff.Hypnotized))
    {
      Messages.Enqueue($"{m.Name.IndefArticle().Capitalize()} is mesmerized.");
      // Check this here so that if a monster acts the same round they had HYIM
      // cast on them but after the casting, they don't miss two turns
      m.Buffs = [.. m.Buffs.Where(b => b.Buff != Buff.Hypnotized)];
      return;  
    }

    // Have to take into account the Special slot eventually
    List<int> targets = [];    
    for (int j = 0; j < gs.Party.Count; j++)
    {
      if (gs.Party.Members[j] is Character c && !c.Incapacitated && !c.Buffs.Any(b => b.Buff == Buff.Hiding))
        targets.Add(j);
    }

    if (targets.Count == 0)
      return;

    List<int> meleeTargets = [.. targets.Where(n => n < 3)];

    List<(string Action, int Target)> options = [];
    if (group < 2)
    {
      options.Add(("melee", meleeTargets[rng.Next(meleeTargets.Count)]));
    }
    
    options.AddRange(ExtraOptions(m.Script, targets, m));
    
    if (options.Count == 0)
      return;

    (string action, int targetNum) = options[rng.Next(options.Count)];
    if (gs.Party.Members[targetNum] is not Character target)
      return;

    switch (action)
    {
      case "melee":
        MonsterMeleeAttack(m, target);
        break;
      case "ARFI_1":
        MonsterCastArcFire(m, target, 1);
        break;
      case "FRFO":
        MonsterCaseFreezeFoes(m);
        break;
      case "WIST":
        MonsterCastWitherStrike(m, target);
        break;
      case "ZAP":
        MonsterCastZap(m, target);
        break;
      case "INVI":
        Messages.Enqueue($"{m.Name.DefArticle().Capitalize() } casts Invisibility!");
        if (!m.Buffs.Any(b => b.Buff == Buff.Invisibility))
        {
          m.Buffs.Add(new TempBuff(Buff.Invisibility, ulong.MaxValue, true));
        }
        break;
    }
  }

  List<(string, int)> ExtraOptions(MonsterScript script, List<int> targets, Monster monster)
  {
    List<(string, int)> options;
    switch (script)
    {
      case MonsterScript.Conjuror0:
        options = [ ("ARFI_1", targets[rng.Next(targets.Count)]) ];
        if (rng.Next(3) >= 0)
          options.Add(("FRFO", 0));
        break;
      case MonsterScript.Magician0:
        // In og BT1, I don't remember Magicians casting WIST very often or 
        // it being a big problem in the early game so I'll make it somewhat
        // uncommon
        options = [ ("ZAP", targets[rng.Next(targets.Count)]) ];
        if (rng.Next(3) == 0 && !monster.Buffs.Any(b => b.Buff == Buff.Invisibility))
          options.Add(("INVI", 0));
        if (rng.Next(7) == 0)
          options.Add(("WIST", targets[rng.Next(targets.Count)]));
        break;
      default:
        options = [];
        break;
    }

    return options;
  }

  void MonsterCaseFreezeFoes(Monster m)
  {
    Messages.Enqueue($"{m.Name.IndefArticle().Capitalize() } casts Freeze Foes the party!");

    foreach (var c in gs.Party.Members.OfType<Character>())
    {
      if (c.Incapacitated)
        continue;
      
      if (c.Buffs.Any(b => b.Buff == Buff.FreezeFoe))
        continue;

      if (c.RollSavingThrow(m.Rank, rng) == SaveResult.Success)
      {
        Messages.Enqueue($"{c.Name} resists!");
      }
      else
      {
        Messages.Enqueue($"{c.Name} becomes enervated!");
        c.Buffs.Add(new TempBuff(Buff.FreezeFoe, ulong.MaxValue, true));
      }
    }
  }

  // It's a little redundant to duplicate a bunch of the spell cast code, but
  // monsters don't really have that many abilities and this keeps the methods
  // pretty simple

  void MonsterCastWitherStrike(Monster m, Character target)
  {
    Messages.Enqueue($"{m.Name.IndefArticle().Capitalize() } casts Wither Strike on {target.Name}!");
    if (target.Condition == "OLD")
      return;
    
    if (target.RollSavingThrow(m.Rank, rng) == SaveResult.Success)
    {
      Messages.Enqueue($"...but they resist!");
    }
    else
    {
      Messages.Enqueue($"{target.Name} is Withered!");
      target.Condition = "OLD";
    }    
  }

  void ResolveMonsterDmgSpell(Monster m, Character target, string msg, int dmg)
  {
    var save = target.RollSavingThrow(m.Rank, rng);
    if (save == SaveResult.Success)
    {
      msg += " but they resist!";
      Messages.Enqueue(msg);
      return;
    }
    
    if (save == SaveResult.Partial) 
    {
      dmg = int.Max(1, dmg / 2);
    }
    msg += $" for {dmg} hp!";
    Messages.Enqueue(msg);

    string s = target.ReceiveDmg(dmg);
    if (s != "")
      Messages.Enqueue(s);
  }

  void MonsterCastArcFire(Monster m, Character target, int level)
  {
    int dmg = rng.Next(1, 5) * level;
    
    string msg = $"{m.Name.DefArticle().Capitalize() } casts Arc Fire on {target.Name}";
    ResolveMonsterDmgSpell(m, target, msg, dmg);
  }

  void MonsterCastZap(Monster m, Character target)
  {
    int dmg = rng.Next(1, 5);
    
    string msg = $"{m.Name.DefArticle().Capitalize() } zaps {target.Name}";
    ResolveMonsterDmgSpell(m, target, msg, dmg);
  }

  void MonsterMeleeAttack(Monster m, Character target)
  {    
    string n = Util.CalcName(m).DefArticle().Capitalize();
    int roll = rng.Next(1, 21);
    int toHitTarget = m.THAC0 - target.AC;

    foreach (var buff in target.Buffs)
    {
      if (buff.Buff == Buff.Defending) // Not sure what BT1 did, but -5 is roughly equip to Disadvantage in 5e
        toHitTarget += 5;                 // and what you get for taking the Dodge action
      else if (buff.Buff == Buff.FreezeFoe)
        toHitTarget -= 2; // Guess at what BT1 did
      else if (buff.Buff == Buff.Invisibility)
        toHitTarget += 5;
      else if (buff.Buff == Buff.PhaseBlur)
        toHitTarget += 2;
    }

    if (target.Condition == "OLD")
      toHitTarget -= 5;

    if (m.Condition == "OLD")
      roll -= 10;

    if (roll >= toHitTarget)
    {
      int dmg = rng.Next(m.DmgLow, m.DmgHigh + 1);
      if (m.Condition == "OLD")
        dmg /= 4;
      Messages.Enqueue($"{n} {m.Verb} {target.Name} and hits for {dmg} hp!");
      string s = target.ReceiveDmg(dmg);
      if (s != "")
        Messages.Enqueue(s);
    }
    else
    {
      Messages.Enqueue($"{n} {m.Verb} {target.Name} and misses!");
    }
  }

  void PlayerMeleeAttack(Character c, int monsterGroup)
  {
    if (Foes[monsterGroup].Count == 0)
      return;

    Monster target = Foes[monsterGroup][0];
    Item? weapon = c.ReadiedWeapon;
     string s = weapon is not null ? $"{c.Name} attacks " : $"{c.Name} punches ";
     s += target.Name.IndefArticle().ToLower();
     int attacks = c.NumOfMeleeAttacks;
     if (attacks > 1)
      s += $" {attacks} times ";

    int strMod = Character.StatToMod(c.Str);

    int hits = 0;
    int dmg = 0;
    bool hiding = false;
    bool vorpalPlating = false;
    bool mageGauntlets = false;
    bool battleSkill = false;
    foreach (var buff in c.Buffs)
    {
      if (buff.Buff == Buff.Hiding)
        hiding = true;
      else if (buff.Buff == Buff.VorpalPlating)
        vorpalPlating = true;
      else if (buff.Buff == Buff.MageGauntlets)
        mageGauntlets = true;
      else if (buff.Buff == Buff.BattleSkill)
        battleSkill = true;
    }

    for (int j = 0; j < c.NumOfMeleeAttacks; j++)
    {
      int roll = rng.Next(1, 21) + strMod;
      if (weapon is not null)
        roll += weapon.AttackBonus;

      if (hiding)
        roll += 3;

      if (c.Condition == "OLD")
         roll -= 10;

      if (battleSkill)
        roll += 4;

      int toHitTarget = c.THAC0 - target.AC;
      foreach (var buff in target.Buffs)
      {
        if (buff.Buff == Buff.FreezeFoe)
          toHitTarget -= 2;
        else if (buff.Buff == Buff.Invisibility)
          toHitTarget += 5;
      }

      if (roll >= toHitTarget)
      {
        ++hits;
        dmg += strMod;

        if (vorpalPlating)
          dmg += rng.Next(2, 9);
        if (mageGauntlets)
          dmg += rng.Next(4, 17);
        if (battleSkill)
          dmg += 4;
          
        if (weapon is null) 
        {
          dmg += 1;
        }
        else
        {
          dmg += weapon.DamageBonus;
          for (int k = 0; k < weapon.NumDamageDice; k++)
            dmg += rng.Next(weapon.DamageDie) + 1;
        }
      }
    }

    if (c.Condition == "OLD")
      dmg /= 4;

    bool criticalHit = hits > 0 && (hiding || (c.CritChance > 0 && rng.Next(256) < c.CritChance));
      
    if (hiding)
      s = $"{c.Name} leaps from the shadows";

    string targetName = target.Name.DefArticle().ToLower();
    if (hits == 0)
      s += " but misses!";
    else if (criticalHit)
      s += $" and strikes {targetName}, killing it instantly!";
    else if (hits == 1)
      s+= $" and hits for {dmg} dmg!";
    else
      s+= $" and hits {hits} times for {dmg} dmg!";

    Messages.Enqueue(s);

    // If you attack from hiding, you are no longer hiding
    c.Buffs = [.. c.Buffs.Where(b => b.Buff != Buff.Hiding)];

    int monsterHit = rng.Next(Foes[monsterGroup].Count);
    if (criticalHit)
      KillMonster(monsterGroup, monsterHit, gs);
    else
      DamageMonster(monsterGroup, monsterHit, dmg, gs, Messages);
  }

  // Killing a monster shifts the indexes of the ones after it in the group,
  // so when damaging a whole group, loop from the back.
  public bool DamageMonster(int group, int targetNum, int dmg, GameState gs, Queue<string> msgQ)
  {
    Monster m = Foes[group][targetNum];
    if (dmg >= m.HP)
    {
      msgQ.Enqueue($"{m.Name.DefArticle().ToLower().Capitalize()} is killed!");
      KillMonster(group, targetNum, gs);
      return true;
    }

    if (dmg > 0)
      m.HP -= dmg;

    return false;
  }

  public void KillMonster(int group, int index, GameState gs)
  {
    Monster m = Foes[group][index];
    _xpWon += m.XP;
    _goldWon += m.Gold;
    Foes[group].RemoveAt(index);

    if (m.DefeatFact is Fact fact)
      gs.FactDb[fact.Key] = fact;
  }

  int Initiative(Character c)
  {
    int roll = rng.Next(20) + 1 + Character.StatToMod(c.Dex);
    switch (c.Role)
    {
      case Role.Rogue:
      case Role.Hunter:
      case Role.Trickster:
        roll += 2;
        break;
    }

    return -roll;
  }
}
