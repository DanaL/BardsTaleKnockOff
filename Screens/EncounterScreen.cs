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

// The display and input side of a fight: collects each character's action,
// then shows the round's messages one at a time. The rules live in Battle.
class BattleScreen(App app, List<List<Monster>> foes) : IScreen, ICastSpellScreen, IMonsterPickScreen, IPartyPickScreen
{
  const SDL_Scancode ATTACK_KEY = SDL_Scancode.SDL_SCANCODE_A;
  const SDL_Scancode DEFEND_KEY = SDL_Scancode.SDL_SCANCODE_D;
  const SDL_Scancode HIDE_KEY = SDL_Scancode.SDL_SCANCODE_H;
  const SDL_Scancode CAST_SPELL_KEY = SDL_Scancode.SDL_SCANCODE_C;

  const int PICK_ACTION = 0;
  const int CONFIRM = 1;
  const int RESOLVING_COMBAT = 2;
  const int END_OF_ROUND = 3;
  const int VICTORY = 4;
  const int FLED = 6;
  const int FAILED_TO_FLEE = 7;

  ulong _nextMsgAt = 0;
  const ulong MsgDelayMS = 600;

  readonly Battle _battle = new(app.GameState, app.Rng, foes);

  int _slot = 0;
  int _state = PICK_ACTION;  
  HashSet<SDL_Scancode> _availableActions = [];

  List<BattleChoice> _choices = [];
  BattleAction _pendingAction = BattleAction.Defend;
  SpellInfo? _pendingSpell = null;

