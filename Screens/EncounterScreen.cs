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

using SDL;

namespace BardsTaleKnockOff;

enum BattleAction { MeleeAttack, Defend, MonsterAction }

record struct BattleChoice(int Slot, BattleAction Action,  int Group);

class BattleScreen(App app, List<List<Monster>> foes) : IScreen
{
  const SDL_Scancode ATTACK_KEY = SDL_Scancode.SDL_SCANCODE_A;
  const SDL_Scancode DEFEND_KEY = SDL_Scancode.SDL_SCANCODE_D;

  const int PICK_ACTION = 0;
  const int CONFIRM = 1;
  const int SHOW_RESULTS = 2;
  const int END_OF_ROUND = 3;
  const int VICTORY = 4;
  const int PICK_MELEE_GROUP = 5;

  ulong _nextMsgAt = 0;
  const ulong MsgDelayMS = 600;

  readonly Queue<string> _messageQ = new();

  int _slot = 0;
  int _state = PICK_ACTION;
  HashSet<SDL_Scancode> _availableActions = [];

  List<BattleChoice> _choices = [];

  string _image = foes[0][0].Image;
  string _caption = foes[0][0].Name;

  int _xpWon = 0;
  int _goldWon = 0;

  void Reset()
  {
    app.GameState.Messages.Clear();
    _choices.Clear();
    _slot = 0;
    _state = PICK_ACTION;
  }

  public void OnKey(SDL_KeyboardEvent key)
  {
    if (_state == PICK_ACTION) 
    {
      if (!_availableActions.Contains(key.scancode))
        return;

      bool next = false;
      switch (key.scancode)
      {
        case SDL_Scancode.SDL_SCANCODE_A:
          if (foes.Count == 1) 
          {
            _choices.Add(new(_slot, BattleAction.MeleeAttack, 0));
            next = true;
          }
          else
          {
            _state = PICK_MELEE_GROUP;
            _availableActions = [];
          }
          break;
        case SDL_Scancode.SDL_SCANCODE_D:
          next = true;
          _choices.Add(new(_slot, BattleAction.Defend, 0));
          break;
      }

      if (next)
      {
        ++_slot;
        _availableActions = [];
        if (_slot >= app.GameState.Party.Alive)
          _state = CONFIRM;
      }
    }
    else if (_state == PICK_MELEE_GROUP)
    {
      if (!_availableActions.Contains(key.scancode))
        return;

      switch (key.scancode)
      {
        case SDL_Scancode.SDL_SCANCODE_A:
          _choices.Add(new(_slot, BattleAction.MeleeAttack, 0));
          break;
        case SDL_Scancode.SDL_SCANCODE_B:
          _choices.Add(new(_slot, BattleAction.MeleeAttack, 1));
          break;
      }

      ++_slot;
      _availableActions = [];
      if (_slot >= app.GameState.Party.Alive)
        _state = CONFIRM;
      else
        _state = PICK_ACTION;
    }
    else if (_state == CONFIRM)
    {
      switch (key.scancode)
      {
        case SDL_Scancode.SDL_SCANCODE_Y:
          CombatRound();          
          _state = SHOW_RESULTS;
          break;
        case SDL_Scancode.SDL_SCANCODE_N:
          Reset();
          break;
      }
    }
    else if (_state == SHOW_RESULTS && key.scancode == SDL_Scancode.SDL_SCANCODE_SPACE)
    {
      _nextMsgAt = 0;
    }
    else if (_state == END_OF_ROUND && key.scancode == SDL_Scancode.SDL_SCANCODE_SPACE)
    {
      Reset();
      PostRound();      
    }
    else if (_state == VICTORY && key.scancode == SDL_Scancode.SDL_SCANCODE_SPACE)
    {
      app.GameState.Messages.Clear();
      app.Screens.Pop();
    }
  }

