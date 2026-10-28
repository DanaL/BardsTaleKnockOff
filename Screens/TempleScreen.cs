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

class TempleScreen(App app) : IScreen
{
  const int MAIN_SCREEN = 0;

  int _slot = 0;
  int _state = MAIN_SCREEN;

  public void OnKey(SDL_KeyboardEvent key)
  {
    if (_state == MAIN_SCREEN)
    {
      switch (key.scancode)
      {
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
    ui.DrawRoster(app.GameState!.Party);
  }
}