  string _image = foes[0][0].Image;
  string _caption = foes[0][0].Name;

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
          if (_battle.Foes.Count == 1)
          {
            _choices.Add(new(_slot, BattleAction.MeleeAttack, 0, -1));
            next = true;
          }
          else if (app.GameState.Party.Members[_slot] is Character mc)
          {
            app.Screens.Push(new ChooseMonsterGroupScreen(app, mc, _battle.Foes, this) { Caption = _caption, Image = _image });
            _pendingAction = BattleAction.MeleeAttack;
          }
          break;
        case DEFEND_KEY:
          next = true;
          _choices.Add(new(_slot, BattleAction.Defend, 0, -1));
          break;
        case HIDE_KEY:
          next = true;
          _choices.Add(new(_slot, BattleAction.HideInShadows, 0, -1));
          break;
        case CAST_SPELL_KEY:
          if (app.GameState.Party.Members[_slot] is Character c) 
          {
            _pendingAction = BattleAction.CastSpell;
            app.Screens.Push(new ChooseSpellScreen(app, c, true, this) { Caption = _caption, Image = _image });
          }
          _availableActions = [];
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
    else if (_state == CONFIRM)
    {
      switch (key.scancode)
      {
        case SDL_Scancode.SDL_SCANCODE_Y:
          _battle.StartRound(_choices);
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
      app.GameState.Party.ClearCombatBuffs();
      app.GameState.Messages.Clear();
      app.Screens.Pop();
    }
    else if (_state == FLED && key.scancode == SDL_Scancode.SDL_SCANCODE_SPACE)
    {
      app.GameState.Party.ClearCombatBuffs();
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
          while (_battle.Messages.Count == 0 && !_battle.RoundOver)
            _battle.ResolveNextAction();

          if (_battle.Messages.Count > 0)
          {
            app.GameState.Say(_battle.Messages.Dequeue());
            _nextMsgAt = now + MsgDelayMS;
          }
          else
          {
            app.GameState.Say("");
            if (_battle.Foes.Any(g => g.Count > 0))
            {
              app.GameState.Say("You still face:");
              foreach (var group in _battle.Foes.Where(g => g.Count > 0))
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

    ui.DrawImage(_image, _caption, null);
    ui.DrawRoster(app.GameState.Party);
  }

  int PostRound()
  {
    if (_battle.EndRound() == RoundResult.Victory)
    {
      Victory();
      return VICTORY;
    }

    _image = _battle.Foes[0][0].Image;
    _caption = _battle.Foes[0][0].Name;

    return END_OF_ROUND;
  }

  void Victory()
  {
    var (xpPerChar, goldPerChar) = _battle.AwardSpoils();

    app.GameState.Say("You emerge victorious from battle!");
    app.GameState.Say("");
    string gp = goldPerChar == 1 ? "a single gold piece" : $"{goldPerChar} gold pieces";
    app.GameState.Say($"Each survivor receives {xpPerChar} XP and {gp}!");

    _image = "treasure.png";
    _caption = "Victory!";
  }

  List<string> Confirm()
  {
    List<string> lines = [];

    foreach (var choice in _choices)
    {
      if (app.GameState.Party.Members[choice.Slot] is not Character c)
        continue;

      string monster;
      List<Monster> group;
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
          monster = group[0].Name.ToLower();
          if (group.Count == 0)
            monster = monster.IndefArticle();
          else
            monster = $"the {monster.Pluralize()}";
          lines.Add($"{c.Name} attacks {monster}.");          
          break;
        case BattleAction.CastSpell:
          if (choice.Spell is not null)
          {
            string s = $"{c.Name} casts {choice.Spell.Code}";
            if (choice.Spell.Target == SpellTarget.Monsters)
            {
              group = _battle.Foes[choice.Group];
              monster = group[0].Name.ToLower();
              monster = $" on the {monster.Pluralize()}";
              s += monster;
            }
            else if (choice.Spell.Target == SpellTarget.Party && app.GameState.Party.Members[choice.PartySlot] is Character target)
            {
              s += $" on {target.Name}";
            }
            s += ".";
            lines.Add(s);
          }            
          break;
      }
    }

    lines.AddRange("", "", "", "Y)es", "N)o");

    return lines;
  }

  List<string> PickActionMenu()
  {
    List<string> lines = [];

    if (app.GameState.Party.Members[_slot] is Character c)
    {
      string n = c.Name;
      if (c.Buffs.Any(b => b.Buff == Buff.Hiding))
        n += " (hiding)";
      lines.AddRange($"{n} has these actions available:", "", "", "", "", "");

      if (_slot < 3 || c.Buffs.Any(b => b.Buff == Buff.Hiding)) 
      {
        _availableActions.Add(ATTACK_KEY);
        lines.Add("A)ttack foes");
      }

      if (c.MaxMP > 0)
      {
        _availableActions.Add(CAST_SPELL_KEY);
        lines.Add("C)ast spell");
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

  public void ActionChosen()
  {
    ++_slot;
    _availableActions = [];
    _pendingSpell = null;
    if (_slot >= app.GameState.Party.Alive)
      _state = CONFIRM;
    else
      _state = PICK_ACTION;
  }

  public void SpellChosen(SpellInfo spell)
  {
    switch (spell.Target)
    {
      case SpellTarget.Party:
        _pendingAction = BattleAction.CastSpell;
        _pendingSpell = spell;
        app.Screens.Push(new ChoosePartyMemberScreen(app, this, $"Cast {spell.Code} on whom") { Caption = _caption, Image = _image });
        break;
      case SpellTarget.Monsters:
        if (app.GameState.Party.Members[_slot] is Character c)
        {
          if (_battle.Foes.Count == 1)
          {
            _choices.Add(new(_slot, BattleAction.CastSpell, 0, -1, spell));
            ActionChosen();
          }
          else
          {
            _pendingAction = BattleAction.CastSpell;
            _pendingSpell = spell;
            app.Screens.Push(new ChooseMonsterGroupScreen(app, c, _battle.Foes, this) { Caption = _caption, Image = _image, AllGroups = true });          
          }          
        }
        
        break;
      default:
        _choices.Add(new(_slot, _pendingAction, -1, -1, spell));
        ActionChosen();
        break;
    }
  }

  public void MonsterGroupChosen(int monsterGroup)
  {
    _choices.Add(new(_slot, _pendingAction, monsterGroup, -1, _pendingSpell));
    ActionChosen();
  }

  public void CharacterChosen(int partySlot)
  {
    _choices.Add(new(_slot, _pendingAction, -1, partySlot, _pendingSpell));
    ActionChosen();
  }

  public void AbortSelection()
  {
    _state = PICK_ACTION;
    _pendingSpell = null;
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

    ui.DrawImage(image, monsterName, null);
    ui.DrawTextPanel(lines);
    ui.DrawRoster(_app.GameState.Party);
  }
}
