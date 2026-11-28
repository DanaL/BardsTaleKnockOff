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

// Walking around the map. This is where the main adventuring loop lives:
// exploring now; random encounters (push a combat screen, pop it when the
// fight ends), shops and so on later.
sealed class AdventureScreen(App app) : IScreen, IPartyPickScreen, ICastSpellScreen
{
  GameState GS => app.GameState;

  const ulong GLIMPSE_DURATION = 300;
  ulong _glimpseMs = 0;

  Character? _caster = null;
  SpellInfo? _spellToCast = null;
  ulong _idleTime = 0;

  public void OnKey(SDL_KeyboardEvent key)
  {
    if (_glimpseMs > 0)
      CheckTileConditions();

    switch (key.scancode)
    {
      case SDL_Scancode.SDL_SCANCODE_UP:
      case SDL_Scancode.SDL_SCANCODE_W:
        GS.Messages.Clear();
        GS.TryMove(GS.Facing);
        CheckTileConditions();
        CheckLocation();
        ResolveTurn();
        Divination();
        CheckForSpinner();
        break;
      case SDL_Scancode.SDL_SCANCODE_DOWN:
      case SDL_Scancode.SDL_SCANCODE_S:
        GS.Facing = GS.Facing.Opposite();
        Divination();
        CheckForSpinner();
        break;
      case SDL_Scancode.SDL_SCANCODE_LEFT:
      case SDL_Scancode.SDL_SCANCODE_A:
        GS.Facing = GS.Facing.TurnLeft();
        Divination();
        CheckForSpinner();
        break;
      case SDL_Scancode.SDL_SCANCODE_RIGHT:
      case SDL_Scancode.SDL_SCANCODE_D:
        GS.Facing = GS.Facing.TurnRight();
        Divination();
        CheckForSpinner();
        break;
      case SDL_Scancode.SDL_SCANCODE_Q:
        if (!GS.Party.HasBuff(Buff.Levitation))
          GS.Say("You have no way to ascend.");
        else if (GS.CurrTile.Ceiling != Ceiling.Hole)
          GS.Say("There is nowhere to go!");
        else
        {
          --GS.CurrLevel;
          GS.Say("You rise upward to the floor above!");
          CheckLocation();
          ResolveTurn();
          Divination();
          CheckForSpinner();
        }
        break;        
      case SDL_Scancode.SDL_SCANCODE_Z:
        HandleDescending();
        break;
      case SDL_Scancode.SDL_SCANCODE_1:
      case SDL_Scancode.SDL_SCANCODE_2:
      case SDL_Scancode.SDL_SCANCODE_3:
      case SDL_Scancode.SDL_SCANCODE_4:
      case SDL_Scancode.SDL_SCANCODE_5:
      case SDL_Scancode.SDL_SCANCODE_6:
        var slot = (PartySlot) (key.scancode - SDL_Scancode.SDL_SCANCODE_1 + 1);
        if (GS.Party.Slot(slot) is Character)
          app.Screens.Push(new CharacterScreen(app, slot));
        break;
      case SDL_Scancode.SDL_SCANCODE_C:
        app.Screens.Push(new ChoosePartyMemberScreen(app, this, "Who will cast a spell", false));
        break;
      case SDL_Scancode.SDL_SCANCODE_N:
        app.Screens.Push(new ReorderPartyScreen(app, null, ""));
        break;
    }
  }

  public void HandleDescending()
  {        
    if (GS.CurrTile.Floor != Floor.Hole)
      GS.Say("You cannot descend here.");
    else if (GS.Party.HasBuff(Buff.Levitation))
    {
      GS.Say("You drift downward into the gloom.");
      Descend();
    }
    else
    {      
      app.Screens.Push(new ConfirmScreen(app, "Really leap into the hole?", JumpIntoHole));
    }

    void JumpIntoHole()
    {
      foreach (var c in GS.Party.Members.OfType<Character>())      
      {
        int dmg = app.Rng.Next(1, 7) + app.Rng.Next(1, 7);
        string s = c.ReceiveDmg(dmg);
        GS.Say($"{c.Name} takes {dmg} damage from the fall!");
        if (s != "")
          GS.Say(s);
      }
      Descend();
    }

    void Descend()
    {
      ++GS.CurrLevel;
      CheckLocation();
      ResolveTurn();
      Divination();
      CheckForSpinner();
    }
  }

