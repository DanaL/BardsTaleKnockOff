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

// Walking around the map. This is where the main adventuring loop lives:
// exploring now; random encounters (push a combat screen, pop it when the
// fight ends), shops and so on later.
class AdventureScreen(App app) : IScreen, IPartyPickScreen, ICastSpellScreen
{
  GameState GS => app.GameState;

  const int EXPLORE = 0;
  const int CAST_SPELL = 1;
  
  Character? _caster = null;
  SpellInfo? _spellToCast = null;
  ulong _idleTime = 0;
  
  public void OnKey(SDL_KeyboardEvent key)
  {
    switch (key.scancode)
    {
      case SDL_Scancode.SDL_SCANCODE_UP:
      case SDL_Scancode.SDL_SCANCODE_W:
        GS.Messages.Clear();
        GS.TryMove(GS.Facing);
        CheckLocation();
        ResolveTurn();
        Divination();
        break;
      case SDL_Scancode.SDL_SCANCODE_DOWN:
      case SDL_Scancode.SDL_SCANCODE_S:        
        GS.Facing = GS.Facing.Opposite();
        Divination();
        break;
      case SDL_Scancode.SDL_SCANCODE_LEFT:
      case SDL_Scancode.SDL_SCANCODE_A:
        GS.Facing = GS.Facing.TurnLeft();
        Divination();
        break;
      case SDL_Scancode.SDL_SCANCODE_RIGHT:
      case SDL_Scancode.SDL_SCANCODE_D:
        GS.Facing = GS.Facing.TurnRight();
        Divination();
        break;
      case SDL_Scancode.SDL_SCANCODE_1:
      case SDL_Scancode.SDL_SCANCODE_2:
      case SDL_Scancode.SDL_SCANCODE_3:
      case SDL_Scancode.SDL_SCANCODE_4:
      case SDL_Scancode.SDL_SCANCODE_5:
      case SDL_Scancode.SDL_SCANCODE_6:
        int slot = key.scancode - SDL_Scancode.SDL_SCANCODE_1;
        if (slot <= app.Roster.Count)
          app.Screens.Push(new CharacterScreen(app, slot));
        break;
      case SDL_Scancode.SDL_SCANCODE_C:
        app.Screens.Push(new ChoosePartyMemberScreen(app, this, "Who will cast a spell"));
        break;
      case SDL_Scancode.SDL_SCANCODE_N:
        app.Screens.Push(new ReorderPartyScreen(app));
        break;
    }
  }

  void CheckLocation()
  {
    _idleTime = 0;

    switch (GS.CurrTile.Feature)
    {
      case Feature.Guild:
        Serialize.WriteSaveGame(app);
        app.Screens.Replace(new GuildScreen(app));
        break;
      case Feature.Shop:
        app.Screens.Push(new ShoppeScreen(app));
        break;
      case Feature.House:
        app.Screens.Push(new HouseScreen(app));
        if (app.Rng.Next(2) == 0)
          app.Screens.Push(new EncounterScreen(app));
        break;
      case Feature.Tavern:
        app.Screens.Push(new TavernScreen(app));
        break;
      case Feature.Temple:
        app.Screens.Push(new TempleScreen(app));
        break;
      case Feature.TrainingHall:
        app.Screens.Push(new TrainingHallScreen(app));
        break;
      case Feature.Special1:
        if (!GS.FactDb.ContainsKey("TownOgreSlain"))
        {
          app.Screens.Push(new PopupScreen(app, "To the east is a yawning portal, beyond which adventure beckons. An ogre lumbers back and forth, blocking your access."));
        }
        break;
      case Feature.Special2:        
        if (!GS.FactDb.ContainsKey("TownOgreSlain"))
        {
          Monster ogre = MonsterCatalog.Get(MonsterCatalog.OGRE, app.Rng);
          ogre.DefeatFact = new FlagFact("TownOgreSlain", true);
          ogre.HP = 35; // an especially tough ogre
          ogre.Rank += 1;          
          app.Screens.Push(new BattleScreen(app, [ new() { Monsters = [ ogre ], Distance = 10}], false) { RetreatLoc = (19, 8), VictoryMessage = "A sinister aura emanates from the portal to the east." });          
          app.Screens.Push(new PopupScreen(app, "An enormous ogre bars your path!"));
        }        
        break;
      case Feature.Special3:
        if (!GS.FactDb.ContainsKey("TutorialChestFound"))
        {
          TreasureChestScreen scr = new(app, app.Rng.Next(300, 501), 1)
          {
            OnLoot = () => { GS.FactDb.Add("TutorialChestFound", new FlagFact("TutorialChestFound", true)); }
          };
          app.Screens.Push(scr);
        }
        else if (IsEncounter())
        {
          app.Screens.Push(new EncounterScreen(app));
        }
        break;
      case Feature.Special4:
        app.Screens.Push(new PortalScreen(app, "WizTowerEntrance"));
        break;
      case Feature.Special5:
        app.Screens.Push(new PortalScreen(app, "WizTowerExit"));
        break;
      case Feature.Trap_Dart0:
        DartTrap0();
        break;
      default:
        if (IsEncounter())
          app.Screens.Push(new EncounterScreen(app));
        break;
    }

    if (app.GameState.CurrMap.Messages.TryGetValue((app.GameState.X, app.GameState.Y), out var msg))
    {
      foreach (string s in msg.Split('\n'))
        app.GameState.Say(s);
    }
  }

