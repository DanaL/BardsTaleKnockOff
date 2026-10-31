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
  GameState GS => app.GameState!;

  const int EXPLORE = 0;
  const int CAST_SPELL = 1;

  Character? _caster = null;

  public void OnKey(SDL_KeyboardEvent key)
  {
    switch (key.scancode)
    {
      case SDL_Scancode.SDL_SCANCODE_UP:
      case SDL_Scancode.SDL_SCANCODE_W:
        GS.TryMove(GS.Facing);
        ResolveMove();
        break;
      case SDL_Scancode.SDL_SCANCODE_DOWN:
      case SDL_Scancode.SDL_SCANCODE_S:
        ++GS.Turn;
        GS.Facing = GS.Facing.Opposite();
        break;
      case SDL_Scancode.SDL_SCANCODE_LEFT:
      case SDL_Scancode.SDL_SCANCODE_A:
        ++GS.Turn;
        GS.Facing = GS.Facing.TurnLeft();
        break;
      case SDL_Scancode.SDL_SCANCODE_RIGHT:
      case SDL_Scancode.SDL_SCANCODE_D:
        ++GS.Turn;
        GS.Facing = GS.Facing.TurnRight();
        break;
      case SDL_Scancode.SDL_SCANCODE_L:
        ++GS.Turn;
        GS.Say($"Loc: {GS.X}, {GS.Y}");
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
      case SDL_Scancode.SDL_SCANCODE_ESCAPE:
        app.Screens.Push(new ConfirmScreen(app, "Abandon this adventure and return to the main menu?", () =>
        {
          app.Screens.Replace(new MainMenuScreen(app));
        }));
        break;
    }
  }

  void ResolveMove()
  {
    ++GS.Turn;
    Tile tile = GS.CurrMap.TileAt(GS.X, GS.Y);
    if (tile.Feature == Feature.Guild)
    {
      Serialize.WriteSaveGame(app);
      app.Screens.Replace(new GuildScreen(app));
    }
    else if (tile.Feature == Feature.Shop)
    {
      app.Screens.Push(new ShopScreen(app));
    }
    else if (tile.Feature == Feature.House)
    {
      app.Screens.Push(new HouseScreen(app));
    }
    else if (tile.Feature == Feature.Temple)
    {
      app.Screens.Push(new TempleScreen(app));
    }
    else if (IsEncounter())
    {
      app.Screens.Push(new EncounterScreen(app));
    }
  }

  public void SpellChosen(SpellInfo spell)
  {
    SpellResolver sr = new([], new Battle(app.GameState, app.Rng, []));
    if (spell.Target == SpellTarget.None && _caster is not null)
    {
      app.GameState.Say($"{_caster.Name} casts {spell.Code}.");
      if (_caster.MP < spell.Cost) 
      {
        app.GameState.Say($"But they have insufficient Magic Points!");
      }
      else
      {
        sr.Cast(spell, _caster, app.Rng, -1, null, app.GameState);
      }
    }
  }

  public void CharacterChosen(int partySlot)
  {
    if (app.GameState.Party.Members[partySlot] is Character c)
    {
      if (c.MaxMP == 0)
        app.GameState.Say($"{c.Name} cannot work magic!");
      else
      {
        _caster = c;
        app.Screens.Push(new ChooseSpellScreen(app, c, false, this));
      }
    }
  }

  public void AbortSelection() => _caster = null;

  bool IsEncounter()
  {
    if (app.GameState.CurrLevel == 0 && app.GameState.DayTime)
    {
      return app.Rng.Next(6) == 0;
    }
    else if (app.GameState.CurrLevel == 0 && !app.GameState.DayTime)
    {
      return app.Rng.Next(4) == 0;
    }

    return false;
  }

  public void Draw(GameUI ui)
  {
    ui.DrawDungeonView(GS);
    ui.DrawTextPanel(GS.Messages);
    ui.DrawRoster(GS.Party);
  }
}
