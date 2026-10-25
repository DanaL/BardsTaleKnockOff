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

// One "page" of the game: the main menu, the Guild, exploring the town,
// combat... The main loop sends input to whichever screen is on top of the
// ScreenStack and asks the stack to draw.
//
// Keyboard input arrives two ways (see the notes in Program.cs):
//   OnKey  - a key was pressed. Use key.scancode for movement keys (physical
//            position, so WASD works on any layout) and key.key for menu
//            letters (what's printed on the key, so B means B everywhere).
//   OnText - characters were typed. Only arrives while text input is
//            switched on (App.StartTextInput), e.g. when entering a name.
interface IScreen
{
  // Called when the screen is put on the stack / taken off it.
  void OnEnter() { }
  void OnExit() { }

  void OnKey(SDL_KeyboardEvent key);
  void OnText(string text) { }

  void Draw(GameUI ui);

  // An overlay (e.g. a yes/no question) is drawn on top of the screen
  // beneath it rather than replacing it.
  bool IsOverlay => false;
}