  public void CheckLocation()
  {
    _idleTime = 0;

    switch (GS.CurrTile.Feature)
    {
      case Feature.Guild:
        while (!GS.DayTime)
          GS.Turn += GameState.TURNS_PER_HOUR;
        Serialize.WriteSaveGame(app);
        app.Screens.Replace(new GuildScreen(app));
        break;
      case Feature.Shop:
        app.Screens.Push(new ShoppeScreen(app));
        break;
      case Feature.House:
        app.Screens.Push(new HouseScreen(app));
        if (app.Rng.Next(2) == 0)
          SetUpEncounter();
        break;
      case Feature.Tavern:
        app.Screens.Push(new TavernScreen(app));
        break;
      case Feature.Temple:
        app.Screens.Push(new TempleScreen(app));
        break;
      case Feature.TrainingHall:
        if (app.GameState.DayTime)
        {
          app.Screens.Push(new TrainingHallScreen(app));
        }
        else
        {
          var (dx, dy) = app.GameState.Facing.Delta();
          app.GameState.X -= dx;
          app.GameState.Y -= dy;
          app.Screens.Push(new PopupScreen(app, "The Training Hall is closed until morning."));
        }
        break;
      case Feature.Burgemeester:
        app.Screens.Push(new BurgemeesterScreen(app));
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
          Monster ogre = MonsterCatalog.Get(Monsters.Ogre, app.Rng);
          ogre.HP = 35; // an especially tough ogre
          ogre.Rank += 1;
          app.Screens.Push(new BattleScreen(app, [new() { Monsters = [ogre], Distance = 10 }], false) { RetreatLoc = (19, 8, app.GameState.Facing.Opposite()), VictoryMessage = "A sinister aura emanates from the portal to the east." });
          app.Screens.Push(new PopupScreen(app, "An enormous ogre bars your path!"));
        }
        break;
      case Feature.Special3:
        if (!GS.FactDb.ContainsKey("TutorialChestFound"))
        {
          TreasureChestScreen scr = new(app, app.Rng.Next(300, 501), 1, 0)
          {
            OnLoot = () => { GS.FactDb.Add("TutorialChestFound", new FlagFact("TutorialChestFound", true)); }
          };
          app.Screens.Push(scr);
        }
        else if (IsEncounter())
        {
          SetUpEncounter();
        }
        break;
      case Feature.Special4:
        app.Screens.Push(new PortalScreen(app, "WizTowerEntrance"));
        break;
      case Feature.Special5:
        app.Screens.Push(new PortalScreen(app, "WizTowerExit"));
        break;      
      case Feature.Special6:
        app.Screens.Push(new HedgeWizardScreen(app));
        break;
      case Feature.Special7:
        app.Screens.Push(new PortalScreen(app, "DungeonEntrance"));
        break;
      case Feature.Special8:
        app.Screens.Push(new PortalScreen(app, "DungeonExit"));
        break;
      case Feature.StairsDown:
        app.Screens.Push(new StairsScreen(app, true));
        if (IsEncounter())
          SetUpEncounter();
        break;
      case Feature.StairsUp:
        app.Screens.Push(new StairsScreen(app, false));
        if (IsEncounter())
          SetUpEncounter();
        break;
      case Feature.Statue:
        string statueKey = $"Statue#{GS.X},{GS.Y},{GS.CurrLevel}";
        if (GS.FactDb.TryGetValue(statueKey, out var fact) && fact is StatueFact statueFact)
        {
          app.Screens.Push(new StatueScreen(app, statueFact));
        }
        if (IsEncounter())
          SetUpEncounter();
        break;
      case Feature.SpecialEncounter:
        if (!GS.FactDb.ContainsKey($"BattleWon#{GS.X},{GS.Y},{GS.CurrLevel}"))
        {
          string battleFactKey = $"ItemDropBattle#{GS.X},{GS.Y},{GS.CurrLevel}";
          if (GS.FactDb.TryGetValue(battleFactKey, out var fact2) && fact2 is ItemDropBattleFact battleFact)
          {
            string prompt = $"{battleFact.Desc} Confront them?";
            app.Screens.Push(new ConfirmScreen(app, prompt, () => { RunItemDropBattle(battleFact); }));
          }
        }
        break;
      case Feature.Teleporter:
        if (GS.CurrMap.Teleporters.TryGetValue((GS.X, GS.Y), out (int, int) dest))
        {
          (GS.X, GS.Y) = dest;
        }
        if (IsEncounter())
          SetUpEncounter();
        break;
      case Feature.Trap_Blades:
      case Feature.Trap_Dart:
      case Feature.Trap_PoisonedNeedles:
        Trap(GS.CurrTile.Feature);
        break;
      case Feature.Encounter:
        if (GS.CurrMap.Encounters.TryGetValue((GS.X, GS.Y), out var enc))
        {
          List<MonsterGroup> foes = [];
          foreach (var e in enc)
          {
            foes.Add(e.ToMonsterGroup(app.Rng));
          }

          var rl = GS.CurrMap.RetreatLocation(GS.X, GS.Y, app.Rng);
          EncounterScreen es = new(app, foes) { RetreatLoc = rl };
          app.Screens.Push(es);
        }
        break;
      case Feature.Lever:
        app.Screens.Push(new ConfirmScreen(app, "You find a lever! Pull it?", PullGateLever) { Image = "lever.png" });
        if (IsEncounter())
          SetUpEncounter();
        break;
      case Feature.MoatLever:
        if (GS.FactDb.TryGetValue($"MoatLever#{GS.X},{GS.Y},{GS.CurrLevel}", out var val) && val is MoatLeverFact moatLeverFact)
          app.Screens.Push(new ConfirmScreen(app, "You find a lever! Pull it?", () => { PullMoatLever(moatLeverFact); }) { Image = "lever.png" });
        if (IsEncounter())
          SetUpEncounter();
        break;
      case Feature.StairGuardian:
        if (!GS.FactDb.ContainsKey($"BattleWon#{GS.X},{GS.Y},{GS.CurrLevel}"))
        {
          string guardianKey = $"StairGuardian#{GS.X},{GS.Y},{GS.CurrLevel}";
          if (GS.FactDb.TryGetValue(guardianKey, out var gf) && gf is StairGuardianFact guardianFact)
          {
            string prompt = $"{guardianFact.Desc} Will you approach?";
            app.Screens.Push(new ConfirmScreen(app, prompt, () => { RunStairGuardianBattle(guardianFact); }));
          }
        }
        break;
      case Feature.Water:
        if (!GS.Party.HasBuff(Buff.Levitation))
        {
          FallIntoWater();
        }
        break;
      default:
        if (IsEncounter())
          SetUpEncounter();
        break;
    }

