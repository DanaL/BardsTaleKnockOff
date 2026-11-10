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

using System.Linq.Expressions;
using SDL;

namespace BardsTaleKnockOff;

// The display and input side of a fight: collects each character's action,
// then shows the round's messages one at a time. The rules live in Battle.
class BattleScreen : IScreen, ICastSpellScreen, IMonsterPickScreen, IPartyPickScreen, IInventorySelectionScreen
{
  // if the player flees some special encounters, like a guarded door, we
  // might want to move them to a different location
  public (int X, int Y) RetreatLoc = (-1, -1); 

  // A message to be written after the player wins the fight
  public string VictoryMessage { get; set; } = "";

  const SDL_Scancode ATTACK_KEY = SDL_Scancode.SDL_SCANCODE_A;
  const SDL_Scancode DEFEND_KEY = SDL_Scancode.SDL_SCANCODE_D;
  const SDL_Scancode HIDE_KEY = SDL_Scancode.SDL_SCANCODE_H;
  const SDL_Scancode CAST_SPELL_KEY = SDL_Scancode.SDL_SCANCODE_C;
  const SDL_Scancode USE_ITEM_KEY = SDL_Scancode.SDL_SCANCODE_U;

  const int PICK_ACTION = 0;
  const int CONFIRM = 1;
  const int RESOLVING_COMBAT = 2;
  const int END_OF_ROUND = 3;
  const int VICTORY = 4;
  const int FLED = 6;
  const int FAILED_TO_FLEE = 7;
  const int DEFEAT = -1;

  ulong _nextMsgAt = 0;
  const ulong MsgDelayMS = 600;

  readonly Battle _battle;
  readonly App _app;
  readonly List<MonsterGroup> _foes;
  int _slot = 0;
  int _state;
  HashSet<SDL_Scancode> _availableActions = [];

  List<BattleChoice> _choices = [];
  BattleAction _pendingAction = BattleAction.Defend;
  SpellInfo? _pendingSpell = null;
  int _chosenInvIdx = -1;
  Item? _itemToUse = null;
  string _abortBattleMessage = "";
  string _image;
  string _caption;

  public BattleScreen(App app, List<MonsterGroup> foes, bool partyAdvance)
  {
    _app = app;
    _foes = foes;
    _battle = new(app.GameState, app.Rng, foes);
    _state = PICK_ACTION;
    _image = foes[0].Image;
    _caption = foes[0].Name;

    if (partyAdvance)
    {
      _battle.StartRound(_choices, true);
      _state = RESOLVING_COMBAT;
    }
  }

  void Reset()
  {
    _app.GameState.Messages.Clear();
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
          if (_battle.Foes.Count == 1 && _battle.Foes[0].Distance == 10)
          {
            _choices.Add(new(_slot, BattleAction.MeleeAttack, 0, -1, null, -1));
            next = true;
          }
          else if (_app.GameState.Party.Members[_slot] is Character mc)
          {
            _app.Screens.Push(new ChooseMonsterGroupScreen(_app, mc, 10, _battle.Foes, this) { Caption = _caption, Image = _image });
            _pendingAction = BattleAction.MeleeAttack;
          }
          break;
        case DEFEND_KEY:
          next = true;
          _choices.Add(new(_slot, BattleAction.Defend, 0, -1, null, -1));
          break;
        case HIDE_KEY:
          next = true;
          _choices.Add(new(_slot, BattleAction.HideInShadows, 0, -1, null, -1));
          break;
        case CAST_SPELL_KEY:
          if (_app.GameState.Party.Members[_slot] is Character c) 
          {
            _pendingAction = BattleAction.CastSpell;
            _app.Screens.Push(new ChooseSpellScreen(_app, c, SpellContext.Combat, this) { Caption = _caption, Image = _image });
          }
          _availableActions = [];
          break;
        case USE_ITEM_KEY:
          if (_app.GameState.Party.Members[_slot] is Character uc) 
          {
            _pendingAction = BattleAction.UseItem;
            InventorySelectionOptions iopts = new(_app, uc, "Use which?", false, "Use", InventoryLabel.None, true);
            _app.Screens.Push(new InventorySelectionScreen(iopts, this) { Caption = _caption, Image = _image });
          }
          _availableActions = [];
          break;          
      }

