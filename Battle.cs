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

// Use Item not implemented yet...
enum BattleAction { MeleeAttack, Defend, MonsterAction, HideInShadows, CastSpell, UseItem, ShootBow, MonstersAdvance }

record struct BattleChoice(PartySlot PartySlot, BattleAction Action,  int Group, PartySlot TargetPartySlot, SpellInfo? Spell, int InvIdx);

enum RoundResult { Continue, Victory, TPK }

class MonsterGroup
{
  public int Distance { get; set; }
  public List<Monster> Monsters { get; set; } = [];

  public string Name => Monsters[0].Name;
  public string Image => Monsters[0].Image;
  public int Count => Monsters.Count;
}

// The rules of combat. BattleScreen collects the party's choices and paces
// the messages; this class decides what actually happens.
//
// A round is: StartRound() with the party's choices, then ResolveNextAction()
// repeatedly until RoundOver, then EndRound(). Each action is resolved on its
// own so the screen can show its messages before the next one changes the
// party or the monsters.
class Battle(GameState gs, Rng rng, List<MonsterGroup> foes)
{
  public List<MonsterGroup> Foes { get; private set; } = foes;

  // Messages describing what happened, for the screen to show in order
  public Queue<string> Messages { get; } = new();

  readonly PriorityQueue<BattleChoice, int> _actions = new();
  int _actingGroup = -1;
  int _actingMonster = 0;

  int _xpWon = 0;
  int _goldWon = 0;
  int _monstersDefeated = 0;

  public bool RoundOver => _actions.Count == 0 && _actingGroup < 0;

  public void StartRound(IEnumerable<BattleChoice> choices, bool partyAdvance)
  {
    if (partyAdvance)
    {
      Messages.Enqueue("The party advances");
      foreach (var mg in Foes)
      {
        if (mg.Distance > 10)
          mg.Distance -= 10;
      }
    }
    else
    {
      QueuePartyChoices(choices);
    }

    int advInitiative = 1000; // groups advancing always happens after everyone else has acted
    for (int n = 0; n < Foes.Count; n++)
    {
      if (Foes[n].Distance == 10 || Foes[n].Monsters[0].HasFlag(MonsterFlag.Ranged))      
      {
        int roll = rng.Next(1, 24);
        _actions.Enqueue(new(PartySlot.None, BattleAction.MonsterAction, n, PartySlot.None, null, -1), -roll);        
      }
      else
      {
        _actions.Enqueue(new(PartySlot.None, BattleAction.MonstersAdvance, n, PartySlot.None, null, -1), advInitiative++);
      }      
    }
  }

  public void QueuePartyChoices(IEnumerable<BattleChoice> choices)
  {
    foreach (var choice in choices)
    {
      if (gs.Party.Slot(choice.PartySlot) is not Character c || c.Incapacitated)
        continue;

      switch (choice.Action)
      {
        case BattleAction.Defend:
          c.Buffs.Add(new TempBuff(Buff.Defending, gs.Turn, true));
          Messages.Enqueue($"{c.Name} defends.");
          break;
        case BattleAction.HideInShadows:
          // Hide in shadows should happen before other actions
          _actions.Enqueue(choice, -int.MaxValue);
          break;
        default:
          _actions.Enqueue(choice, Initiative(c));
          break;
      }
    }

    if (gs.Party.Special is Monster m)
    {
      
    }
  }

