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

[Flags]
enum MonsterFlag
{
  None       = 0b00000000,
  Undead     = 0b00000010,
  Ranged     = 0b00000100,
  Poison     = 0b00001000,
  Corrosion  = 0b00010000,
}

enum MonsterScript { Melee, Conjuror0, Magician0 };

class Monster(string name, int ac, int hp, int thac0, int dmgLow, int dmgHigh, int xp, int gold, MonsterScript script, string verb, string image, MonsterFlag flags, int rank) : IBeing
{
  public string Name { get; } = name;
  public string FullName { get; } = name.DefArticle();  
  public int HP { get; set; } = hp;
  public int MaxHP { get; set; } = hp;
  public int THAC0 { get; } = thac0;
  public int DmgLow { get; } = dmgLow;
  public int DmgHigh { get; } = dmgHigh;
  public int XP { get; } = xp;
  public int Gold { get; } = gold;
  public MonsterScript Script { get; } = script;
  public string Verb { get; } = verb;
  public string Image { get; } = image;
  public MonsterFlag Flags { get; set; } = flags;
  public int Rank { get; set; } = rank;
  public string Condition { get; set; } = "";  
  public List<TempBuff> Buffs { get; set; } = [];
  public int MP => 0;
  public int MaxMP => 0;
  public string RoleName => "Monster";
  
  int _baseAC { get; } = ac;
  public int AC => _baseAC + this.ACMods();

  public bool Incapacitated => HP < 1;

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

  public string ReceiveDmg(int dmg)
  {
    HP = int.Max(HP - dmg, 0);
    if (HP < 1)
    {
      Condition = "DEAD";
      return $"{Name} is killed!";
    }

    return "";
  }
  
  public bool HasFlag(MonsterFlag flag) => (Flags & flag) != MonsterFlag.None;
}

enum Monsters
{
  Goblin, Orc, Skeleton, Conjurer0, Magician0, FeralDog, Spider, Ogre, Nomad, Barbarian, Wolf, BlackWidow, Slime,
  AcidicSlime
}

class MonsterCatalog
{
  public static Monster Get(Monsters id, Rng rng) => id switch
  {
    Monsters.Slime => new Monster("Slime", 9, rng.Next(3, 8), 19, 1, 4, 60, rng.Next(20, 61), MonsterScript.Melee, "slimes", "slime.png", MonsterFlag.None, 1),
    Monsters.AcidicSlime => new Monster("Acidic Slime", 9, rng.Next(3, 12), 18, 1, 6, 80, rng.Next(20, 61), MonsterScript.Melee, "slimes", "slime.png", MonsterFlag.Corrosion, 1),
    Monsters.Goblin => new Monster("Goblin", 10, rng.Next(3, 8), 19, 1, 4, 60, rng.Next(20, 51), MonsterScript.Melee, "attacks", "goblin.png", MonsterFlag.None, 1),
    Monsters.Orc => new Monster("Orc", 8, rng.Next(3, 12), 18, 2, 8, 90, rng.Next(20, 81), MonsterScript.Melee, "attacks", "orc.png", MonsterFlag.None, 1),
    Monsters.Skeleton => new Monster("Skeleton", 6, rng.Next(7, 16), 18, 3, 12, 110, rng.Next(20, 81), MonsterScript.Melee, "claws at", "skeleton.png", MonsterFlag.Undead, 1),
    Monsters.FeralDog => new Monster("Feral dog", 7, rng.Next(3, 12), 18, 2, 8, 90, rng.Next(20, 41), MonsterScript.Melee, "bites", "canine.png", MonsterFlag.None, 1),
    Monsters.Conjurer0 => new Monster("Conjurer", 9, rng.Next(1, 10), 18, 1, 4, 100, rng.Next(25, 61), MonsterScript.Conjuror0, "attacks", "magicuser_yellow.png", MonsterFlag.Ranged, 1),
    Monsters.Magician0 => new Monster("Magician", 9, rng.Next(1, 10), 18, 1, 4, 100, rng.Next(25, 61), MonsterScript.Magician0, "attacks", "magicuser_green.png", MonsterFlag.Ranged, 1),
    Monsters.Nomad => new Monster("Nomad", 6, rng.Next(8, 16), 17, 3, 12, 120, rng.Next(40, 121), MonsterScript.Melee, "attacks", "barbarian.png", MonsterFlag.None, 1),
    Monsters.Barbarian => new Monster("Barbarian", 6, rng.Next(10, 18), 16, 4, 16, 180, rng.Next(50, 141), MonsterScript.Melee, "attacks", "barbarian.png", MonsterFlag.None, 1),
    Monsters.Spider => new Monster("Spider", 6, rng.Next(5, 12), 17, 3, 12, 120, rng.Next(20, 51), MonsterScript.Melee, "bites", "spider.png", MonsterFlag.None, 1),
    Monsters.Ogre => new Monster("Ogre", 6, rng.Next(8, 24), 14, 6, 24, 1024, rng.Next(100, 151), MonsterScript.Melee, "swings", "orc.png", MonsterFlag.None, 3),
    Monsters.Wolf => new Monster("Wolf", 6, rng.Next(10, 18), 15, 4, 16, 256, rng.Next(20, 41), MonsterScript.Melee, "bites", "canine.png", MonsterFlag.None, 2),
    Monsters.BlackWidow => new Monster("Black Widow", 5, rng.Next(2, 18), 15, 5, 20, 1024, rng.Next(75, 151), MonsterScript.Melee, "bites", "spider.png", MonsterFlag.Poison, 2),    
    _ => throw new Exception($"Unknown monster id {id}")  
  };
}