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

enum BattleAction { MeleeAttack, Defend, MonsterAction, HideInShadows }

record struct BattleChoice(int Slot, BattleAction Action,  int Group);

class BattleScreen(App app, List<List<Monster>> foes) : IScreen
{
  const SDL_Scancode ATTACK_KEY = SDL_Scancode.SDL_SCANCODE_A;
  const SDL_Scancode DEFEND_KEY = SDL_Scancode.SDL_SCANCODE_D;
  const SDL_Scancode HIDE_KEY = SDL_Scancode.SDL_SCANCODE_H;

  const int PICK_ACTION = 0;
  const int CONFIRM = 1;
  const int RESOLVING_COMBAT = 2;
  const int END_OF_ROUND = 3;
  const int VICTORY = 4;
  const int PICK_MELEE_GROUP = 5;
  const int FLED = 6;
  const int FAILED_TO_FLEE = 7;

  ulong _nextMsgAt = 0;
  const ulong MsgDelayMS = 600;

  readonly Queue<string> _messageQ = new();

  int _slot = 0;
  int _state = PICK_ACTION;
  HashSet<SDL_Scancode> _availableActions = [];

  List<BattleChoice> _choices = [];

  // The round's pending actions, resolved one at a time as messages are shown
  // so the roster never gets ahead of the text.
  readonly PriorityQueue<BattleChoice, int> _actions = new();
  int _actingGroup = -1;
  int _actingMonster = 0;

