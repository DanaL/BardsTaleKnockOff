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

record SpellInfo(string Code, string Name, int Level, int Cost, int Range, SpellDuration Duration, bool Combat, Role Role);

class Spells
{
  static List<SpellInfo> _spells = [
    new SpellInfo("MAFL","Mage Flame", 1, 2, 0, SpellDuration.Medium, false, Role.Conjurer),
    new SpellInfo("ARFI","Arc Fire", 1, 3, 0, SpellDuration.NA, true, Role.Conjurer),
    new SpellInfo("SOSH","Sorcerer Shield", 1, 3, 0, SpellDuration.Combat, true, Role.Conjurer),
    new SpellInfo("TRZP","Trap Zap", 1, 2, 3, SpellDuration.NA, false, Role.Conjurer),

    new SpellInfo("VOPL","Vorpal Plating", 1, 3, 0, SpellDuration.Combat, true, Role.Magician),
    new SpellInfo("AIAR","Air Armour", 1, 3, 0, SpellDuration.Combat, true, Role.Magician),
    new SpellInfo("STLI","Steel Light", 1, 2, 2, SpellDuration.Short, false, Role.Magician),
    new SpellInfo("SCSI","Scry Site", 1, 2, 0, SpellDuration.NA, false, Role.Magician)
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