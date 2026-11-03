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

class TrainingHallServicesScreen(App app, Character c) : IScreen
{
  const int MAIN = 0;
  const int MEDITATION = 1;

  const char MEDITATION_OPT = 'm';
  const char LEVEL_UP_OPT = 'l';

  const char YES_OPT = 'y';
  const char NO_OPT = 'n';

  int _state = MAIN;
   
  HashSet<char> _options = [];

  public void OnKey(SDL_KeyboardEvent key)
  {
    if (_state == MAIN)
    {
      switch (key.scancode)
      {
        case SDL_Scancode.SDL_SCANCODE_M:
          if (_options.Contains(MEDITATION_OPT))
            _state = MEDITATION;
          break;
        case SDL_Scancode.SDL_SCANCODE_L:
          if (_options.Contains(LEVEL_UP_OPT)) 
          {
            c.LevelUp(app.Rng);
            app.Screens.Push(new PopupScreen(app, $"{c.Name} is now level {c.Level}!"));
          }
          break;
        case SDL_Scancode.SDL_SCANCODE_ESCAPE:
          app.Screens.Pop();
          break;
      }  
    }
    else if (_state == MEDITATION)
    {
      switch (key.scancode)
      {
        case SDL_Scancode.SDL_SCANCODE_N:
          if (_options.Contains(NO_OPT))
          {
            _options.Clear();
            _state = MAIN;  
          }
          break;
        case SDL_Scancode.SDL_SCANCODE_ESCAPE:
          _options.Clear();
          _state = MAIN;
          break;
      }
    }
  }

  public void Draw(GameUI ui)
  {
    List<string> lines = [ $"What can we do for {c.Name} today?", "" ];

    if (_state == MAIN)
    {
      int xpNeeded = XPTable.Needed(c.Role, c.Level + 1);
      if (c.XP < xpNeeded)
        lines.AddRange($"They require {xpNeeded - c.XP} more XP to reach the next level.", "");
      else 
      {
        lines.AddRange($"They are ready to advance to level {c.Level + 1}!", "", "L) Level up");
        _options.Add(LEVEL_UP_OPT);
      }

      if (c.MP < c.MaxMP)
      {
        lines.Add("M) Relax in the meditation hall.");
        _options.Add(MEDITATION_OPT);
      }
    }
    else if (_state == MEDITATION)
    {
      int fee = 15 * (c.MaxMP - c.MP);
      lines.Add($"Some contemplation and breath work will indeed refocus {c.Name}'s mind and restore their MP.");
      if (fee > c.Gold)
      {
        lines.AddRange($"Unfortunately, they lack the {fee} gp we require to book a session.");
      }
      else
      {
        lines.AddRange("", $"Renting time in the meditation hall will cost {fee} gp.", "", "Shall they pay? (y/n)");
        _options.Add(YES_OPT);
        _options.Add(NO_OPT);
      }
    }

    ui.DrawNoBackground("Training Hall");
    ui.DrawTextPanel(lines);
    ui.DrawRoster(app.GameState.Party);
  }
}

class TrainingHallScreen(App app) : IScreen
{
  const int MAIN = 0;

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
          int slot = key.scancode - SDL_Scancode.SDL_SCANCODE_1;
          if (slot <= app.GameState.Party.Count)
          {
            Character? c = app.GameState!.Party.Members[slot];
            if (c is not null)
              app.Screens.Push(new TrainingHallServicesScreen(app, c));
          }          
          break;
        case SDL_Scancode.SDL_SCANCODE_ESCAPE:
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
    List<string> lines = [ "Welcome to the Training Hall!", ""];

    if (_state == MAIN)
    {
      lines.Add("Who among you wishes to avail themselves of our services?");
    }

    ui.DrawNoBackground("Training Hall");
    ui.DrawTextPanel(lines);
    ui.DrawRoster(app.GameState.Party);
  }
}