    if (GS.CurrTile.Ceiling == Ceiling.Hole)
      GS.Say("There is a hole in the ceiling above you, out of reach.");
    if (GS.CurrTile.Floor == Floor.Hole)
      GS.Say("There is a hole in the ground here, revealing darkness below.");

    if (GS.CurrMap.Messages.TryGetValue((GS.X, GS.Y), out var msg))
    {
      foreach (string s in msg.Split('\n'))
        GS.Say(s);
    }    
  }

  void SetUpEncounter()
  {
    var monsters = Encounters.Generate(GS, app.Rng);
    IScreen scr;
    if (monsters.Count == 1 && monsters[0].Count == 1 && app.Rng.Next(3) == 0)
      scr = new WanderingMonsterScreen(app, monsters[0].Monsters[0]);
    else
      scr = new EncounterScreen(app, monsters);
    app.Screens.Push(scr);
  }

  void FallIntoWater()
  {
    // Floodfill from the water tile to find the nearest 'safe' tile to get there
    Queue<(int X, int Y)> q = new();    
    q.Enqueue((GS.X, GS.Y));
    HashSet<(int, int)> visited = [];
    List<(int, int)> opts = [];

    while (q.Count > 0)
    {
      (int cx, int cy) = q.Dequeue();

      Tile curr = GS.CurrMap.TileAt(cx, cy);
      if (visited.Contains((cx, cy)))
        continue;
      visited.Add((cx, cy));
      
      if (curr.Feature != Feature.Water) 
      {
        opts.Add((cx, cy));
        continue;
      }
      
      foreach (Dir dir in Util.Cardinals)
      {
        var (deltaX, deltaY) = dir.Delta();
        (int X, int Y) adj = (DungeonLevel.Wrap(cx + deltaX), DungeonLevel.Wrap(cy + deltaY));
        if (!visited.Contains(adj) && GS.CurrMap.Passable(cx, cy, dir))
          q.Enqueue(adj);
      }
    }

    var (landingX, landingY) = opts[app.Rng.Next(opts.Count)];
    GS.Facing = Util.Cardinals[app.Rng.Next(Util.Cardinals.Length)];
    GS.X = landingX;
    GS.Y = landingY;

    foreach (Character c in GS.Party.Members.OfType<Character>())
    {
      c.HP = int.Max(1, c.HP / 2);
    }
    
    CheckLocation();
    
    app.Screens.Push(new PopupScreen(app, "You plunge into water, barely managing to drag yourselves to safety!"));
  }

  void RunItemDropBattle(ItemDropBattleFact battleFact)
  {
    List<Monster> monsters = [];
    for (int j = 0; j < battleFact.MonsterCount; j++)
      monsters.Add(MonsterCatalog.Get(battleFact.Monster, app.Rng));
   
    Item prize = ItemCatalog.Item(battleFact.ItemId);
    BattleScreen bs = new(app, [new() { Monsters = monsters, Distance = 10 }], false) 
    { 
      ItemDrop = battleFact.ItemId, 
      VictoryMessage = $"You find {prize.Name.IndefArticle()}",
      VictoryFact = new BattleWonFact(GS.X, GS.Y, GS.CurrLevel)
    };
    app.Screens.Push(bs);
  }

  void RunStairGuardianBattle(StairGuardianFact fact)
  {
    Monster m = MonsterCatalog.Get(fact.Monster, app.Rng);
    m.HP += fact.BonusHP;

     BattleScreen bs = new(app, [new() { Monsters = [ m ], Distance = 10 }], false) 
    { 
      VictoryFact = new BattleWonFact(GS.X, GS.Y, GS.CurrLevel),
      VictoryHandler = Victory
    };
    app.Screens.Push(bs);

    void Victory()
    {
      Tile tile = app.GameState.CurrMap.TileAt(app.GameState.X, app.GameState.Y);
      app.GameState.CurrMap.SetTile(app.GameState.X, app.GameState.Y, tile with { Feature = Feature.StairsDown });
      string savePath = Serialize.LevelPath(app.CampaignName, app.GameState.CurrLevel);
      MapDiskUtils.Save(app.GameState.CurrMap, savePath);

      CheckLocation();
    }
  }

  void PullMoatLever(MoatLeverFact leverFact)
  {
    bool bridgeRaised = false;
    foreach (var dir in Util.Cardinals)
    {
      var (deltaX, deltaY) = dir.Delta();
      int otherX = leverFact.StairsX + deltaX;
      int otherY = leverFact.StairsY + deltaY;
      Tile tile = GS.CurrMap.TileAt(otherX, otherY);
      if (tile.Feature == Feature.Water)
      {
        GS.CurrMap.SetTile(otherX, otherY, tile with { Feature = Feature.None, Floor = Floor.Stone });
        bridgeRaised = true;
      }
    }

    string s;
    if (bridgeRaised)
    {
      string savePath = Serialize.LevelPath(app.CampaignName, app.GameState.CurrLevel);
      MapDiskUtils.Save(app.GameState.CurrMap, savePath);  
      s = "The echoes of grinding machinery reverberate through the Underhalls.";
    }
    else
    {
      s = "Click.";
    }
    
    app.Screens.Push(new PopupScreen(app, s));
  }

  void PullGateLever()
  {
    var leverKey = $"Lever#{GS.X},{GS.Y},{GS.CurrLevel}";
    
    if (!GS.FactDb.ContainsKey(leverKey) || GS.FactDb[leverKey] is not LeverFact leverFact)
      return;

    int portcullisX = leverFact.PortcullisX;
    int portcullisY = leverFact.PortcullisY;
    var portcullisKey = $"PortcullisState#{portcullisX},{portcullisY},{GS.CurrLevel}";
    if (!GS.FactDb.ContainsKey(portcullisKey) || GS.FactDb[portcullisKey] is not PortcullisStateFact pFact)
      return;
    
    Tile portcullisTile = GS.CurrMap.TileAt(pFact.X, pFact.Y);
    Edge door = portcullisTile.EdgeForDir(pFact.Dir);
    Edge newDoor = door == Edge.Portcullis ? Edge.Open : Edge.Portcullis;
    Util.SetDoor(GS.CurrMap, new(pFact.X, pFact.Y, pFact.Dir), newDoor);
    app.Screens.Push(new PopupScreen(app, "The echoes of grinding machinery reverberate through the Underhalls."));
  }

  void Trap(Feature trapType)
  {
    // Levitation should project from traps most of the time
    if (GS.Party.Buffs.Any(b => b.Buff == Buff.Levitation) && app.Rng.Next(4) > 0)
      return;

    // Once I have implemented slot S, a monster occupying it will be
    // hit before the character in slot 1.
    switch (trapType)
    {
      case Feature.Trap_Dart:
        app.Screens.Push(new PopupScreen(app, "A trap!! Darts fly at the party!"));
        if (GS.Party.Members[0] is Character dc)
        {
          foreach (string s in Traps.Darts(dc, app.GameState, app.Rng))
            GS.Say(s);
        }
        break;
      case Feature.Trap_PoisonedNeedles:
        if (GS.Party.Members[0] is Character nc)
        {
          app.Screens.Push(new PopupScreen(app, "A trap!! Poisoned needles fly at the party!"));
          foreach (string s in Traps.PoisonNeedles(nc, app.GameState, app.Rng))
            GS.Say(s);
        }        
        break;
      case Feature.Trap_Blades:
        app.Screens.Push(new PopupScreen(app, "A trap!! Whirling blades slice through the party!"));
        foreach (string s in Traps.Blades(GS, app.Rng))
          GS.Say(s);
        break;        
    }    
  }

  void CheckTileConditions()
  {
    if (GS.CurrTile.HasFlag(TileFlags.Darkness) && GS.Party.HasLight())
    {
      GS.Say("Darkness!");
      GS.Party.ClearLightSources();
    }

    if (GS.CurrTile.HasFlag(TileFlags.Antimagic))
    {
      // I think in og BT, antimagic would snuff light from a torch but since
      // I have it as a separate buff, I'll allow it
      GS.Party.Buffs = [.. GS.Party.Buffs.Where(b => b.Buff is Buff.Torch)];
      foreach (var c in GS.Party.Members.OfType<Character>())
        c.ClearMagicalBuffs();
    }
  }

  void CheckForSpinner()
  {
    if (GS.CurrTile.Feature == Feature.Spinner)
    {
      List<Dir> other = [.. Util.Cardinals.Where(d => d != GS.Facing)];
      Dir spin = other[app.Rng.Next(other.Count)];      
      GS.Facing = spin;
    }
  }

  void ResolveTurn()
  {
    GS.Party.StatusEffects(GS);

    if (GS.Party.Alive == 0)
    {
      app.Screens.Replace(new TPKScreen(app));
      return;
    }

    // I feel like in OG Bard's Tale you didn't regenerate spell points when
    // moving, only when you were idle but I'm going to do it every 10 turns
    // in town (during daytime). Easier to code and track and makes more sense
    // to me?
    if (_idleTime > 10_000)
    {
      if (GS.CurrRegion == "town" && GS.DayTime)
      {
        foreach (var character in GS.Party.Members.OfType<Character>())
        {
          if (character.MaxMP > 0 && character.MP < character.MaxMP)
            ++character.MP;
        }
      }

      if (IsEncounter())
        SetUpEncounter();
    }

    _idleTime = 0;
    ++GS.Turn;

    GS.Party.CheckBuffs(GS.Turn);
    GS.Party.MoveIncapacitatedToBack();    
  }

  void Divination()
  {
    foreach (var buff in GS.Party.Buffs)
    {
      switch (buff.Buff)
      {
        case Buff.AreaEnchant:
          CheckAreaEnchant();
          break;
        case Buff.LocateTraps:
          LocateTraps();
          break;
      }
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
      if (tile.IsStairs())
      {
        GS.Say("There are stairs nearby!");
        break;
      }
    }
  }

  void LocateTraps()
  {
    var (dx, dy) = GS.Facing.Delta();
    int x = GS.X;
    int y = GS.Y;

    for (int j = 0; j < 3; j++)
    {
      x += dx;
      y += dy;
      Tile tile = GS.CurrMap.TileAt(x, y);
      if (tile.IsTrap())
      {
        GS.Say("There is a trap nearby!");
        break;
      }
    }
  }

  public void SpellChosen(SpellInfo spell)
  {
    if (_caster is null)
      return;

    GS.Say($"{_caster.Name} casts {spell.Name}.");
    if (_caster.MP < spell.Cost)
    {
      GS.Say($"But they have insufficient Magic Points!");
      return;
    }

    if (GS.CurrTile.HasFlag(TileFlags.Antimagic))
    {
      GS.Say($"The spell fizzles!");
      _caster.MP -= spell.Cost;
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
      app.Screens.Push(new ChoosePartyMemberScreen(app, this, $"Cast {spell.Name} on who", true));
    }
  }

  public void CharacterChosen(PartySlot partySlot)
  {
    if (_caster is not null && _spellToCast is not null)
    {
      // If _caster isn't null, we are picking a target of a spell
      SpellResolver sr = new([], new Battle(app.GameState, app.Rng, []));
      foreach (string s in sr.Cast(_spellToCast, _caster, app.Rng, -1, app.GameState.Party.Slot(partySlot), GS))
        GS.Say(s);
      _spellToCast = null;
      ResolveTurn();
    }
    else if (app.GameState.Party.Slot(partySlot) is Character c)
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
    int odds = 5;

    if (app.GameState.CurrRegion == "town" && app.GameState.DayTime)
    {
      odds = 8;
    }
    else if (app.GameState.CurrRegion == "town" && !app.GameState.DayTime)
    {
      odds = 6;
    }

    if (app.GameState.Party.Buffs.Any(b => b.Buff == Buff.SoftStep))
      odds *= 2;
    
    //return false;
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
    if (_glimpseMs > 0)
    {
      _glimpseMs = elapsedMs >= _glimpseMs ? 0 : _glimpseMs - elapsedMs;
      if (_glimpseMs == 0)
        CheckTileConditions();
    }
    else if (GS.Party.HasLight() && GS.CurrTile.HasFlag(TileFlags.Darkness))
    {
      _glimpseMs = GLIMPSE_DURATION;
    }

    _idleTime += elapsedMs;
    if (_idleTime >= 10_000)
    {
      ResolveTurn();
    }
  }
}

