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

enum SpellDuration { NA, Short, Medium, Long, Combat }
enum SpellTarget { None, Self, PartyMember, FullParty, Monsters }
enum SpellContext { Exploring, Combat, Both }

record SpellInfo(string Code, string Name, int Level, int Cost, int Range, SpellDuration Duration, SpellContext Context, Role Role, SpellTarget Target);

class Spells
{
  static List<SpellInfo> _spells = [
    new SpellInfo("MAFL","Mage Flame", 1, 2, 0, SpellDuration.Medium, SpellContext.Exploring, Role.Conjurer, SpellTarget.None),
    new SpellInfo("ARFI","Arc Fire", 1, 3, 0, SpellDuration.NA, SpellContext.Combat, Role.Conjurer, SpellTarget.Monsters),
    new SpellInfo("SOSH","Sorcerer Shield", 1, 3, 0, SpellDuration.Combat, SpellContext.Combat, Role.Conjurer, SpellTarget.Self),
    new SpellInfo("TRZP","Trap Zap", 1, 2, 3, SpellDuration.NA, SpellContext.Exploring, Role.Conjurer, SpellTarget.None),
    new SpellInfo("FRFO","Freeze Foes", 2, 3, 0, SpellDuration.NA, SpellContext.Combat, Role.Conjurer, SpellTarget.Monsters),
    new SpellInfo("MACO","Magic Compass", 2, 3, 0, SpellDuration.Medium, SpellContext.Exploring, Role.Conjurer, SpellTarget.None),
    new SpellInfo("BASK","Battleskill", 2, 4, 0, SpellDuration.Combat, SpellContext.Combat, Role.Conjurer, SpellTarget.PartyMember),
    new SpellInfo("WOHL","Word of Healing", 2, 4, 0, SpellDuration.NA, SpellContext.Both, Role.Conjurer, SpellTarget.PartyMember),

    new SpellInfo("VOPL","Vorpal Plating", 1, 3, 0, SpellDuration.Combat, SpellContext.Combat, Role.Magician, SpellTarget.PartyMember),
    new SpellInfo("AIAR","Air Armour", 1, 3, 0, SpellDuration.Combat, SpellContext.Combat, Role.Magician, SpellTarget.Self),
    new SpellInfo("STLI","Steel Light", 1, 2, 2, SpellDuration.Short, SpellContext.Exploring, Role.Magician, SpellTarget.None),
    new SpellInfo("SCSI","Scry Site", 1, 2, 0, SpellDuration.NA, SpellContext.Exploring, Role.Magician, SpellTarget.None),
    new SpellInfo("MAGA","Mage Gaunlets", 2, 5, 0, SpellDuration.Combat, SpellContext.Combat, Role.Magician, SpellTarget.PartyMember),

    new SpellInfo("PHBL","Phase Blur", 1, 2, 0, SpellDuration.Combat, SpellContext.Combat, Role.Sorcerer | Role.Trickster, SpellTarget.FullParty),
    new SpellInfo("HYIM","Hyponotic Image", 1, 3, 0, SpellDuration.Combat, SpellContext.Combat, Role.Sorcerer | Role.Trickster, SpellTarget.Monsters),
    new SpellInfo("SOST","Soft Step", 1, 2, 0, SpellDuration.Medium, SpellContext.Exploring, Role.Trickster, SpellTarget.None)
  ];

  public static List<SpellInfo> SpellsFor(Character c)
  {
    List<SpellInfo> spells = [];
    
    spells.AddRange(_spells.Where(s => (s.Role & Role.Conjurer) == Role.Conjurer && s.Level <= c.ConjurerSpellLevel));
    spells.AddRange(_spells.Where(s => (s.Role & Role.Magician) == Role.Magician && s.Level <= c.MagicianSpellLevel));
    spells.AddRange(_spells.Where(s => (s.Role & Role.Sorcerer) == Role.Sorcerer && s.Level <= c.SorcererSpellLevel));
    spells.AddRange(_spells.Where(s => (s.Role & Role.Wizard) == Role.Wizard && s.Level <= c.WizardSpellLevel));
    spells.AddRange(_spells.Where(s => (s.Role & Role.Trickster) == Role.Trickster && s.Level <= c.TricksterSpellLevel));

    return [.. spells.OrderBy(s => s.Level).ThenBy(s => s.Name)];
  }
}

