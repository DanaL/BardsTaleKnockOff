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
enum BattleAction { MeleeAttack, Defend, MonsterAction, HideInShadows, CastSpell }

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

    for (int n = 0; n < Foes.Count; n++)
    {
      int roll = rng.Next(1, 24);
      BattleChoice monsterChoice = new(n, BattleAction.MonsterAction, n, -1);
      _actions.Enqueue(monsterChoice, -roll);
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
            if (choice.Spell.Target == SpellTarget.Party && partyTarget is not null)
              s += $" on {partyTarget.Name}";
            s += ".";
            Messages.Enqueue(s);
            SpellResolver sr = new(Foes, this);
            
            if (c.MP < choice.Spell.Cost) 
            {
              Messages.Enqueue($"But they have insufficient Magic Points!");
            }
            else
            {
              sr.Cast(choice.Spell, c, rng, choice.Group, partyTarget, gs);  
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
    ++gs.Turn;
    if (gs.Party.Alive == 0)
    {
      // Uh-oh, TPK
    }

    foreach (Character member in gs.Party.Members.OfType<Character>())
    {
      member.Buffs = [.. member.Buffs.Where(b => b.ExpiresOn > gs.Turn)];
    }

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

  // Only monsters in the first 2 ranks can engage in melee
  // Not sure what rate they should do their special attacks at yet
  void MonsterAction(Monster m, int group)
  {
    // eventually check for special abilities
    if (group > 1)
      return;

    // also have to take into account the Special slot eventually
    List<int> targets = [];
    for (int j = 0; j < 3; j++)
    {
      if (gs.Party.Members[j] is Character c && !c.Incapacitated && !c.Buffs.Any(b => b.Buff == Buff.Hiding))
        targets.Add(j);
    }

    if (targets.Count == 0)
      return;

    int s = targets[rng.Next(targets.Count)];
    if (gs.Party.Members[s] is Character target)
    {
      string n = m.Name.ToLower().DefArticle().Capitalize();
      int roll = rng.Next(1, 21);
      int targetAC = m.THAC0 - target.AC;
      if (target.Buffs.Any(b => b.Buff == Buff.Defending))
        targetAC += 5; // Not sure what BT1 did, but -5 is roughly equip to Disadvantage in 5e
                       // and what you get for taking the Dodge action
      if (roll > targetAC)
      {
        int dmg = rng.Next(m.DmgLow, m.DmgHigh + 1);
        Messages.Enqueue($"{n} {m.Verb} {target.Name} and hits for {dmg} hp!");
        target.HP -= dmg;

        if (target.HP < 1)
        {
          Messages.Enqueue($"{target.Name} is killed!");
          target.Condition = "DEAD";
        }
      }
      else
      {
        Messages.Enqueue($"{n} {m.Verb} {target.Name} and misses!");
      }
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
    foreach (var buff in c.Buffs)
    {
      if (buff.Buff == Buff.Hiding)
        hiding = true;
      else if (buff.Buff == Buff.VorpalPlating)
        vorpalPlating = true;
    }

    for (int j = 0; j < c.NumOfMeleeAttacks; j++)
    {
      int roll = rng.Next(1, 21) + strMod;
      if (weapon is not null)
        roll += weapon.AttackBonus;

      if (hiding)
        roll += 5;
      // Eventually there will be other buffs?

      int targetAC = c.THAC0 - target.AC;
      if (roll > targetAC)
      {
        ++hits;
        dmg += strMod;

        if (vorpalPlating)
          dmg += rng.Next(2, 9);

        if (weapon is null)
          dmg += 1;
        else
        {
          dmg += weapon.DamageBonus;
          for (int k = 0; k < weapon.NumDamageDice; k++)
            dmg += rng.Next(weapon.DamageDie) + 1;
        }
      }
    }

    if (hiding)
      s = $"{c.Name} leaps from the shadows";

    string targetName = target.Name.DefArticle().ToLower();
    if (hits == 0)
      s += " but misses!";
    else if (hiding)
      s += $" and strikes {targetName}, killing it instantly!";
    else if (hits == 1)
      s+= $" and hits for {dmg} dmg!";
    else
      s+= $" and hits {hits} times for {dmg} dmg!";

    Messages.Enqueue(s);

    // If you attack from hiding, you are no longer hiding
    c.Buffs = [.. c.Buffs.Where(b => b.Buff != Buff.Hiding)];

    int monsterHit = rng.Next(Foes[monsterGroup].Count);
    if (hiding && hits > 0)
      KillMonster(monsterGroup, monsterHit);
    else
      DamageMonster(monsterGroup, monsterHit, dmg);
  }

  // Killing a monster shifts the indexes of the ones after it in the group,
  // so when damaging a whole group, loop from the back.
  public bool DamageMonster(int group, int targetNum, int dmg)
  {
    Monster m = Foes[group][targetNum];
    if (dmg >= m.HP)
    {
      Messages.Enqueue($"{m.Name.DefArticle().ToLower().Capitalize()} is killed!");
      KillMonster(group, targetNum);
      return true;
    }

    if (dmg > 0)
      Foes[group][targetNum] = m with { HP = m.HP - dmg };

    return false;
  }

  public void KillMonster(int group, int index)
  {
    Monster m = Foes[group][index];
    _xpWon += m.XP;
    _goldWon += m.Gold;
    Foes[group].RemoveAt(index);
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