  public void Draw(GameUI ui)
  {
    List<string> lines;

    switch (_state)
    {
      case PICK_ACTION:
        lines = PickAction();
        ui.DrawTextPanel(lines);
        break;
      case PICK_MELEE_GROUP:
        lines = PickMeleeGroup();
        ui.DrawTextPanel(lines);
        break;
      case CONFIRM:
        lines = Confirm();
        ui.DrawTextPanel(lines);
        break;
      case SHOW_RESULTS:
        ulong now = SDL3.SDL_GetTicks();
        if (_messageQ.Count > 0 && now >= _nextMsgAt)
        {
          app.GameState.Say(_messageQ.Dequeue());
          _nextMsgAt = now + MsgDelayMS;
        }
        if (_messageQ.Count == 0)
          _state = END_OF_ROUND;
        ui.DrawTextPanel(app.GameState.Messages);
        break;
      case VICTORY:
      case END_OF_ROUND:
        ui.DrawTextPanel(app.GameState.Messages);        
        break;
    }

    ui.DrawImage(_image, _caption);
    ui.DrawRoster(app.GameState.Party);
  }

  void PostRound()
  {
    if (app.GameState.Party.Alive == 0)
    {
      // Uh-oh, TPK   
    }

    app.GameState.Party.MoveIncapacitatedToBack();

    List<List<Monster>> remainingFoes = [];
    foreach (var group in foes)
    {
      if (group.Count > 0)
        remainingFoes.Add(group);
    }

    if (remainingFoes.Count == 0)
    {
      Victory();
      _state = VICTORY;
    }
    else
    {
      foes = remainingFoes;
    
    _image = foes[0][0].Image;
    _caption = foes[0][0].Name;  
    }    
  }

  // Eventually, I need to add finding the occasional item!
  void Victory()
  {
    // If gold given is less than the # of alive characters, I'm giving a
    // little extra gold but who cares...
    int goldPerChar = int.Max(_goldWon / app.GameState.Party.Alive, 1);
    int xpPerChar = int.Max(_xpWon / app.GameState.Party.Alive, 1);

    app.GameState.Messages.Clear();
    app.GameState.Say("You emerge victorious from battle!");
    app.GameState.Say("");
    string gp = goldPerChar == 1 ? "a single gold piece" : $"{goldPerChar} gold pieces";
    app.GameState.Say($"Each survivor receives {xpPerChar} XP and {gp}!");

    for (int j = 0; j < app.GameState.Party.Count; j++)
    {
      if (app.GameState.Party.Members[j] is Character c && !c.Incapacitated)
      {
        c.XP += xpPerChar;
        c.Gold += goldPerChar;
      }
    }

    _image = "treasure.png";
    _caption = "Victory!";
  }

  void CombatRound()
  {
    PriorityQueue<BattleChoice, int> actions = new();
    Dictionary<int, BattleChoice> characters = [];
    
    foreach (var choice in _choices)
    {
      if (app.GameState.Party.Members[choice.Slot] is not Character c || c.Incapacitated)
        continue;

      characters[choice.Slot] = choice;
      if (choice.Action == BattleAction.Defend)
        _messageQ.Enqueue($"{c.Name} defends.");      
      else if (choice.Action == BattleAction.MeleeAttack)
        actions.Enqueue(choice, Initiative(c));      
    }
   
    for (int n = 0; n < foes.Count; n++)
    {
      int roll = app.Rng.Next(1, 24);
      BattleChoice monsterChoice = new(n, BattleAction.MonsterAction, n);
      actions.Enqueue(monsterChoice, -roll);
    }  

    do
    {
      var choice = actions.Dequeue();
      if (choice.Action == BattleAction.MonsterAction)
      {
        MonsterAction(choice.Group);
      }
      else
      {
        if (app.GameState.Party.Members[choice.Slot] is not Character c || c.Incapacitated)
          continue;

        if (choice.Action == BattleAction.MeleeAttack)     
        {
          PlayerMeleeAttack(c, choice.Group);
        }
      }
    }
    while (actions.Count > 0);
    
    _messageQ.Enqueue("");
    _messageQ.Enqueue("- space to continue -");

    _state = SHOW_RESULTS;
  }

