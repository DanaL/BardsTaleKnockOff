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

enum MonsterScript { Melee, Conjuror0, Magician0 };
class Monster(string name, int ac, int hp, int thac0, int dmgLow, int dmgHigh, int xp, int gold, MonsterScript script, string verb, string image, bool ranged, int rank)
{
  public string Name { get; } = name;
  public int AC { get; } = ac;
  public int HP { get; set; } = hp;
  public int THAC0 { get; } = thac0;
  public int DmgLow { get; } = dmgLow;
  public int DmgHigh { get; } = dmgHigh;
  public int XP { get; } = xp;
  public int Gold { get; } = gold;
  public MonsterScript Script { get; } = script;
  public string Verb { get; } = verb;
  public string Image { get; } = image;
  public bool Ranged { get; } = ranged;
  public int Rank { get; } = rank;
  public Fact? DefeatFact { get; set; } = null;
  
  public List<TempBuff> Buffs { get; set; } = [];

  public SaveResult RollSavingThrow(int magicPower, Rng rng)
  {
    int roll = Rank + rng.Next(8);
    int margin = roll - magicPower;

    if (margin < 0)
      return SaveResult.Failure;
    else if (margin <= 4)
      return SaveResult.Partial;
    else
      return SaveResult.Success;
  }
}

class MonsterCatalog
{
  public const int GOBLIN = 0;
  public const int ORC = 1;
  public const int SKELETON = 2;
  public const int CONJURER_0 = 3;
  public const int MAGICIAN_0 = 4;
  public const int FERAL_DOG = 5;
  public const int SPIDER = 6;
  
  public static Monster Get(int id, Rng rng) => id switch
  {    
    GOBLIN => new Monster("Goblin", 10, rng.Next(3, 7) + 1, 19, 1, 4, 60, rng.Next(20, 51), MonsterScript.Melee, "attacks", "goblin.png", false, 1),
    ORC => new Monster("Orc", 8, rng.Next(3, 11) + 1, 18, 2, 8, 90, rng.Next(20, 81), MonsterScript.Melee, "attacks", "orc.png", false, 1),
    SKELETON => new Monster("Skeleton", 6, rng.Next(7, 15) + 1, 18, 3, 12, 110, rng.Next(20, 81), MonsterScript.Melee, "claws at", "skeleton.png", false, 1),
    FERAL_DOG => new Monster("Feral dog", 7, rng.Next(3, 11) + 1, 18, 2, 8, 90, rng.Next(20, 41), MonsterScript.Melee, "bites", "canine.png", false, 1),
    CONJURER_0 => new Monster("Conjurer", 9, rng.Next(1, 9) + 1, 18, 1, 4, 100, rng.Next(25, 61), MonsterScript.Conjuror0, "attacks", "magicuser_yellow.png", true, 1),
    MAGICIAN_0 => new Monster("Magician", 9, rng.Next(1, 9) + 1, 18, 1, 4, 100, rng.Next(25, 61), MonsterScript.Magician0, "attacks", "magicuser_green.png", true, 1),
    SPIDER => new Monster("Spider", 6, rng.Next(5, 12), 17, 3, 12, 120, rng.Next(20, 51), MonsterScript.Melee, "bites", "spider.png", false, 1),
    _ => throw new Exception($"Unknown monster id {id}")  
  };
}