  // Resolve a single attack. When a monster group's turn comes up, each
  // monster in it attacks on its own step. Nothing else acts until the
  // whole group has gone, so the indexes into the group stay valid.
  public void ResolveNextAction()
  {
    if (_actingGroup >= 0)
    {
      MonsterGroup group = Foes[_actingGroup];
      if (_actingMonster < group.Count)
      {
        MonsterAction(group.Monsters[_actingMonster++], group.Distance);
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
    else if (choice.Action == BattleAction.MonstersAdvance)
    {
      AdvanceGroup(choice.Group);
    }
    //else if (choice.PartySlot is )
    else if (gs.Party.Slot(choice.PartySlot) is Character c && !c.Incapacitated)
    {
      switch (choice.Action)
      {
        case BattleAction.MeleeAttack:
          CharacterMeleeAttack(c, choice.Group);
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
        case BattleAction.ShootBow:
          if (choice.InvIdx != -1)
            CharacterBowShot(c, choice.InvIdx, choice.Group);
          break;
        case BattleAction.UseItem:
          if (choice.InvIdx != -1)
            CharacterUseItem(c, choice.InvIdx, gs);
          break;
        case BattleAction.CastSpell:
          if (choice.Spell is SpellInfo spell)
          {
            IBeing? partyTarget = gs.Party.Slot(choice.TargetPartySlot);
            string s = $"{c.Name} casts {spell.Code}";

            if (gs.CurrTile.HasFlag(TileFlags.Antimagic))
            {
              Messages.Enqueue(s + " but the spell fizzles!");
              c.MP -= spell.Cost;              
              return;
            }

            if (choice.Spell.Target == SpellTarget.PartyMember && partyTarget is not null)
            {
              s += $" on {partyTarget.Name}";
            }
            else if (choice.Spell.Target == SpellTarget.Monsters && Foes[choice.Group].Count > 0)
            {
              var g = Foes[choice.Group];
              string mn = g.Count == 1 ? g.Name.DefArticle() : $"some {g.Name.Pluralize()}";
              s += $" on {mn}";
            }
              
            s += ".";
            
            if (c.MP < choice.Spell.Cost) 
            {
              Messages.Enqueue(s);
              Messages.Enqueue($"But they have insufficient Magic Points!");
            }
            else
            {
              // Skip the message for Smite because I think "So-and-so casts SMTE on..."
              // looks goofy
              if (spell.Code != "SMTE")
                Messages.Enqueue(s);
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

  void CharacterUseItem(Character c, int invIdx, GameState gs)
  {
    if (c.Inventory[invIdx] is not InvSlot invSlot)
      return;

    Item item = ItemCatalog.Item(invSlot.Id);
    Messages.Enqueue($"{c.Name} uses {item.Name.IndefArticle()}");
    bool consumed = ItemCatalog.Use(item, gs, c, Messages, rng);

    if (!consumed)
      return;

    c.Inventory[invIdx] = invSlot.Count == 1 ? null : invSlot with { Count = invSlot.Count - 1};
  }

  // Advance the clock, expire buffs, move the fallen to the back and drop
  // any monster groups that have been wiped out.
  public RoundResult EndRound()
  {
    gs.Party.StatusEffects(gs);

    if (gs.Party.Alive == 0)
    {
      return RoundResult.TPK;
    }

    foreach (Character member in gs.Party.Members.OfType<Character>())
    {
      member.Buffs = [.. member.Buffs.Where(b => b.ExpiresOn > gs.Turn)];
    }
    foreach (var mg in Foes)
    {
      foreach (var monster in mg.Monsters)
        monster.Buffs = [.. monster.Buffs.Where(b => b.ExpiresOn > gs.Turn)];
    }

    ++gs.Turn;

    gs.Party.MoveIncapacitatedToBack();

    for (int j = Foes.Count - 1; j >= 0; j--)
    {
      if (Foes[j].Count == 0)
        Foes.RemoveAt(j);
    }

    return Foes.Count == 0 ? RoundResult.Victory : RoundResult.Continue;
  }

  // Share out the XP and gold won among the survivors. Returns each
  // survivor's share.
  // Eventually, I need to add finding the occasional item!
  public (int XP, int Gold, int MonstersDefeated) AwardSpoils() => (_xpWon, _goldWon, _monstersDefeated);

  void AdvanceGroup(int groupNum)
  {
    var mg = Foes[groupNum];

    if (mg.Count == 0)
      return;

    // Cases:
    // 1) You are the first group and > 10' feet
    // 2) There is more than a 10' gap between you and the next group
    // 3) Check if you can swap with the next group down the line

    if (groupNum == 0 && mg.Distance > 10)
    {
      mg.Distance -= 10;
      Msg(mg);
      return;
    }

    var otherGroup = Foes[groupNum - 1];
    if (mg.Distance - otherGroup.Distance > 10 || groupNum == 1)
    {
      mg.Distance -= 10;
      Msg(mg);
    }
    else if (otherGroup.Count == 0 || otherGroup.Monsters[0].HasFlag(MonsterFlag.Ranged) || otherGroup.Distance > 10)
    {      
      mg.Distance -= 10;
      otherGroup.Distance += 10;
      Msg(mg);
      (Foes[groupNum], Foes[groupNum - 1]) = (Foes[groupNum - 1], Foes[groupNum]);
    }
    
    void Msg(MonsterGroup mg)
    {
      string s = mg.Count > 1 ? mg.Name.Pluralize() : mg.Name;
      Messages.Enqueue($"{s.DefArticle().Capitalize()} advance!");
    }
  }

  void MonsterAction(Monster m, int distance)
  {
    if (m.Buffs.Any(b => b.Buff == Buff.Hypnotized))
    {
      Messages.Enqueue($"{m.Name.IndefArticle().Capitalize()} is mesmerized.");
      // Check this here so that if a monster acts the same round they had HYIM
      // cast on them but after the casting, they don't miss two turns
      m.Buffs = [.. m.Buffs.Where(b => b.Buff != Buff.Hypnotized)];
      return;  
    }

    List<(IBeing Being, bool Melee)> targets = [];    
    if (gs.Party.Special is IBeing special && !special.Incapacitated) 
    {
      targets.Add((special, true));
    }
    for (int j = 0; j < gs.Party.Count; j++)
    {
      if (gs.Party.Members[j] is Character c && !c.Incapacitated && !c.Buffs.Any(b => b.Buff == Buff.Hiding))
      {
        bool melee = j < 3;
        targets.Add((c, melee));
      }
    }
    
    if (targets.Count == 0)
      return;

    List<IBeing> meleeTargets = [.. targets.Where(t => t.Melee).Select(t => t.Being)];

    List<Action> options = [];
    if (distance == 10 && meleeTargets.Count > 0)
    {
      var target = meleeTargets[rng.Next(meleeTargets.Count)];
      options.Add(() => MonsterMeleeAttack(m, target));
    }
    
    options.AddRange(MonsterOptions(m, [.. targets.Select(t => t.Being)]));
    
    if (options.Count == 0)
      return;

    options[rng.Next(options.Count)]();
  }

  List<Action> MonsterOptions(Monster m, List<IBeing> targets)
  {
    List<Action> options = [];
    switch (m.Script)
    {
      case MonsterScript.Conjuror0:
        options.Add(() => MonsterCastArcFire(m, Pick(targets), 1));
        if (rng.Next(3) >= 0)
          options.Add(() => MonsterCaseFreezeFoes(m));
        break;
      case MonsterScript.Magician0:
        // In og BT1, I don't remember Magicians casting WIST very often or 
        // it being a big problem in the early game so I'll make it somewhat
        // uncommon
        options.Add(() => MonsterCastZap(m, Pick(targets)));
        if (rng.Next(3) == 0 && !m.Buffs.Any(b => b.Buff == Buff.Invisibility))
          options.Add(() => MonsterCastInvisibility(m));
        if (rng.Next(7) == 0)
          options.Add(() => MonsterCastWitherStrike(m, Pick(targets)));
        break;
      default:
        options = [];
        break;
    }

    return options;

    IBeing Pick(List<IBeing> targets) => targets[rng.Next(targets.Count)];
  }

  void MonsterCastInvisibility(Monster m)
  {
    Messages.Enqueue($"{m.FullName.Capitalize() } casts Invisibility!");
    if (!m.Buffs.Any(b => b.Buff == Buff.Invisibility))
    {
      m.Buffs.Add(new TempBuff(Buff.Invisibility, ulong.MaxValue, true));
    }
  }

  void MonsterCaseFreezeFoes(Monster m)
  {
    Messages.Enqueue($"{m.Name.IndefArticle().Capitalize() } casts Freeze Foes on the party!");

    foreach (var c in gs.Party.ActiveMembers())
    {
      if (c.Buffs.Any(b => b.Buff == Buff.FreezeFoe))
        continue;

      if (c.RollSavingThrow(m.Rank, rng) == SaveResult.Success)
      {
        Messages.Enqueue($"{c.FullName} resists!");
      }
      else
      {
        Messages.Enqueue($"{c.FullName} becomes enervated!");
        c.Buffs.Add(new TempBuff(Buff.FreezeFoe, ulong.MaxValue, true));
      }
    }
  }

  // It's a little redundant to duplicate a bunch of the spell cast code, but
  // monsters don't really have that many abilities and this keeps the methods
  // pretty simple

  void MonsterCastWitherStrike(Monster m, IBeing target)
  {
    Messages.Enqueue($"{m.Name.IndefArticle().Capitalize() } casts Wither Strike on {target.FullName}!");
    if (target.Condition == "OLD")
      return;
    
    if (target.RollSavingThrow(m.Rank, rng) == SaveResult.Success)
    {
      Messages.Enqueue($"...but they resist!");
    }
    else
    {
      Messages.Enqueue($"{target.FullName} is Withered!");
      target.Condition = "OLD";
    }    
  }

  void ResolveMonsterDmgSpell(Monster m, IBeing target, string msg, int dmg)
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

  void MonsterCastArcFire(Monster m, IBeing target, int level)
  {
    int dmg = rng.Next(1, 5) * level;
    
    string msg = $"{m.FullName.Capitalize() } casts Arc Fire on {target.FullName}";
    ResolveMonsterDmgSpell(m, target, msg, dmg);
  }

  void MonsterCastZap(Monster m, IBeing target)
  {
    int dmg = rng.Next(1, 5);
    
    string msg = $"{m.FullName.Capitalize() } zaps {target.FullName}";
    ResolveMonsterDmgSpell(m, target, msg, dmg);
  }

  void MonsterMeleeAttack(Monster m, IBeing target)
  {    
    string n = Util.CalcName(m).DefArticle().Capitalize();
    int roll = rng.Next(1, 21);
    bool nat20 = roll == 20;
    var mods = m.MeleeMods(rng);
    roll += mods.ToHit;
    int toHitTarget = m.THAC0 - target.AC + target.DefenseMods();

    if (m.Condition == "OLD")
      roll -= 10;

    if (roll >= toHitTarget || nat20)
    {
      int dmg = rng.Next(m.DmgLow, m.DmgHigh + 1) + mods.Damage;
      if (m.Condition == "OLD")
        dmg /= 4;
      Messages.Enqueue($"{n} {m.Verb} {target.FullName} and hits for {dmg} hp!");
      string s = target.ReceiveDmg(dmg);
      if (s != "")
        Messages.Enqueue(s);

      if (m.HasFlag(MonsterFlag.Poison))
      {
        var save = target.RollSavingThrow(m.Rank, rng);
        if (save != SaveResult.Success)
        {
          Messages.Enqueue($"{target.FullName} is poisoned!");
          target.Condition = "POISONED";
        }
      }
      if (m.HasFlag(MonsterFlag.Corrosion) && target is Character c)
      {
        CheckCorrosion(m, c);
      }
    }
    else
    {
      Messages.Enqueue($"{n} {m.Verb} {target.FullName} and misses!");
    }
  }

  void CheckCorrosion(Monster m, Character victim)
  {
    if (rng.Next(3) >= 0)
    {
      Messages.Enqueue($"The attack of {m.FullName} is acidic!");
      List<int> slots = [];
      for (int j = 0; j < Character.INVENTORY_SLOTS; j++)
      {
        if (victim.Inventory[j] is InvSlot i && i.Equipped)
          slots.Add(j);
      }

      if (slots.Count == 0)
        return;
      
      int slot = slots[rng.Next(slots.Count)];
      if (victim.Inventory[slot] is not InvSlot invSlot)
        return;

      int corrodedId = ItemCatalog.CorrodedId(invSlot.Id);
      if (corrodedId != invSlot.Id)
      {
        Item item = ItemCatalog.Item(invSlot.Id);
        Messages.Enqueue($"Their {item.Name} corrodes!");
        victim.Inventory[slot] = invSlot with { Id = corrodedId };
      }
    }
  }

  public void CharacterBowShot(Character c, int invIdx, int monsterGroup)
  {
    if (Foes[monsterGroup].Count == 0)
      return;

    if (c.Inventory[invIdx] is not InvSlot ammoSlot)
      return;

    if (c.EquippedBow is not Item bow)
      return;

    Item ammo = ItemCatalog.Item(ammoSlot.Id);

    var mods = c.RangedMods();
    Monster target = Foes[monsterGroup].Monsters[0];
    int dexMod = Character.StatToMod(c.Dex);
    int archerBonus = c.Role == Role.Archer ? 1 + c.Level / 4 : 0;

    int roll = rng.Next(1, 21) + dexMod + bow.AttackBonus + archerBonus + mods.ToHit;

    if (c.Condition == "OLD")
      roll -= 10;

    int toHitTarget = c.THAC0 - target.AC + target.DefenseMods();

    string s = $"{c.Name} shoots at {target.Name.IndefArticle().ToLower()}";
    bool criticalHit = false;
    if (roll >= toHitTarget)
    {
      int dmg = archerBonus + bow.DamageBonus + dexMod + mods.Damage;
      for (int k = 0; k < bow.NumDamageDice; k++)
        dmg += rng.Next(bow.DamageDie) + 1;
      criticalHit = c.CritChance > 0 && rng.Next(256) < c.CritChance;

      s+= criticalHit ? $" and hits, killing it instantly!" : $" and hits for {dmg} dmg!";
      Messages.Enqueue(s);

      int monsterHit = rng.Next(Foes[monsterGroup].Count);
      if (criticalHit)
        KillMonster(monsterGroup, monsterHit, gs);
      else
        DamageMonster(monsterGroup, monsterHit, dmg, gs, Messages);
    } 
    else
    {
      s += " but misses!";
      Messages.Enqueue(s);
    }
    
    // If you attack from hiding, you are no longer hiding
    c.Buffs = [.. c.Buffs.Where(b => b.Buff != Buff.Hiding)];

    // Consume the ammunition
    if (ammoSlot.Count > 1)
      c.Inventory[invIdx] = ammoSlot with { Count = ammoSlot.Count - 1 };
    else
      c.Inventory[invIdx] = null;
  }

  public void CharacterMeleeAttack(Character c, int monsterGroup)
  {
    if (Foes[monsterGroup].Count == 0)
      return;

    bool hiding = c.Buffs.Any(b => b.Buff == Buff.Hiding);
    bool smiting = c.Buffs.Any(b => b.Buff == Buff.Smite);

    Monster target = Foes[monsterGroup].Monsters[0];
    Item? weapon = c.EquippedWeapon;

    string verb;
    if (weapon is null)
      verb = "punches";
    else if (smiting)
      verb = "smites";
    else
      verb = "attacks";
    
     string s = $"{c.Name} {verb} {target.Name.IndefArticle().ToLower()}";
     int attacks = c.NumOfMeleeAttacks;
     if (attacks > 1)
      s += $" {attacks} times";

    int strMod = Character.StatToMod(c.Str);
    int hits = 0;
    int dmg = 0;
    
    for (int j = 0; j < c.NumOfMeleeAttacks; j++)
    {
      var mods = c.MeleeMods(rng);
      int roll = rng.Next(1, 21) + strMod + mods.ToHit;
      if (weapon is not null)
        roll += weapon.AttackBonus;

      if (c.Condition == "OLD")
         roll -= 10;
      if (smiting)
        roll += 3;

      int toHitTarget = c.THAC0 - target.AC + target.DefenseMods();

      if (roll >= toHitTarget)
      {
        ++hits;
        dmg += strMod + mods.Damage;

        if (smiting)
          dmg += rng.Next(1, 9) * int.Max(1, c.Level / 2);

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

    // Archers only crit with bows
    bool criticalHit = hits > 0 && (hiding || (c.CritChance > 0 && c.Role != Role.Archer && rng.Next(256) < c.CritChance));
      
    if (hiding)
      s = $"{c.Name} leaps from the shadows";

    string targetName = target.FullName.ToLower();
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
    Monster m = Foes[group].Monsters[targetNum];
    if (dmg >= m.HP)
    {
      msgQ.Enqueue($"{m.FullName.ToLower().Capitalize()} is killed!");
      KillMonster(group, targetNum, gs);
      return true;
    }

    if (dmg > 0)
      m.HP -= dmg;

    return false;
  }

  public void KillMonster(int group, int index, GameState gs)
  {
    Monster m = Foes[group].Monsters[index];
    _xpWon += m.XP;
    _goldWon += m.Gold;
    ++_monstersDefeated;
    Foes[group].Monsters.RemoveAt(index);
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


// Bonuses an attacker gets from its buffs. Damage includes dice rolls (Vorpal
// Plating, Mage Gauntlets), so get a fresh AttackMods for each swing.
record struct AttackMods(int ToHit, int Damage);

static class CombatMods
{
  // Smite isn't here because its damage scales with a character's level; it's
  // handled in CharacterMeleeAttack.
  public static AttackMods MeleeMods(this IBeing attacker, Rng rng)
  {
    int hit = 0, dmg = 0;
    foreach (var b in attacker.Buffs)
    {
      switch (b.Buff)
      {
        case Buff.Hiding:
          hit += 3;
          break;
        case Buff.BattleSkill:
          hit += 4;
          dmg += 4;
          break;
        case Buff.OgreStrength:
          hit += 7;
          dmg += 7;
          break;
        case Buff.VorpalPlating:
          dmg += rng.Next(2, 9);
          break;
        case Buff.MageGauntlets:
          dmg += rng.Next(4, 17);
          break;
      }
    }

    return new(hit, dmg);
  }

  public static AttackMods RangedMods(this IBeing attacker)
  {
    int hit = 0, dmg = 0;
    foreach (var b in attacker.Buffs)
    {
      switch (b.Buff)
      {
        case Buff.Hiding:
          hit += 3;
          break;
        case Buff.BattleSkill:
          hit += 4;
          dmg += 4;
          break;
      }
    }

    return new(hit, dmg);
  }

  // Added to the roll an attacker needs to hit this target: positive makes the
  // target harder to hit, negative easier.
  public static int DefenseMods(this IBeing target)
  {
    int mod = 0;
    foreach (var b in target.Buffs)
    {
      switch (b.Buff)
      {
        case Buff.Defending:
          mod += 5;  // roughly Disadvantage, or the Dodge action, in 5e
          break;
        case Buff.Invisibility:
          mod += 5;
          break;
        case Buff.PhaseBlur:
          mod += 2;
          break;
        case Buff.FreezeFoe:
          mod -= 2;
          break;
      }
    }

    if (target.Condition == "OLD")
      mod -= 5;

    return mod;
  }

  // Separate from DefenseMods because for the party I display the current
  // AC live
  public static int ACMods(this IBeing b)
  {
    int mods = 0;
    foreach (var buff in b.Buffs)
    {
      if (buff.Buff == Buff.AirArmour)
        mods -= 2;
      else if (buff.Buff == Buff.SorcShield)
        mods -= 1;
      else if (buff.Buff == Buff.MysticShield || buff.Buff == Buff.MysticCoatOfArmour)
        mods -= 2;
    }

    return mods;
  }
}
