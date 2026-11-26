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

using SDL;

namespace BardsTaleKnockOff;

// The display and input side of a fight: collects each character's action,
// then shows the round's messages one at a time. The rules live in Battle.
sealed class BattleScreen : IScreen, ICastSpellScreen, IMonsterPickScreen, IPartyPickScreen, IInventorySelectionScreen
{
  // if the player flees some special encounters, like a guarded door, we
  // might want to move them to a different location
  public (int X, int Y, Dir Dir) RetreatLoc = (-1, -1, Dir.None); 
  public int ItemDrop { get; set; } = -1;

  // A message to be written after the player wins the fight
  public string VictoryMessage { get; set; } = "";
  // A fact to be set upon Victory
  public Fact? VictoryFact { get; set; } = null;
  // A function to call on victory
  public Action? VictoryHandler { get; set; } = null; 

  const SDL_Scancode ATTACK_KEY = SDL_Scancode.SDL_SCANCODE_A;
  const SDL_Scancode DEFEND_KEY = SDL_Scancode.SDL_SCANCODE_D;
  const SDL_Scancode HIDE_KEY = SDL_Scancode.SDL_SCANCODE_H;
  const SDL_Scancode CAST_SPELL_KEY = SDL_Scancode.SDL_SCANCODE_C;
  const SDL_Scancode USE_ITEM_KEY = SDL_Scancode.SDL_SCANCODE_U;
  const SDL_Scancode SMITE_KEY = SDL_Scancode.SDL_SCANCODE_S;
  const SDL_Scancode FIRE_BOW_KEY = SDL_Scancode.SDL_SCANCODE_F;

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
            _choices.Add(new(_slot, BattleAction.MeleeAttack, 0, PartySlot.None, null, -1));
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
          _choices.Add(new(_slot, BattleAction.Defend, 0, PartySlot.None, null, -1));
          break;
        case HIDE_KEY:
          next = true;
          _choices.Add(new(_slot, BattleAction.HideInShadows, 0, PartySlot.None, null, -1));
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
        case SMITE_KEY:
          SpellChosen(Spells.Smite);
          break;
        case FIRE_BOW_KEY:
          if (_app.GameState.Party.Members[_slot] is Character c2)
          {
            int ammo = -1;
            do
            {
              ++ammo;
              if (c2.Inventory[ammo] is InvSlot slot && ItemCatalog.Item(slot.Id) is Item item && item.Type == ItemType.Ammunition)
                break;
            }
            while (ammo < Character.INVENTORY_SLOTS);

            if (ammo > -1)
              SetupBowShot(c2, ammo);
          }
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
      _app.GameState.Facing = RetreatLoc.Dir;
    }

    Cleanup();
  }

  public void Cleanup()
  {
    if (_app.GameState.Party.Special is Monster m && m.Incapacitated)
      _app.GameState.Party.Special = null;
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

    // Scale up gold rewards in more dangerous areas
    if (_app.GameState.CurrRegion != "town")
    {
      double scale = 1.0 + 0.1 + _app.GameState.CurrLevel / 20.0;
      gold = (int)(gold * scale);
    }

    if (ItemDrop != -1)
    {      
      int xpPerChar = int.Max(xp / _app.GameState.Party.Alive, 1);

      for (int j = 0; j < Party.MAX_PARTY_SIZE; j++)
      {
        if (_app.GameState.Party.Members[j] is Character c && !c.Incapacitated)
        {
          c.XP += xpPerChar;
        }
      }

      _app.GameState.Say("You emerge victorious from battle!");
      _app.GameState.Say("");
      _app.GameState.Say($"Each survivor receives {xpPerChar} XP!");
      _app.GameState.Say("");

      string s = _app.GameState.Party.GiveItem(ItemDrop, _app.Rng);
      _app.GameState.Say(s);

      _image = "treasure.png";
      _caption = "Victory!";
    }
    else if (_app.GameState.CurrRegion == "town" || _app.Rng.Next(8) == 0)
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

    if (VictoryFact is not null)
      _app.GameState.FactDb[VictoryFact.Key] = VictoryFact;

    if (VictoryHandler is not null)
      VictoryHandler();
      
    // if this was a fixed encounter, we want to clear it out for this current
    // visit to the dungeon.
    Tile tile = _app.GameState.CurrTile;
    if (tile.Feature == Feature.Encounter)
    {
      _app.GameState.CurrMap.SetTile(_app.GameState.X, _app.GameState.Y, tile with { Feature = Feature.None });
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
          if (choice.InvIdx != -1 && c.Inventory[choice.InvIdx] is InvSlot invSlot)
          {          
            Item item = ItemCatalog.Item(invSlot.Id);
            lines.Add($"{c.Name} uses {item.Name.IndefArticle()}.");
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
            else if (choice.Spell.Target == SpellTarget.PartyMember && _app.GameState.Party.Slot(choice.TargetPartySlot) is IBeing target)
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

      if (c.PaladinSpellLevel > 0 && c.MP >= 3)
      {
        _availableActions.Add(SMITE_KEY);
        lines.Add("S) Smite");
      }
      else if (c.EquippedBow is Item bow && c.HasAmmunition())
      {
        _availableActions.Add(FIRE_BOW_KEY);
        lines.Add($"F) Shoot {bow.Name}");
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
        _app.Screens.Push(new ChoosePartyMemberScreen(_app, this, $"Cast {spell.Code} on whom", true) { Caption = _caption, Image = _image });
        break;
      case SpellTarget.Monsters:
        if (_app.GameState.Party.Members[_slot] is Character c)
        {
          if (_battle.Foes.Count == 1 && _battle.Foes[0].Distance <= spell.Range)
          {
            _choices.Add(new(_slot, BattleAction.CastSpell, 0, PartySlot.None, spell, -1));
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
        _choices.Add(new(_slot, _pendingAction, -1, PartySlot.None, spell, _chosenInvIdx));
        ActionChosen();
        break;
    }
  }

  public void MonsterGroupChosen(int monsterGroup)
  {
    _choices.Add(new(_slot, _pendingAction, monsterGroup, PartySlot.None, _pendingSpell, _chosenInvIdx));
    ActionChosen();
  }

  public void CharacterChosen(PartySlot partySlot)
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
      _choices.Add(new(_slot, BattleAction.ShootBow, 0, PartySlot.None, null, invIdx));
      ActionChosen();
      if (_app.Screens.Top is not BattleScreen)
        _app.Screens.Pop();
    }
    else
    {
      _pendingAction = BattleAction.ShootBow;

      ChooseMonsterGroupScreen chooseGroupScr = new(_app, c, 90, _battle.Foes, this)
      { 
        Caption = _caption, 
        Image = _image }
      ;
      if (_app.Screens.Top is BattleScreen)
        _app.Screens.Push(chooseGroupScr);
      else
        _app.Screens.Replace(chooseGroupScr);  
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
      _choices.Add(new(_slot, BattleAction.UseItem, -1, PartySlot.None, null, invIdx));
      ActionChosen();
    }
  }
}

sealed class EncounterScreen : IScreen
{
  public (int X, int Y, Dir Dir) RetreatLoc = (-1, -1, Dir.None); 

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

  public EncounterScreen(App app, List<MonsterGroup>? monsters = null)
  {
    _app = app;
    _app.GameState.Messages.Clear();

    if (monsters is null)
      PickMonsters();
    else
      _groups = monsters;
    
    // _groups = [ new MonsterGroup() { Monsters = [ 
    //     MonsterCatalog.Get(Monsters.AcidicSlime, _app.Rng), MonsterCatalog.Get(Monsters.AcidicSlime, _app.Rng),
    //     MonsterCatalog.Get(Monsters.AcidicSlime, _app.Rng), MonsterCatalog.Get(Monsters.AcidicSlime, _app.Rng),
    //     MonsterCatalog.Get(Monsters.AcidicSlime, _app.Rng), MonsterCatalog.Get(Monsters.AcidicSlime, _app.Rng),
    //     MonsterCatalog.Get(Monsters.AcidicSlime, _app.Rng), MonsterCatalog.Get(Monsters.AcidicSlime, _app.Rng),
    //     MonsterCatalog.Get(Monsters.AcidicSlime, _app.Rng), MonsterCatalog.Get(Monsters.AcidicSlime, _app.Rng),        
    //     MonsterCatalog.Get(Monsters.AcidicSlime, _app.Rng) ], Distance = 10 }];

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
      1 => $"The sudden scream of battle brings your party to a halt. You are confronted by {desc}!",
      2 => $"You curse as monsters charge you. You face {desc}!",
      _ => $"The denizens of this foul place assault you! You face {desc}!"
    };
  }

  void PickMonsters()
  {
    var gs = _app.GameState;
    List<(Monsters, int)> monsterOptions = [.. Util.MonsterTable[gs.CurrRegion][gs.CurrLevel]];
    if (gs.CurrRegion == "town" && !gs.DayTime)
      monsterOptions.AddRange((Monsters.Magician0, 4), (Monsters.Spider, 4));
    else if (_app.GameState.CurrTile.Feature == Feature.House)
      monsterOptions.Add((Monsters.Spider, 4));

    (Monsters monsterId, int count) = monsterOptions[_app.Rng.Next(monsterOptions.Count)];
    _groups.Add(GenGroup(monsterId, count, 10, _app.Rng));
    if (gs.CurrRegion == "town" && _app.Rng.Next(4) == 0) 
    {
      (Monsters monsterId2, int count2) = monsterOptions[_app.Rng.Next(monsterOptions.Count)];
      _groups.Add(GenGroup(monsterId2, count2, 10, _app.Rng));
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
        (Monsters mId, int cnt) = monsterOptions[_app.Rng.Next(monsterOptions.Count)];
        var group = GenGroup(mId, cnt, distance, _app.Rng);
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
  }

  static MonsterGroup GenGroup(Monsters monsterId, int num, int distance, Rng rng)
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
      if (RetreatLoc.X != -1 && RetreatLoc.Y != -1)
      {
        _app.GameState.X = RetreatLoc.X;
        _app.GameState.Y = RetreatLoc.Y;
        _app.GameState.Facing = RetreatLoc.Dir;
      }
      _app.Screens.Pop();
    }
    else if ((_state == PRELUDE && key.scancode == SDL_Scancode.SDL_SCANCODE_F) || _state == FAILED_TO_FLEE)
    {
      _app.Screens.Replace(new BattleScreen(_app, _groups, false) { RetreatLoc = RetreatLoc });
    }
    else if (_state == PRELUDE && _advance && key.scancode == SDL_Scancode.SDL_SCANCODE_A)
    {
      _app.Screens.Replace(new BattleScreen(_app, _groups, true) { RetreatLoc = RetreatLoc });
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