      if (next)
      {
        ++_slot;
        _availableActions = [];
        if (_slot >= _app.GameState.Party.Alive)
          _state = CONFIRM;
      }
    }
    else if (_state == CONFIRM)
    {
      switch (key.scancode)
      {
        case SDL_Scancode.SDL_SCANCODE_Y:
          _battle.StartRound(_choices, false);
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
      _state = Util.TryToFlee(_app.Rng) ? FLED : FAILED_TO_FLEE;      
    }
    else if (_state == END_OF_ROUND && key.scancode == SDL_Scancode.SDL_SCANCODE_A && _foes[0].Distance > 10)
    {
      _battle.StartRound(_choices, true);
      _state = RESOLVING_COMBAT;
    }
    else if (_state == VICTORY && key.scancode == SDL_Scancode.SDL_SCANCODE_SPACE)
    {
      Cleanup();
      if (!string.IsNullOrEmpty(VictoryMessage))
        _app.GameState.Say(VictoryMessage);
    }
    else if (_state == FLED && key.scancode == SDL_Scancode.SDL_SCANCODE_SPACE)
    {
      SetUpRetreat();

      // Handle case where we were in a special encounter screen
      while (_app.Screens.Top is not AdventureScreen)
        _app.Screens.Pop(); 
    }
    else if (_state == FAILED_TO_FLEE && key.scancode == SDL_Scancode.SDL_SCANCODE_SPACE)
    {
      Reset();
    }
    else if (_state == DEFEAT)
    {
       _app.Screens.Replace(new TPKScreen(_app));
    }
  }

  void SetUpRetreat()
  {
    if (RetreatLoc.X != -1 && RetreatLoc.Y != -1)
    {
      _app.GameState.X = RetreatLoc.X;
      _app.GameState.Y = RetreatLoc.Y;
      _app.GameState.Facing = _app.GameState.Facing.Opposite();
    }

    Cleanup();
  }

  public void Cleanup()
  {
    _app.GameState.Party.ClearCombatBuffs();
    _app.GameState.Messages.Clear();
    _app.Screens.Pop();
  }

  public void Draw(GameUI ui)
  {
    List<string> lines;

    switch (_state)
    {
      case PICK_ACTION:
        lines = PickActionMenu();
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
          try
          {
            while (_battle.Messages.Count == 0 && !_battle.RoundOver)
            _battle.ResolveNextAction();
          }          
          catch (AbortBattleException abe)
          {
            // I knooowwww you aren't supposed to use Exceptions for control
            // flow but sometimes they feel like the natural way to handle
            // something? (Like a character casting FLEE mid-battle)
            _abortBattleMessage = $"{abe.Message} casts FLEE and you are magically whisked away from danger!";
            _state = FLED;
            return;
          }

          if (_battle.Messages.Count > 0)
          {
            _app.GameState.Say(_battle.Messages.Dequeue());
            _nextMsgAt = now + MsgDelayMS;
          }
          else
          {
            _state = PostRound();

            if (_state != DEFEAT)
            {
              _app.GameState.Say("");
              if (_battle.Foes.Any(g => g.Count > 0))
              {
                _app.GameState.Say("You still face:");
                foreach (var group in _battle.Foes.Where(g => g.Count > 0))
                {
                  string gn = group.Count == 1 ? group.Name : group.Name.Pluralize();
                  _app.GameState.Say($"{group.Count} {gn} ({group.Distance}')");
                }
                _app.GameState.Say("");
              } 
            }
          }
        }
        ui.DrawTextPanel(_app.GameState.Messages);
        break;
      case END_OF_ROUND:
        lines = [.. _app.GameState.Messages];
        if (_foes[0].Distance > 10)
        {
          lines.Add("A) Advance");
        }
        lines.AddRange("F) Fight", "R) Run away");

        ui.DrawTextPanel(lines);
        break;
      case VICTORY:
        ui.DrawTextPanel([.. _app.GameState.Messages," - press space to continue -" ]);
        break;
      case FLED:
        if (_abortBattleMessage != "")
          ui.DrawTextPanel([_abortBattleMessage, "",  "- press space to continue - "]);
        else
          ui.DrawTextPanel(["You flee the battle!", "", " - press space to continue - "]);
        break;
      case FAILED_TO_FLEE:
        ui.DrawTextPanel(["You are unable to escape!", "", " - press space to continue - "]);
        break;
      case DEFEAT:
        ui.DrawTextPanel(_app.GameState.Messages);
        break;
    }

    ui.DrawImage(_image, _caption, null);
    ui.DrawRoster(_app.GameState);
  }

  int PostRound()
  {
    var result = _battle.EndRound();
    if (result == RoundResult.Victory)
    {
      Victory();
      return VICTORY;
    }
    else if (result == RoundResult.TPK)
    {
      _app.GameState.Say("");
      _app.GameState.Say("Your party as been defeated!");
      _app.GameState.Say("");
      _app.GameState.Say(" - press space to continue - ");
      return DEFEAT;
    }

    _image = _battle.Foes[0].Image;
    _caption = _battle.Foes[0].Name;

    return END_OF_ROUND;
  }

  void Victory()
  {
    var (xp, gold, monstersDefeated) = _battle.AwardSpoils();

    if (_app.GameState.CurrRegion == "town" || _app.Rng.Next(8) == 0)
    {        
      // If gold given is less than the # of alive characters, I'm giving a
      // little extra gold but who cares...
      int goldPerChar = int.Max(gold / _app.GameState.Party.Alive, 1);
      int xpPerChar = int.Max(xp / _app.GameState.Party.Alive, 1);

      for (int j = 0; j < Party.MAX_PARTY_SIZE; j++)
      {
        if (_app.GameState.Party.Members[j] is Character c && !c.Incapacitated)
        {
          c.XP += xpPerChar;
          c.Gold += goldPerChar;
        }
      }

      _app.GameState.Say("You emerge victorious from battle!");
      _app.GameState.Say("");
      string gp = goldPerChar == 1 ? "a single gold piece" : $"{goldPerChar} gold pieces";
      _app.GameState.Say($"Each survivor receives {xpPerChar} XP and {gp}!");
      _image = "treasure.png";
      _caption = "Victory!";
    }
    else
    {
      Cleanup();
      TreasureChestScreen scr = new(_app, gold, ItemsFound(monstersDefeated), xp);
      _app.Screens.Push(scr);
    }
  }

  int ItemsFound(int monstersDefeated)
  {
    if (_app.GameState.CurrRegion == "town")
      return 0;
    
    int max = _app.GameState.CurrLevel > 3 ? 2 : 1;
    int items = 0;
    for (int j = 0; j < monstersDefeated && items < max; j++)
    {
      if (_app.Rng.Next(8) == 0)
        ++items;
    }

    return items;
  }

  List<string> Confirm()
  {
    List<string> lines = [];

    foreach (var choice in _choices)
    {
      if (_app.GameState.Party.Members[choice.PartySlot] is not Character c)
        continue;

      string monster;
      MonsterGroup group;
      switch (choice.Action)
      {
        case BattleAction.Defend:
          lines.Add($"{c.Name} defends.");
          break;
        case BattleAction.HideInShadows:
          lines.Add($"{c.Name} hides in the shadows.");
          break;
        case BattleAction.MeleeAttack:
          group = _battle.Foes[choice.Group];
          monster = group.Name.ToLower();
          if (group.Count == 0)
            monster = monster.IndefArticle();
          else
            monster = $"the {monster.Pluralize()}";
          lines.Add($"{c.Name} attacks {monster}.");          
          break;
        case BattleAction.ShootBow:
           if (choice.InvIdx != -1)
            {
              group = _battle.Foes[choice.Group];
              monster = group.Name.DefArticle().ToLower();
              if (group.Count > 1)
                monster = monster.Pluralize();
              lines.Add($"{c.Name} shoots at {monster}.");
            }
          break;
        case BattleAction.UseItem:
          if (choice.InvIdx != -1 && _itemToUse is not null)
          {
            lines.Add($"{c.Name} uses {_itemToUse.Name.IndefArticle()}.");
          }
          break;
        case BattleAction.CastSpell:
          if (choice.Spell is not null)
          {
            string s;
            if (choice.Spell.Code == "SMTE")
              s = $"{c.Name} smites";
            else
              s = $"{c.Name} casts {choice.Spell.Code}";

            if (choice.Spell.Target == SpellTarget.Monsters)
            {
              group = _battle.Foes[choice.Group];
              monster = group.Name.ToLower();
              monster = $" on the {monster.Pluralize()}";
              s += monster;
            }
            else if (choice.Spell.Target == SpellTarget.PartyMember && _app.GameState.Party.Members[choice.TargetPartySlot] is Character target)
            {
              s += $" on {target.Name}";
            }
            s += ".";
            lines.Add(s);
          }            
          break;
      }
    }

    lines.AddRange("", "", "", "Y) Yes", "N) No");

    return lines;
  }

  List<string> PickActionMenu()
  {
    List<string> lines = [];

    if (_app.GameState.Party.Members[_slot] is Character c)
    {
      string n = c.Name;
      if (c.Buffs.Any(b => b.Buff == Buff.Hiding))
        n += " (hiding)";
      lines.AddRange($"{n} has these actions available:", "", "", "", "", "");

      bool foesInMelee = _foes[0].Distance == 10;
      if (foesInMelee && (_slot < 3 || c.Buffs.Any(b => b.Buff == Buff.Hiding))) 
      {
        _availableActions.Add(ATTACK_KEY);
        lines.Add("A) Attack foes");
      }

      if (c.MaxMP > 0)
      {
        _availableActions.Add(CAST_SPELL_KEY);
        lines.Add("C) Cast spell");
      }

      if (c.HideInShadows > 0)
      {
        lines.AddRange($"H) Hide in shadows ({c.HideInShadows}% chance)");
        _availableActions.Add(HIDE_KEY);
      }

      if (c.UseableItems)
      {
        lines.AddRange("U) Use item");
        _availableActions.Add(USE_ITEM_KEY);
      }
        
      lines.AddRange("D) Defend");
      _availableActions.Add(DEFEND_KEY);
    }
    
    return lines;
  }

  public void ActionChosen()
  {
    ++_slot;
    _availableActions = [];
    _pendingSpell = null;
    _itemToUse = null;
    _chosenInvIdx = -1;
    if (_slot >= _app.GameState.Party.Alive)
      _state = CONFIRM;
    else
      _state = PICK_ACTION;
  }

  public void SpellChosen(SpellInfo spell)
  {
    switch (spell.Target)
    {
      case SpellTarget.PartyMember:
        _pendingAction = BattleAction.CastSpell;
        _pendingSpell = spell;
        _app.Screens.Push(new ChoosePartyMemberScreen(_app, this, $"Cast {spell.Code} on whom") { Caption = _caption, Image = _image });
        break;
      case SpellTarget.Monsters:
        if (_app.GameState.Party.Members[_slot] is Character c)
        {
          if (_battle.Foes.Count == 1 && _battle.Foes[0].Distance <= spell.Range)
          {
            _choices.Add(new(_slot, BattleAction.CastSpell, 0, -1, spell, -1));
            ActionChosen();
          }
          else
          {
            _pendingAction = BattleAction.CastSpell;
            _pendingSpell = spell;
            _app.Screens.Push(new ChooseMonsterGroupScreen(_app, c, spell.Range, _battle.Foes, this) { Caption = _caption, Image = _image });          
          }          
        }
        
        break;
      default:
        _choices.Add(new(_slot, _pendingAction, -1, -1, spell, _chosenInvIdx));
        ActionChosen();
        break;
    }
  }

  public void MonsterGroupChosen(int monsterGroup)
  {
    _choices.Add(new(_slot, _pendingAction, monsterGroup, -1, _pendingSpell, _chosenInvIdx));
    ActionChosen();
  }

  public void CharacterChosen(int partySlot)
  {
    _choices.Add(new(_slot, _pendingAction, -1, partySlot, _pendingSpell, _chosenInvIdx));
    ActionChosen();
  }

  public void AbortSelection()
  {
    _state = PICK_ACTION;
    _pendingSpell = null;
    _chosenInvIdx = -1;
    _itemToUse = null;
  }

  void SetupBowShot(Character c, int invIdx)
  {
    if (_battle.Foes.Count == 1)
    {
      _choices.Add(new(_slot, BattleAction.ShootBow, 0, -1, null, invIdx));
      _app.Screens.Pop(); 
      ActionChosen();
    }
    else
    {
      _pendingAction = BattleAction.ShootBow;
      _app.Screens.Replace(new ChooseMonsterGroupScreen(_app, c, 90, _battle.Foes, this) { Caption = _caption, Image = _image });  
    }    
  }

  public void ItemSelected(InvSlot slot, Item item, int invIdx)
  {
    if (_app.GameState.Party.Members[_slot] is not Character c)
      return;

    _chosenInvIdx = invIdx;
    _itemToUse = item;

    if (item.Type == ItemType.Ammunition)
    {
      SetupBowShot(c, invIdx);           
    }
    else
    {
      _app.Screens.Pop(); // Unlike Character or Spell selection screens, the Inv Select screen doesn't pop itself
      _choices.Add(new(_slot, BattleAction.UseItem, -1, -1, null, invIdx));
      ActionChosen();
    }
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
  bool _advance = false;

  readonly List<MonsterGroup> _groups = [];
  readonly string _blurb;

  // Monster tables

  static readonly Dictionary<string, List<List<(int, int)>>> _monsterTable = new()
  {
    ["town"] =[ [ (MonsterCatalog.GOBLIN, 8), (MonsterCatalog.ORC, 8), (MonsterCatalog.SKELETON, 4), 
                (MonsterCatalog.FERAL_DOG, 8), (MonsterCatalog.CONJURER_0, 4) ] ],
    ["wiztower"] = [ [ (MonsterCatalog.GOBLIN, 8), (MonsterCatalog.ORC, 8), (MonsterCatalog.SKELETON, 4), (MonsterCatalog.FERAL_DOG, 8),
                        (MonsterCatalog.CONJURER_0, 4), (MonsterCatalog.MAGICIAN_0, 4),  (MonsterCatalog.SPIDER, 4) ] ],
  };

  public EncounterScreen(App app)
  {
    _app = app;
    _app.GameState.Messages.Clear();

    var gs = app.GameState;
    List<(int, int)> monsterOptions = [.. _monsterTable[gs.CurrRegion][gs.CurrLevel]];
    if (gs.CurrRegion == "town" && !gs.DayTime)
      monsterOptions.AddRange((MonsterCatalog.MAGICIAN_0, 4), (MonsterCatalog.SPIDER, 4));
    else if (_app.GameState.CurrTile.Feature == Feature.House)
      monsterOptions.Add((MonsterCatalog.SPIDER, 4));

    List<Monster> monsterGroup = [];
    (int monsterId, int count) = monsterOptions[app.Rng.Next(monsterOptions.Count)];
    _groups.Add(GenGroup(monsterId, count, 10, app.Rng));
    if (gs.CurrRegion == "town" && app.Rng.Next(4) == 0) 
    {
      (int monsterId2, int count2) = monsterOptions[app.Rng.Next(monsterOptions.Count)];
      _groups.Add(GenGroup(monsterId2, count2, 10, app.Rng));
    }
    else if (gs.CurrRegion != "town")
    {
      int extraGroups = _app.Rng.Next(10) switch
      {
        < 2 => 0,
        >= 2 and <= 6 => 1,
        7 or 8 => 2,
        _ => 3
      };

      int distance = 10;
      for (int j = 0; j < extraGroups; j++)
      {
        (int mId, int cnt) = monsterOptions[app.Rng.Next(monsterOptions.Count)];
        var group = GenGroup(mId, cnt, distance, app.Rng);
        if (group.Monsters[0].HasFlag(MonsterFlag.Ranged))
        {
          int extraD = _app.Rng.Next(3) == 0 ? 20 : 10;
          distance += extraD;
          group.Distance += extraD;
        }
        _groups.Add(group);
        distance += 10;
      }      
    }
    
    //_groups = [];
    //_groups.Add(new() { Monsters = [MonsterCatalog.Get(MonsterCatalog.GOBLIN, app.Rng), MonsterCatalog.Get(MonsterCatalog.GOBLIN, app.Rng), MonsterCatalog.Get(MonsterCatalog.GOBLIN, app.Rng)], Distance = 10});
    //_groups.Add(new() { Monsters = [MonsterCatalog.Get(MonsterCatalog.SPIDER, app.Rng) ], Distance = 10});
    //_groups.Add(new() { Monsters = [MonsterCatalog.Get(MonsterCatalog.MAGICIAN_0, app.Rng), MonsterCatalog.Get(MonsterCatalog.MAGICIAN_0, app.Rng)], Distance = 90});
    //_groups.Add(new() { Monsters = [MonsterCatalog.Get(MonsterCatalog.SKELETON, app.Rng), MonsterCatalog.Get(MonsterCatalog.SKELETON, app.Rng), MonsterCatalog.Get(MonsterCatalog.SKELETON, app.Rng)], Distance = 40});
    
    List<string> summaries = [.. _groups.Select(g => $"{g.Count} {(g.Count == 1 ? g.Name : g.Name.Pluralize())} ({g.Distance}')")];
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

  static MonsterGroup GenGroup(int monsterId, int num, int distance, Rng rng)
  {
    MonsterGroup monsters = new() { Distance = distance };
    int count = rng.Next(num) + 1;
    for (int j = 0; j < count; j++)
      monsters.Monsters.Add(MonsterCatalog.Get(monsterId, rng));

    return monsters;
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
      _app.Screens.Replace(new BattleScreen(_app, _groups, false));
    }
    else if (_state == PRELUDE && _advance && key.scancode == SDL_Scancode.SDL_SCANCODE_A)
    {
      _app.Screens.Replace(new BattleScreen(_app, _groups, true));
    }
  }

  public void Draw(GameUI ui)
  {
    List<string> lines = [ _blurb, ""];

    if (_state == PRELUDE) 
    {
      lines.AddRange("Will your stalwart band elect to: ", "");
      if (_groups[0].Distance > 10) 
      {
        lines.Add("(A) Advance");
        _advance = true;
      }
      lines.AddRange("(F) Fight", "(R) Run");
    }
    else if (_state == FAILED_TO_FLEE)
    {
      lines.AddRange("You were unable to escape!", "", "Press any key to continue");
    }
    else if (_state == FLED)
    {
      lines.AddRange("Discretion is the better part of valour...", "", "Press any key to continue");
    }

    string image = _groups[0].Image;
    string monsterName = _groups[0].Name;

    ui.DrawImage(image, monsterName, null);
    ui.DrawTextPanel(lines);
    ui.DrawRoster(_app.GameState);
  }
}
