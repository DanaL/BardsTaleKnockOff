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

class HealingScreen(App app, Character c, int slot) : IScreen
{
  const int MAIN = 0;
  const int BROKE = 1;
  const int HEALED = 2;

  int _state = MAIN;
  
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
          int nextSlot = key.scancode - SDL_Scancode.SDL_SCANCODE_1;
          if (nextSlot <= app.GameState!.Party.Count)
          {
            Character? c = app.GameState!.Party.Members[nextSlot];
            if (c is not null)
              app.Screens.Replace(new HealingScreen(app, c, nextSlot));
          }          
          break;
        case SDL_Scancode.SDL_SCANCODE_P:
          app.GameState.Party.PoolGold(slot);
          break;
        case SDL_Scancode.SDL_SCANCODE_H:
          HealCharacter();
          break;
        case SDL_Scancode.SDL_SCANCODE_ESCAPE:
          app.Screens.Pop();
          break;
      }      
    }
    else if (_state == BROKE && key.scancode == SDL_Scancode.SDL_SCANCODE_SPACE)
    {
      _state = MAIN;
    }
    else if (_state == HEALED && key.scancode == SDL_Scancode.SDL_SCANCODE_SPACE)
    {
      _state = MAIN;
    }
  }
  
  public void Draw(GameUI ui)
  {
    List<string> lines = [];

    if (_state == MAIN)
    {
      // Eventually we'll need to check for status effects like poisoned
      // or dead
      if (c.HP < c.MaxHP)
      {
        lines.Add($"{c.Name} is indeed in need of healing! They can be made well for the small donation of {Invoice()} gold pieces.");
        lines.AddRange("", $"{c.Name} has {c.Gold} gp.", "", $"H)eal {c.Name}");
      }
      else
      {
        lines.AddRange($"{c.Name} appears healthy and hale!", "");
        lines.AddRange($"{c.Name} has {c.Gold} gp.", "");
      }

      lines.Add("P)ool gold");
    }
    else if (_state == BROKE)
    {
      lines.AddRange($"Unfortunately, {c.Name} appears to be unable to make the required tithe.", "", "- space to continue -", "");
    }
    else if (_state == HEALED)
    {
      lines.AddRange($"{c.Name} is healed!", "", "- space to continue -", "");
    }

    ui.DrawPicture("Temple");
    ui.DrawTextPanel(lines);
    ui.DrawRoster(app.GameState.Party);
  }

  void HealCharacter()
  {
    if (Invoice() > c.Gold)
    {
      _state = BROKE;
    }
    else
    {
      c.Gold -= Invoice();
      c.HP = c.MaxHP;

      // And eventually fix other statuses
      // Note in og Bard's Tale, you were brought back to life at 1 HP

      _state = HEALED;
    }
  }

  int Invoice()
  {
    int invoice = (c.MaxHP - c.HP) * 10;

    // Eventually poisoning, stoned, dead will add to the cheque

    return invoice;
  }
}

class TempleScreen(App app) : IScreen
{
  const int MAIN_SCREEN = 0;

  int _state = MAIN_SCREEN;

  public void OnKey(SDL_KeyboardEvent key)
  {
    if (_state == MAIN_SCREEN)
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
          if (slot <= app.GameState!.Party.Count)
          {
            Character? c = app.GameState!.Party.Members[slot];
            if (c is not null)
              app.Screens.Push(new HealingScreen(app, c, slot));
          }          
          break;
        case SDL_Scancode.SDL_SCANCODE_ESCAPE:
        case SDL_Scancode.SDL_SCANCODE_E:
          app.GameState.Facing = app.GameState.Facing.Opposite();
          var (dx, dy) = app.GameState.Facing.Delta();
          app.GameState.X += dx;
          app.GameState.Y += dy;
          app.Screens.Pop();
          break;
      }
    }
  }

  public void Draw(GameUI ui)
  {
    List<string> lines = [];

    if (_state == MAIN_SCREEN)
    {
      lines.AddRange("Greetings Seekers, and welcome to Temple of St. Bunten.", "", "Which of you has need for healing?", "", "E) Exit");
    }
    
    ui.DrawPicture("Temple");
    ui.DrawTextPanel(lines);
    ui.DrawRoster(app.GameState.Party);
  }
}