class HedgeWizardScreen : IScreen
{
  readonly App _app;
  readonly string _blurb;
  readonly HashSet<int> _students = [];

  int _state = MAIN;
  int _student;
  bool _insufficientFunds = true;
  const int MAIN = 0;
  const int TEACHING = 1;
  const int LESSON_OVER = 2;

  const int PRICE = 500;

  public HedgeWizardScreen(App app)
  {
    _app = app;
    _blurb = Blurb();
  }

  string Blurb()
  {
    return _app.Rng.Next(4) switch
    {
      0 => "Did my guards harass you at all? We've had troubles with break-ins lately so they can be a smidge aggressive.",
      1 => "Since those cultists have come to Sarnath, we've been simply thick with monsters. Sometimes you have to fight fire with fire, as they say.",
      2 => "Did Karnov send you? Is this about my bar tab?",
      _ => "Greetings adventurers! I hope none of my minions gave you any trouble. It's tough finding good help on salaries I'm willing to pay!"
    };
  }

  public void OnKey(SDL_KeyboardEvent key)
  {
    if (_state == MAIN)
    {
      switch (key.scancode)
      {
        case SDL_Scancode.SDL_SCANCODE_1:
        case SDL_Scancode.SDL_SCANCODE_2:
        case SDL_Scancode.SDL_SCANCODE_3:
        case SDL_Scancode.SDL_SCANCODE_4:
        case SDL_Scancode.SDL_SCANCODE_5:
        case SDL_Scancode.SDL_SCANCODE_6:
          int slot = key.scancode - SDL_Scancode.SDL_SCANCODE_1;
          if (_students.Contains(slot))
          {
            _student = slot;
            _state = TEACHING;
          }
          break;
        case SDL_Scancode.SDL_SCANCODE_ESCAPE:
        case SDL_Scancode.SDL_SCANCODE_E:
          _app.GameState.X -= 1;
          _app.GameState.Facing = Dir.West;
          _app.Screens.Pop();
          break;
      }
    }
    else if (_state == TEACHING)
    {
      switch (key.scancode)
      {
        case SDL_Scancode.SDL_SCANCODE_ESCAPE:
          _app.GameState.X -= 1;
          _app.GameState.Facing = Dir.West;
          _app.Screens.Pop();
          break;
        case SDL_Scancode.SDL_SCANCODE_L:
          if (!_insufficientFunds)
          {
            TeachEscape();
          }
          break;
        default:
          _state = MAIN;
          break;
      }
    }
    else if (_state == LESSON_OVER)
    {
      _state = MAIN;
    }
  }