class SpellResolver(List<List<Monster>> foes, Battle battle)
{
  const int DURATION_SHORT = 1000;
  const int DURATION_MEDIUM = 2000;
  
  public List<string> Cast(SpellInfo spell, Character caster, Rng rng, int monsterGroup, Character? target, GameState gs)
  {
    List<string> msgBuffer = [];

    switch (spell.Code)
    {
      case "ARFI":
        Arfi(caster, monsterGroup, spell.Cost, rng, gs);
        break;
      case "AIAR":
        caster.MP -= spell.Cost;
        if (!caster.Buffs.Any(b => b.Buff == Buff.AirArmour))
          caster.Buffs.Add(new TempBuff(Buff.AirArmour, ulong.MaxValue, true));
        break;
      case "BASK":
        if (target is not null)
        {
          caster.MP -= spell.Cost;
          if (!target.Buffs.Any(b => b.Buff == Buff.BattleSkill))
            target.Buffs.Add(new TempBuff(Buff.BattleSkill, ulong.MaxValue, true));
        }
        break;
      case "FRFO":
        msgBuffer.AddRange(FreezeFoes(caster, monsterGroup, spell.Cost, rng, gs));
        break;
      case "HYIM":
        Hyim(caster, monsterGroup, spell.Cost, rng, gs);
        break;
      case "MACO":
        caster.MP -= spell.Cost;
        gs.Party.Buffs = [.. gs.Party.Buffs.Where(b => b.Buff != Buff.Compass)];
        gs.Party.Buffs.Add(new TempBuff(Buff.Compass, gs.Turn + DURATION_MEDIUM, false));
        break;
      case "MAFL":
        Mafl(caster, gs, spell.Cost);
        break;
      case "MAGA":
        if (target is not null)
        {
          caster.MP -= spell.Cost;
          if (!target.Buffs.Any(b => b.Buff == Buff.MageGauntlets))
            target.Buffs.Add(new TempBuff(Buff.MageGauntlets, ulong.MaxValue, true));
        }
        break;
      case "PHBL":
        Phbl(caster, gs, spell.Cost);
        break;
      case "SCSI":
        caster.MP -= spell.Cost;
        string s = "You are ";
        if (gs.CurrLevel == 0)
          s += $"in town at X {gs.X}, Y {gs.Y}. ";
        else
          s += $"on level {gs.CurrLevel} at X {gs.X}, Y {gs.Y}. ";
        s += $"You face {gs.Facing}.";
        gs.Say(s);

        if (gs.CurrLevel == 0)
        {
          var (h, m) = gs.CurrTime();
          gs.Say($"The time is {h}:{m.ToString().PadLeft(2, '0')}.");
        }
        break;
      case "SOSH":
        caster.MP -= spell.Cost;
        if (!caster.Buffs.Any(b => b.Buff == Buff.SorcShield))
          caster.Buffs.Add(new TempBuff(Buff.SorcShield, ulong.MaxValue, true));
        break;
      case "SOST":
        caster.MP -= spell.Cost;
        if (!gs.Party.Buffs.Any(b => b.Buff == Buff.SoftStep))
          gs.Party.Buffs.Add(new TempBuff(Buff.SoftStep, gs.Turn + DURATION_MEDIUM, false));
        break;
      case "STLI":
        Stli(caster, gs, spell.Cost);
        break;
      case "WOHL":
        if (target is not null && target.Condition != "DEAD")
        {
          caster.MP -= spell.Cost;
          int hp = rng.Next(2, 9);
          target.HP = int.Min(target.HP + hp, target.MaxHP);
          msgBuffer.Add($"{target.Name} is healed for {hp} hp.");
        }
        break;
      case "VOPL":
        if (target is not null)
        {
          caster.MP -= spell.Cost;
          if (!target.Buffs.Any(b => b.Buff == Buff.VorpalPlating))
            target.Buffs.Add(new TempBuff(Buff.VorpalPlating, ulong.MaxValue, true));
        }
        break;
      default:
        throw new Exception($"Unknown spell: {spell.Code}!");
    }

    return msgBuffer;
  }