  void DartTrap0()
  {
    app.Screens.Push(new PopupScreen(app, "A trap!!"));
    
    if (GS.Party.Members[0] is Character c)
    {
      // Once I have implemented slot S, a monster occupying it will be
      // hit before the character in slot 1.
      foreach (string s in Traps.Darts0(c, GS, app.Rng))
        GS.Say(s);
    }    
  }

  void ResolveTurn()
  {
    // I feel like in OG Bard's Tale you didn't regenerate spell points when
    // moving, only when you were idle but I'm going to do it every 10 turns
    // in town (during daytime). Easier to code and track and makes more sense
    // to me?
    if (_idleTime > 10_000)
    {
      if (app.GameState.CurrLevel == 0 && app.GameState.DayTime && app.GameState.CurrLevel == 0)
      {
        foreach (var character in app.GameState.Party.Members.OfType<Character>())
        {
          if (character.MaxMP > 0 && character.MP < character.MaxMP)
            ++character.MP;
        }
      }

      if (IsEncounter())
        app.Screens.Push(new EncounterScreen(app));
    }

    _idleTime = 0;
    ++GS.Turn;
    GS.Party.Buffs = [.. GS.Party.Buffs.Where(b => b.ExpiresOn > GS.Turn)];
    GS.Party.MoveIncapacitatedToBack();
  }

  void Divination()
  {
    foreach (var buff in GS.Party.Buffs)
    {
      if (buff.Buff == Buff.AreaEnchant)
        CheckAreaEnchant();
    }
  }

  void CheckAreaEnchant()
  {
    var (dx, dy) = GS.Facing.Delta();
    int x = GS.X;
    int y = GS.Y;

    for (int j = 0; j < 3; j++)
    {
      x += dx;
      y += dy;
      Tile tile = GS.CurrMap.TileAt(x, y);
      if (tile.Feature == Feature.Special5 || tile.Feature == Feature.StairsUp || tile.Feature == Feature.StairsDown)
      {
        GS.Say("There are stairs nearby!");
        break;
      }
    }
  }

  public void SpellChosen(SpellInfo spell)
  {
    if (_caster is null)
      return;

    app.GameState.Say($"{_caster.Name} casts {spell.Name}.");
    if (_caster.MP < spell.Cost) 
    {
      app.GameState.Say($"But they have insufficient Magic Points!");
      return;
    }

    if (spell.Target == SpellTarget.None || spell.Target == SpellTarget.FullParty || spell.Target == SpellTarget.Self)
    {      
      SpellResolver sr = new([], new Battle(app.GameState, app.Rng, []));
      foreach (string s in sr.Cast(spell, _caster, app.Rng, -1, null, app.GameState))
        GS.Say(s);

      ResolveTurn();      
    }
    else if (spell.Target == SpellTarget.PartyMember)
    {
      _spellToCast = spell;
      app.Screens.Push(new ChoosePartyMemberScreen(app, this, $"Cast {spell.Name} on who"));
    }
  }

  public void CharacterChosen(int partySlot)
  {
    if (_caster is not null && _spellToCast is not null)
    {
      // If _caster isn't null, we are picking a target of a spell
      SpellResolver sr = new([], new Battle(app.GameState, app.Rng, []));
      foreach (string s in sr.Cast(_spellToCast, _caster, app.Rng, -1, app.GameState.Party.Members[partySlot], GS))
        GS.Say(s);
      _spellToCast = null;
      ResolveTurn();
    }
    else if (app.GameState.Party.Members[partySlot] is Character c)
    {
      // We are choosing who to cast a spell here
      if (c.MaxMP == 0)
        app.GameState.Say($"{c.Name} cannot work magic!");
      else
      {
        _caster = c;
        app.Screens.Push(new ChooseSpellScreen(app, c, SpellContext.Exploring, this));
      }
    }
  }

  public void AbortSelection() => _caster = null;

  bool IsEncounter()
  {
    int odds = 4;

    if (app.GameState.CurrLevel == 0 && app.GameState.DayTime)
    {
      odds = 7;
    }
    else if (app.GameState.CurrLevel == 0 && !app.GameState.DayTime)
    {
      odds = 5;
    }

    if (app.GameState.Party.Buffs.Any(b => b.Buff == Buff.SoftStep))
      odds *= 2;

    return app.Rng.Next(odds) == 0;
  }
  
  public void Draw(GameUI ui)
  {
    ui.DrawDungeonView(GS);
    ui.DrawTextPanel(GS.Messages);
    ui.DrawRoster(GS);
  }

  public void Update(ulong elapsedMs)
  {
    _idleTime += elapsedMs;
    if (_idleTime >= 10_000) 
    {    
      ResolveTurn();
    }
  }
}