  void TeachEscape()
  {
    if (_app.GameState.Party.Members[_student] is not Character c)
      return;

    _state = LESSON_OVER;
    c.Buffs.Add(new TempBuff(Buff.EbenezumEscape, ulong.MaxValue, false));
    c.Gold -= PRICE;
  }

  public void Draw(GameUI ui)
  {    
    List<string> lines = _state switch
    {
      TEACHING => SetupTeaching(),
      LESSON_OVER => LessonOver(),
      _ => SetupMainMenu()
    };
    
    ui.DrawImage("magicuser_blue.png", "Ebenezum");
    ui.DrawTextPanel(lines);
    ui.DrawRoster(_app.GameState);
  }

  List<string> LessonOver()
  {
    if (_app.GameState.Party.Members[_student] is not Character c)
      return [];

    return [ $"Under Ebenezum's tutelage, {c.Name} learns the arcane recipe for Escape!", "", " - press any key to continue - " ];
  }

  List<string> SetupTeaching()
  {
    // At this point, Members[_student] should/must be valid
    if (_app.GameState.Party.Members[_student] is not Character c)
      return [];

    List<string> lines = [ "I've always said that bravery is simply a lack of imagination for the myriad ways to resolve a dangerous situation." , "",
      $"And thus, I developed a little cantrip that immediately whisks you out of a kerfuffle you no longer desire to partake in. Its formula can be yours for a paltry {PRICE} gp!", "" ];

    if (c.Gold < PRICE)
    {
      lines.AddRange($"Hmm...{c.Name} seems a bit short on coin. Mayhaps return when they are a little more flush.", "", " - press any key to continue - ");
      _insufficientFunds = true;
    }
    else
    {
      lines.AddRange("L) Learn Ebenezum's Escape");
      _insufficientFunds = false;
    }

    return lines;
  }