  // Only monsters in the first 2 ranks can engage in melee
  // Not sure what rate they should do their special attacks at yet
  void MonsterAction(int group)
  {
    if (foes[group].Count == 0)
      return;

    foreach (Monster m in foes[group])
    {
      // eventually check for special abilities
      if (group > 1)
        continue;
      
      // also have to take into account the Special slot eventually
      List<int> targets = [];
      for (int j = 0; j < 3; j++)
      {
        if (app.GameState.Party.Members[j] is Character c && c.HP > 0)
          targets.Add(j);
      }

      if (targets.Count == 0)
        continue;

      int s = targets[app.Rng.Next(targets.Count)];
      if (app.GameState.Party.Members[s] is Character target)
      {
        string n = m.Name.ToLower().DefArticle().Capitalize();
        int roll = app.Rng.Next(1, 21);
        int targetAC = m.THAC0 - target.AC;
        if (roll > targetAC)
        {
          int dmg = app.Rng.Next(m.DmgLow, m.DmgHigh + 1);
          _messageQ.Enqueue($"{n} {m.Verb} {target.Name} and hits for {dmg} hp!");
          target.HP -= dmg;

          if (target.HP < 1)
          {
            _messageQ.Enqueue($"{target.Name} is killed!");
            target.Condition = "DEAD";
          }
        }
        else
        {
          _messageQ.Enqueue($"{n} {m.Verb} {target.Name} and misses!");
        }
      }
    }  
  }

  void PlayerMeleeAttack(Character c, int monsterGroup)
  {        
    // I need to check what happens in BT if a group you've targeted
    // is eliminated before you act...
    if (foes[monsterGroup].Count == 0)
      return;

    Monster target = foes[monsterGroup][0];
    Item? weapon = c.ReadiedWeapon;
     string s = weapon is not null ? $"{c.Name} attacks " : $"{c.Name} punches ";
     s += target.Name.IndefArticle().ToLower();
     int attacks = c.NumOfMeleeAttacks;
     if (attacks > 1)
      s += $" {attacks} times ";

    int strMod = Character.StatToMod(c.Str);
    
    int hits = 0;
    int dmg = 0;
    
    for (int j = 0; j < c.NumOfMeleeAttacks; j++)
    {
      int roll = app.Rng.Next(1, 21) + strMod;
      if (weapon is not null)
        roll += weapon.AttackBonus;

      // Eventually there will be other buffs?

      int targetAC = c.THAC0 - target.AC;
      if (roll > targetAC)
      {
        ++hits;
        dmg = strMod;
        if (weapon is null)
          dmg += 1;
        else 
        {
          dmg += weapon.DamageBonus;
          for (int k = 0; k < weapon.NumDamageDice; k++)
            dmg += app.Rng.Next(weapon.DamageDie) + 1;
        }
      }      
    } 

    if (hits == 0)
      s += " and misses!";
    else if (hits == 1)
      s+= $" and hits for {dmg} dmg!";
    else 
      s+= $" and hits {hits} times for {dmg} dmg!";

    _messageQ.Enqueue(s);

    int monsterHit = app.Rng.Next(foes[monsterGroup].Count);
    if (dmg >= foes[monsterGroup][monsterHit].HP)
    {
      _messageQ.Enqueue($"{target.Name.DefArticle().ToLower().Capitalize()} is killed!");
      _xpWon += target.XP;
      _goldWon += app.Rng.Next(target.Gold) + 1;
      foes[monsterGroup].RemoveAt(monsterHit);
    }
    else if (dmg > 0)
    {
      Monster m = foes[monsterGroup][monsterHit];
      foes[monsterGroup][monsterHit] = m with { HP = m.HP - dmg };
    }
  }

