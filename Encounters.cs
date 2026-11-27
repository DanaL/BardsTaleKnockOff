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

static class Encounters
{
  // Pick the monster groups for a random encounter at the party's current location.
  public static List<MonsterGroup> Generate(GameState gs, Rng rng)
  {
    List<MonsterGroup> groups = [];

    List<(Monsters, int)> monsterOptions = [.. Util.MonsterTable[gs.CurrRegion][gs.CurrLevel]];
    if (gs.CurrRegion == "town" && !gs.DayTime)
      monsterOptions.AddRange((Monsters.Magician0, 4), (Monsters.Spider, 4));
    else if (gs.CurrTile.Feature == Feature.House)
      monsterOptions.Add((Monsters.Spider, 4));

    (Monsters monsterId, int count) = monsterOptions[rng.Next(monsterOptions.Count)];
    groups.Add(GenGroup(monsterId, count, 10, rng));
    if (gs.CurrRegion == "town" && rng.Next(4) == 0)
    {
      (Monsters monsterId2, int count2) = monsterOptions[rng.Next(monsterOptions.Count)];
      groups.Add(GenGroup(monsterId2, count2, 10, rng));
    }
    else if (gs.CurrRegion != "town")
    {
      int extraGroups = rng.Next(10) switch
      {
        < 2 => 0,
        >= 2 and <= 6 => 1,
        7 or 8 => 2,
        _ => 3
      };

      int distance = 10;
      for (int j = 0; j < extraGroups; j++)
      {
        (Monsters mId, int cnt) = monsterOptions[rng.Next(monsterOptions.Count)];
        var group = GenGroup(mId, cnt, distance, rng);
        if (group.Monsters[0].HasFlag(MonsterFlag.Ranged))
        {
          int extraD = rng.Next(3) == 0 ? 20 : 10;
          distance += extraD;
          group.Distance += extraD;
        }
        groups.Add(group);
        distance += 10;
      }
    }

    return groups;
  }

  static MonsterGroup GenGroup(Monsters monsterId, int num, int distance, Rng rng)
  {
    MonsterGroup monsters = new() { Distance = distance };
    int count = rng.Next(num) + 1;
    for (int j = 0; j < count; j++)
      monsters.Monsters.Add(MonsterCatalog.Get(monsterId, rng));

    return monsters;
  }
}