  List<string> SetupMainMenu()
  {
    List<string> lines = [ _blurb ];
    List<(int, Character)> canLearn = [];
    bool tooLowLevel = false;

    for (int j = 0; j < Party.MAX_PARTY_SIZE; j++)
    {
      if (_app.GameState.Party.Members[j] is not Character c)
        continue;
      bool knowsEscape = c.Buffs.Any(b => b.Buff == Buff.EbenezumEscape);
      switch (c.Role)
      {
        case Role.Conjurer:
          if (c.ConjurerSpellLevel >= 2 && !knowsEscape)
            canLearn.Add((j, c));
          else if (c.ConjurerSpellLevel < 2)
            tooLowLevel = true;
          break;
        case Role.Magician:
          if (c.MagicianSpellLevel >= 2 && !knowsEscape)
            canLearn.Add((j, c));
          else if (c.MagicianSpellLevel < 2)
            tooLowLevel = true;
          break;
        case Role.Sorcerer:
          if (c.SorcererSpellLevel >= 2 && !knowsEscape)
            canLearn.Add((j, c));
          else if (c.SorcererSpellLevel < 2)
            tooLowLevel = true;
          break;
        case Role.Wizard:
          if (c.WizardSpellLevel >= 2 && !knowsEscape)
            canLearn.Add((j, c));
          else if (c.WizardSpellLevel < 2)
            tooLowLevel = true;
          break;
      }
    }

    if (canLearn.Count > 0)
    {
      lines.AddRange("", "I can teach a little magic for a small sum of gold.");
      foreach (var (slot, c) in canLearn)
      {
        lines.Add($"{slot + 1}) Teach a spell to {c.Name}");
        _students.Add(slot);
      }
    }
    else if (tooLowLevel)
    {
      lines.AddRange("", "Come back when you have a little more field experience!");
    }
    else
    {
      lines.AddRange("", "Hmm well, I've taught all the magic secrets I'm willing to part with.");
    }

    lines.AddRange("", "E) Exit");

    return lines;
  }
}