  int Initiative(Character c)
  {
    int roll = app.Rng.Next(20) + 1 + Character.StatToMod(c.Dex);
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

  List<string> Confirm()
  {
    List<string> lines = [];

    foreach (var choice in _choices)
    {
      if (app.GameState.Party.Members[choice.Slot] is not Character c)
        continue;

      switch (choice.Action)
      {
        case BattleAction.Defend:
          lines.Add($"{c.Name} defends.");
          break;
        case BattleAction.MeleeAttack:
          var group = foes[choice.Group];
          string monster = group[0].Name.ToLower();
          if (group.Count == 0)
            monster = monster.IndefArticle();
          else
            monster = $"the {monster.Pluralize()}";
          lines.Add($"{c.Name} attacks {monster}.");          
          break;
      }
    }

    lines.AddRange("", "", "", "Y)es", "N)o");

    return lines;
  }

  // A rogue hiding in shadows should be able to hit any group
  List<string> PickMeleeGroup()
  {
    List<string> lines = [ "Attack which group:", ""];

    int n = foes[0].Count;
    string m = n == 1 ? foes[0][0].Name : foes[0][0].Name.Pluralize();
    lines.Add($"a) {n} {m}");

    n = foes[1].Count;
    m = n == 1 ? foes[1][0].Name : foes[1][0].Name.Pluralize();
    lines.Add($"b) {n} {m}");

    _availableActions = [ SDL_Scancode.SDL_SCANCODE_A, SDL_Scancode.SDL_SCANCODE_B ];

    return lines;
  }

  List<string> PickAction()
  {
    List<string> lines = [];

    if (app.GameState.Party.Members[_slot] is Character c)
    {
      lines.AddRange($"{c.Name} has these actions available:", "", "", "", "", "");

      if (_slot < 3) 
      {
        _availableActions.Add(ATTACK_KEY);
        lines.AddRange("A)ttack foes");
      }
      lines.AddRange("D)efend");
      _availableActions.Add(DEFEND_KEY);
    }
    
    return lines;
  }  
}

class EncounterScreen : IScreen
{
  readonly App _app;

  // Not actually sure what your chances of successfully fleeing in BT1
  // but it worked most of the time, iirc
  const int ODDS_TO_FLEE = 75;

  const int PRELUDE = 0;
  const int PARTY_SURPRISED = 1;
  const int MONSTERS_SURPRISED = 2;
  const int FAILED_TO_FLEE = 3;
  const int FLED = 4;

  int _state = PRELUDE;
  
  readonly List<List<Monster>> _groups = [];
  readonly string _blurb;

  public EncounterScreen(App app)
  {
    _app = app;
    _app.GameState.Messages.Clear();

    _groups.Add(MonsterCatalog.GroupForLevel(app.GameState.CurrLevel, app.Rng));
    if (app.GameState.CurrLevel == 0 && app.Rng.Next(4) == 0)
      _groups.Add(MonsterCatalog.GroupForLevel(app.GameState.CurrLevel, app.Rng));

     List<string> summaries = [.. _groups.Select(g => $"{g.Count} {(g.Count == 1 ? g[0].Name : g[0].Name.Pluralize())}")];
    string desc = summaries.Count switch
    {
      0 => "",
      1 => summaries[0],
      2 => $"{summaries[0]} and {summaries[1]}",
      _ => string.Join(", ", summaries.Take(summaries.Count - 1)) + ", and " + summaries[^1]
    };

    _blurb = _app.Rng.Next(4) switch
    {
      0 => $"You find yourself face-to-face with {desc}!",
      1 => $"The sudden scream of battle brings your party to a half. You are confront {desc}!",
      2 => $"You curse as monsters charge you. You face {desc}!",
      _ => $"The denizens of this foul place assault you! You face {desc}!"
    };
  }

  public void OnKey(SDL_KeyboardEvent key)
  {    
    if (_state == PRELUDE && key.scancode == SDL_Scancode.SDL_SCANCODE_R)
    {
      // Maybe eventually items or having particular classes in your party
      // or some items will increase your chance to flee?
      if (_app.Rng.Next(100) < ODDS_TO_FLEE)
      {
        _state = FLED;
      }
      else
      {
        _state = FAILED_TO_FLEE;
      }
    }
    else if (_state == FLED)
    {
      _app.Screens.Pop();
    }
    else if ((_state == PRELUDE && key.scancode == SDL_Scancode.SDL_SCANCODE_F) || _state == FAILED_TO_FLEE)
    {
      _app.Screens.Replace(new BattleScreen(_app, _groups));
    }
  }

  public void Draw(GameUI ui)
  {
    List<string> lines = [ _blurb, ""];

    if (_state == PRELUDE)
      lines.Add("Will your stalwart band elect to (F)ight for (R)un?");
    else if (_state == FAILED_TO_FLEE)
      lines.AddRange("You were unable to escape!", "", "Press any key to continue");
    else if (_state == FLED)
      lines.AddRange("Discretion is the better part of valour...", "", "Press any key to continue");

    string image = _groups[0][0].Image;
    string monsterName = _groups[0][0].Name;

    ui.DrawImage(image, monsterName);
    ui.DrawTextPanel(lines);
    ui.DrawRoster(_app.GameState.Party);
  }
}