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
enum SpellTarget { None, Self, Party, Monsters }

record SpellInfo(string Code, string Name, int Level, int Cost, int Range, SpellDuration Duration, bool Combat, Role Role, SpellTarget Target);

class Spells
{
  static List<SpellInfo> _spells = [
    new SpellInfo("MAFL","Mage Flame", 1, 2, 0, SpellDuration.Medium, false, Role.Conjurer, SpellTarget.None),
    new SpellInfo("ARFI","Arc Fire", 1, 3, 0, SpellDuration.NA, true, Role.Conjurer, SpellTarget.Monsters),
    new SpellInfo("SOSH","Sorcerer Shield", 1, 3, 0, SpellDuration.Combat, true, Role.Conjurer, SpellTarget.Self),
    new SpellInfo("TRZP","Trap Zap", 1, 2, 3, SpellDuration.NA, false, Role.Conjurer, SpellTarget.None),

    new SpellInfo("VOPL","Vorpal Plating", 1, 3, 0, SpellDuration.Combat, true, Role.Magician, SpellTarget.Party),
    new SpellInfo("AIAR","Air Armour", 1, 3, 0, SpellDuration.Combat, true, Role.Magician, SpellTarget.Self),
    new SpellInfo("STLI","Steel Light", 1, 2, 2, SpellDuration.Short, false, Role.Magician, SpellTarget.None),
    new SpellInfo("SCSI","Scry Site", 1, 2, 0, SpellDuration.NA, false, Role.Magician, SpellTarget.None)
  ];

  public static List<SpellInfo> SpellsFor(Character c)
  {
    List<SpellInfo> spells = [];
    
    spells.AddRange(_spells.Where(s => s.Role == Role.Conjurer && s.Level <= c.ConjurerSpellLevel));
    spells.AddRange(_spells.Where(s => s.Role == Role.Magician && s.Level <= c.MagicianSpellLevel));
    spells.AddRange(_spells.Where(s => s.Role == Role.Sorcerer && s.Level <= c.SorcererSpellLevel));
    spells.AddRange(_spells.Where(s => s.Role == Role.Wizard && s.Level <= c.WizardSpellLevel));

    return [.. spells.OrderBy(s => s.Level).ThenBy(s => s.Name)];
  }
}

class SpellResolver(List<List<Monster>> foes, Battle battle)
{
  public void Cast(SpellInfo spell, Character? caster, Rng rng, int monsterGroup, Character? target, GameState gs)
  {
    switch (spell.Code)
    {
      case "ARFI":
        Arfi(caster, monsterGroup, target, spell.Cost, rng);
        break;      
      case "AIAR":
        if (caster is not null)
        {
          caster.MP -= spell.Cost;
          if (!caster.Buffs.Any(b => b.Buff == Buff.AirArmour))
            caster.Buffs.Add(new TempBuff(Buff.AirArmour, ulong.MaxValue, true));
        }
        break;
      case "SCSI":
        if (caster is not null)
        {
          caster.MP -= spell.Cost;
          string s = "You are ";
          if (gs.CurrLevel == 0)
            s += $"in town at X {gs.X}, Y {gs.Y}. ";
          else
            s += $"on level {gs.CurrLevel} at X {gs.X}, Y {gs.Y}. ";
          s += $"You face {gs.Facing}.";
          gs.Say(s);
        }
        break;
      case "SOSH":
        if (caster is not null)
        {
          caster.MP -= spell.Cost;
          if (!caster.Buffs.Any(b => b.Buff == Buff.SorcShield))
            caster.Buffs.Add(new TempBuff(Buff.SorcShield, ulong.MaxValue, true));
        }
        break;
      case "VOPL":
        if (caster is not null && target is not null)
        {
          caster.MP -= spell.Cost;
          target.Buffs.Add(new TempBuff(Buff.VorpalPlating, ulong.MaxValue, true));
        }
        break;
      default:
        throw new Exception($"Unknown spell: {spell.Code}!");
    }
  }

  public void Arfi(Character? caster, int monsterGroup, Character? target, int cost, Rng rng)
  {
    if (caster is not null)
    {
      caster.MP -= cost;

      if (foes[monsterGroup].Count > 0)
      {
        int targetNum = rng.Next(foes[monsterGroup].Count);
        int dmg = rng.Next(1, 5) * caster.Level;
        battle.DamageMonster(monsterGroup, targetNum, dmg);
      }
    }
    else
    {
      // I'll indicate a monster casting a spell with a null caster
    }
  }
}