  static void ClearLightSources(Party party)
  {
    party.Buffs = [.. party.Buffs.Where(b => b.Buff != Buff.MageFlame && b.Buff != Buff.SteelLight)];
  }

  List<string> Hyim(Character caster, int monsterGroup, int cost, Rng rng, GameState gs)
  {
    List<string> msgs = [];

    caster.MP -= cost;
    foreach (Monster m in foes[monsterGroup])
    {
      if (m.RollSavingThrow(caster.MagicPower, rng) == SaveResult.Success)
      {
        msgs.Add($"{m.Name.IndefArticle().Capitalize()} resists!");        
      }
      else if (!m.Buffs.Any(b => b.Buff == Buff.Hypnotized))
      {
        m.Buffs.Add(new(Buff.Hypnotized, gs.Turn + 1, true));
        msgs.Add($"{m.Name.IndefArticle().Capitalize()} becomes hypnotized!");
      }      
    }

    return msgs;
  }

  public static void Phbl(Character caster, GameState gs, int cost)
  {
    caster.MP -= cost;

    foreach (Character c in gs.Party.Members.OfType<Character>())
    if (!c.Buffs.Any(b => b.Buff == Buff.PhaseBlur))
      c.Buffs.Add(new TempBuff(Buff.PhaseBlur, uint.MaxValue, true));
  }

  // caster is nullable because lights can be created from torches, etc
  public static void Mafl(Character? caster, GameState gs, int cost)
  {
    caster?.MP -= cost;

    // Casting a light spell replaces an existing light
    ClearLightSources(gs.Party);
    gs.Party.Buffs.Add(new TempBuff(Buff.MageFlame, gs.Turn + 2000, false));
  }

  public static void Stli(Character caster, GameState gs, int cost)
  {
    ClearLightSources(gs.Party);

    caster.MP -= cost;
    gs.Party.Buffs.Add(new TempBuff(Buff.SteelLight, gs.Turn + DURATION_SHORT, false));
  }

  public List<string> FreezeFoes(Character caster, int monsterGroup, int cost, Rng rng, GameState gs)
  {
    List<string> msgs = [];

    caster.MP -= cost;
    foreach (Monster m in foes[monsterGroup])
    {
      if (m.RollSavingThrow(caster.MagicPower, rng) == SaveResult.Success)
      {
        msgs.Add($"{m.Name.IndefArticle().Capitalize()} resists!");
      }
      else
      {
        msgs.Add($"{m.Name.IndefArticle().Capitalize()} slows down!");
        m.Buffs.Add(new(Buff.FreezeFoe, ulong.MaxValue, true));
      }
    }
    
    return msgs;
  }

  public void Arfi(Character caster, int monsterGroup, int cost, Rng rng, GameState gs)
  {
    caster.MP -= cost;
    if (foes[monsterGroup].Count > 0)
    {
      int targetNum = rng.Next(foes[monsterGroup].Count);
      int dmg = rng.Next(1, 5) * caster.Level;
      Monster m = foes[monsterGroup][targetNum];
      var save = m.RollSavingThrow(caster.MagicPower, rng);
      if (save == SaveResult.Success)
      {
        gs.Say("But they resist!");
        return;
      }
      else if (save == SaveResult.Partial)
      {
        dmg = int.Max(1, dmg / 2);
      }
      battle.DamageMonster(monsterGroup, targetNum, dmg, gs);
    }
  }
}