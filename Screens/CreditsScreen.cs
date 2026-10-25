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

// Pushed on top of the main menu; any key pops back to it.
class CreditsScreen(App app) : IScreen
{
  public void OnKey(SDL_KeyboardEvent key) => app.Screens.Pop();

  public void Draw(GameUI ui)
  {
    ui.DrawPicture("Credits");
    // TODO: check the art credit against the licence file in the art pack.
    ui.DrawTextPanel([
      "Programming: Dana",
      "",
      "Dungeon and outdoor art: Clint Bellanger (Heroine Dusk), CC-BY 3.0",
      "",
      "m5x7 font: Daniel Linssen",
      "",
      "Inspired by The Bard's Tale (Interplay, 1985)",
      "",
      "",
      "Press any key."
    ]);
    ui.DrawRoster(null);
  }
}