  bool RoundOver => _actions.Count == 0 && _actingGroup < 0;

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
        case ATTACK_KEY:
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
        case DEFEND_KEY:
          next = true;
          _choices.Add(new(_slot, BattleAction.Defend, 0));
          break;
        case HIDE_KEY:
          next = true;
          _choices.Add(new(_slot, BattleAction.HideInShadows, 0));
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
          StartRound();
          _state = RESOLVING_COMBAT;
          break;
        case SDL_Scancode.SDL_SCANCODE_N:
          Reset();
          break;
      }
    }
    else if (_state == RESOLVING_COMBAT && key.scancode == SDL_Scancode.SDL_SCANCODE_SPACE)
    {
      _nextMsgAt = 0;
    }
    else if (_state == END_OF_ROUND && key.scancode == SDL_Scancode.SDL_SCANCODE_F)
    {
      Reset();
    }
    else if (_state == END_OF_ROUND && key.scancode == SDL_Scancode.SDL_SCANCODE_R)
    {
      _state = Util.TryToFlee(app.Rng) ? FLED : FAILED_TO_FLEE;      
    }
    else if (_state == VICTORY && key.scancode == SDL_Scancode.SDL_SCANCODE_SPACE)
    {
      ClearBuffs();
      app.GameState.Messages.Clear();
      app.Screens.Pop();
    }
    else if (_state == FLED && key.scancode == SDL_Scancode.SDL_SCANCODE_SPACE)
    {
      ClearBuffs();
      app.GameState.Messages.Clear();
      app.Screens.Pop();
    }
    else if (_state == FAILED_TO_FLEE && key.scancode == SDL_Scancode.SDL_SCANCODE_SPACE)
    {
      Reset();
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
      case RESOLVING_COMBAT:
        ulong now = SDL3.SDL_GetTicks();
        if (now >= _nextMsgAt)
        {
          // Some actions produce no messages (eg., the attacker died earlier
          // in the round), so keep going until one does.
          while (_messageQ.Count == 0 && !RoundOver)
            ResolveNextAction();

          if (_messageQ.Count > 0)
          {
            app.GameState.Say(_messageQ.Dequeue());
            _nextMsgAt = now + MsgDelayMS;
          }
          else
          {
            app.GameState.Say("");
            if (foes.Any(g => g.Count > 0))
            {
              app.GameState.Say("You still face:");
              foreach (var group in foes.Where(g => g.Count > 0))
              {
                string gn = group.Count == 1 ? group[0].Name : group[0].Name.Pluralize();
                app.GameState.Say($"{group.Count} {gn}");
              }
              app.GameState.Say("");
            }

            _state = PostRound();
          }
        }
        ui.DrawTextPanel(app.GameState.Messages);
        break;
      case END_OF_ROUND:
        ui.DrawTextPanel([.. app.GameState.Messages,"F)ight", "R)un away"]);
        break;
      case VICTORY:
        ui.DrawTextPanel([.. app.GameState.Messages,"- press space to continue -"]);
        break;
      case FLED:
        ui.DrawTextPanel(["You flee the battle!", "", "- press space to continue -"]);
        break;
      case FAILED_TO_FLEE:
        ui.DrawTextPanel(["You are unable to escape!", "", "- press space to continue -"]);
        break;
    }

    ui.DrawImage(_image, _caption, app.GameState);
    ui.DrawRoster(app.GameState.Party);
  }

  int PostRound()
  {
    ++app.GameState.Turn;
    if (app.GameState.Party.Alive == 0)
    {
      // Uh-oh, TPK   
    }

    foreach (Character member in app.GameState.Party.Members.OfType<Character>())
    {
      member.Buffs = [.. member.Buffs.Where(b => b.ExpiresOn > app.GameState.Turn)];
    }

    app.GameState.Party.MoveIncapacitatedToBack();

    List<List<Monster>> remainingFoes = [];
    foreach (var group in foes)
    {
      if (group.Count > 0)
        remainingFoes.Add(group);
    }

    int nextState = END_OF_ROUND;
    if (remainingFoes.Count == 0)
    {
      Victory();
      nextState = VICTORY;
    }
    else
    {
      foes = remainingFoes;
      _image = foes[0][0].Image;
      _caption = foes[0][0].Name;
    }

    return nextState;
  }

  // Eventually, I need to add finding the occasional item!
  void Victory()
  {
    // If gold given is less than the # of alive characters, I'm giving a
    // little extra gold but who cares...
    int goldPerChar = int.Max(_goldWon / app.GameState.Party.Alive, 1);
    int xpPerChar = int.Max(_xpWon / app.GameState.Party.Alive, 1);

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

  // Clear out any buffs that only last for the duration of combat
  void ClearBuffs()
  {
    for (int j = 0; j < app.GameState.Party.Count; j++)
    {
      if (app.GameState.Party.Members[j] is Character c)
        c.Buffs = [.. c.Buffs.Where(b => !b.CombatOnly)];
    }
  }

  void StartRound()
  {    
    foreach (var choice in _choices)
    {
      if (app.GameState.Party.Members[choice.Slot] is not Character c || c.Incapacitated)
        continue;

      if (choice.Action == BattleAction.Defend)
      {
        c.Buffs.Add(new TempBuff(Buff.Defending, app.GameState.Turn, true));
        _messageQ.Enqueue($"{c.Name} defends.");
      }        
      else if (choice.Action == BattleAction.MeleeAttack) 
      {
        _actions.Enqueue(choice, Initiative(c));
      }
      else if (choice.Action == BattleAction.HideInShadows)
      {
        // Hide in shadows should happen before other actions
        _actions.Enqueue(choice, -int.MaxValue);
      }
    }

    for (int n = 0; n < foes.Count; n++)
    {
      int roll = app.Rng.Next(1, 24);
      BattleChoice monsterChoice = new(n, BattleAction.MonsterAction, n);
      _actions.Enqueue(monsterChoice, -roll);
    }

    _state = RESOLVING_COMBAT;
  }

  // Resolve a single attack. When a monster group's turn comes up, each
  // monster in it attacks on its own step. Nothing else acts until the
  // whole group has gone, so the indexes into the group stay valid.
  void ResolveNextAction()
  {
    if (_actingGroup >= 0)
    {
      List<Monster> group = foes[_actingGroup];
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
      if (app.GameState.Party.Members[choice.Slot] is not Character c || c.Incapacitated)
        return;

      switch (choice.Action)
      {
        case BattleAction.MeleeAttack:
          PlayerMeleeAttack(c, choice.Group);
          break;
        case BattleAction.HideInShadows:
          int roll = app.Rng.Next(0, 100) + 1;
          if (roll <= c.HideInShadows)
          {
            _messageQ.Enqueue($"{c.Name} slips into the shadows!");
            c.Buffs.Add(new TempBuff(Buff.Hiding, ulong.MaxValue, true));
          }
          else
          {
            _messageQ.Enqueue($"{c.Name} fails to hide!");
            c.Buffs = [.. c.Buffs.Where(b => b.Buff != Buff.Hiding )];
          }
          break;
      }
    }
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
      if (app.GameState.Party.Members[j] is Character c && !c.Incapacitated && !c.Buffs.Any(b => b.Buff == Buff.Hiding))
        targets.Add(j);
    }

    if (targets.Count == 0)
      return;

    int s = targets[app.Rng.Next(targets.Count)];
    if (app.GameState.Party.Members[s] is Character target)
    {
      string n = m.Name.ToLower().DefArticle().Capitalize();
      int roll = app.Rng.Next(1, 21);
      int targetAC = m.THAC0 - target.AC;
      if (target.Buffs.Any(b => b.Buff == Buff.Defending))
        targetAC += 5; // Not sure what BT1 did, but -5 is roughly equip to Disadvantage in 5e
                       // and what you get for taking the Dodge action
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
    bool hiding = c.Buffs.Any(b => b.Buff == Buff.Hiding);
    
    for (int j = 0; j < c.NumOfMeleeAttacks; j++)
    {
      int roll = app.Rng.Next(1, 21) + strMod;
      if (weapon is not null)
        roll += weapon.AttackBonus;

      if (hiding)
        roll += 5;
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

    _messageQ.Enqueue(s);

    // If you attack from hiding, you are no longer hiding
    c.Buffs = [.. c.Buffs.Where(b => b.Buff != Buff.Hiding)];

    int monsterHit = app.Rng.Next(foes[monsterGroup].Count);
    if (hiding && hits > 0)
    {
      _xpWon += target.XP;
      _goldWon += target.Gold;
      foes[monsterGroup].RemoveAt(monsterHit);      
    }
    else if (dmg >= foes[monsterGroup][monsterHit].HP)
    {
      _messageQ.Enqueue($"{targetName.Capitalize()} is killed!");
      _xpWon += target.XP;
      _goldWon += target.Gold;
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
        case BattleAction.HideInShadows:
          lines.Add($"{c.Name} hides in the shadows.");
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
    if (app.GameState.Party.Members[_slot] is not Character c)
      return [];

    List<string> lines = [ "Attack which group:", ""];

    int n = foes[0].Count;
    string m = n == 1 ? foes[0][0].Name : foes[0][0].Name.Pluralize();
    lines.Add($"a) {n} {m}");

    n = foes[1].Count;
    m = n == 1 ? foes[1][0].Name : foes[1][0].Name.Pluralize();
    lines.Add($"b) {n} {m}");

    _availableActions = [ SDL_Scancode.SDL_SCANCODE_A, SDL_Scancode.SDL_SCANCODE_B ];

    if (c.Buffs.Any(b => b.Buff == Buff.Hiding))
    {
      if (foes.Count > 2)
      {
        n = foes[2].Count;
        m = n == 1 ? foes[2][0].Name : foes[2][0].Name.Pluralize();
        lines.Add($"c) {n} {m}");
        _availableActions.Add(SDL_Scancode.SDL_SCANCODE_C);
      }
      if (foes.Count > 3) 
      {
        n = foes[3].Count;
        m = n == 1 ? foes[3][0].Name : foes[3][0].Name.Pluralize();
        lines.Add($"d) {n} {m}");
        _availableActions.Add(SDL_Scancode.SDL_SCANCODE_D);
      }
    }

    return lines;
  }

  List<string> PickAction()
  {
    List<string> lines = [];

    if (app.GameState.Party.Members[_slot] is Character c)
    {
      lines.AddRange($"{c.Name} has these actions available:", "", "", "", "", "");

      if (_slot < 3 || c.Buffs.Any(b => b.Buff == Buff.Hiding)) 
      {
        _availableActions.Add(ATTACK_KEY);
        lines.AddRange("A)ttack foes");
      }

      if (c.HideInShadows > 0)
      {
        lines.AddRange($"H)ide in shadows ({c.HideInShadows}% chance)");
        _availableActions.Add(HIDE_KEY);
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
      _state = Util.TryToFlee(_app.Rng) ? FLED : FAILED_TO_FLEE;
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

    ui.DrawImage(image, monsterName, _app.GameState);
    ui.DrawTextPanel(lines);
    ui.DrawRoster(_app.GameState.Party);
  }
}