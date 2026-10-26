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

record struct MonsterDef(int Id, int GroupSize);
record Monster(string Name, int AC, int HP, int DmgLow, int DmgHigh, int XP, int Gold, string Special, string Verb, string Image);

class MonsterCatalog
{
  const int GOBLIN = 0;
  const int ORC = 1;
  const int SKELETON = 2;
  const int CONJURER_0 = 3;
  const int MAGICIAN_0 = 4;
  const int FERAL_DOG = 5;

  static readonly Dictionary<int, List<MonsterDef>> _monsterTable = new()
  {
    [0] = [ new(GOBLIN, 8), new(ORC, 8), new(SKELETON, 4), new(FERAL_DOG, 8), new(CONJURER_0, 4), new(MAGICIAN_0, 4) ],
  };

  static Monster Get(int id, Rng rng) => id switch
  {
    GOBLIN => new Monster("Goblin", 10, rng.Next(3, 7) + 1, 1, 4, 60, rng.Next(1, 8) + 1, "", "attack", "goblin.png"),
    ORC => new Monster("Orc", 8, rng.Next(3, 11) + 1, 2, 8, 90, rng.Next(2, 20) + 1, "", "attack", "orc.png"),
    SKELETON => new Monster("Skeleton", 6, rng.Next(7, 15) + 1, 3, 12, 110, rng.Next(2, 20) + 1, "", "claw", "skeleton.png"),
    FERAL_DOG => new Monster("Feral dog", 7, rng.Next(3, 11) + 1, 2, 8, 90, rng.Next(0, 4) + 1, "", "bite", "canine.png"),
    CONJURER_0 => new Monster("Conjurer", 9, rng.Next(1, 9) + 1, 1, 4, 100, rng.Next(2, 20) + 1, "ARFI", "attack", "conjurer.png"),
    MAGICIAN_0 => new Monster("Magician", 9, rng.Next(1, 9) + 1, 1, 4, 100, rng.Next(2, 20) + 1, "WIST", "attack", "conjurer.png"),
    _ => throw new Exception($"Unknown monster id {id}")  
  };

  public static List<Monster> GroupForLevel(int level, Rng rng)
  {
    List<Monster> group = [];

    if (_monsterTable.TryGetValue(level, out var table))
    {
      var (id, max) = table[rng.Next(table.Count)];
      int count = rng.Next(max) + 1;
      for (int j = 0; j < count; j++)
        group.Add(Get(id, rng));
    }

    return group;
  }
}