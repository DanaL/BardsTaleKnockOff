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

namespace BardsTaleKnockOff;

// The screens currently open, newest on top. Only the top one gets input.
//
//   Push    - open a screen on top (credits, combat, a question). Popping it
//             returns to exactly where you were.
//   Pop     - close the top screen.
//   Replace - move on to a different screen (main menu -> Guild -> town),
//             with no way "back" to the old one.
//   Clear   - close everything, which ends the program.
//
// It's fine to call these from inside a screen's OnKey: the change takes
// effect immediately and the next event goes to the new top screen.
class ScreenStack
{
  readonly List<IScreen> _screens = [];

  public IScreen? Top => _screens.Count > 0 ? _screens[^1] : null;
  public bool IsEmpty => _screens.Count == 0;

  public void Push(IScreen screen)
  {
    _screens.Add(screen);
    screen.OnEnter();
  }

  public void Pop()
  {
    if (_screens.Count == 0)
      return;
    var top = _screens[^1];
    _screens.RemoveAt(_screens.Count - 1);
    top.OnExit();
  }

  public void Replace(IScreen screen)
  {
    Pop();
    Push(screen);
  }

  public void Clear()
  {
    while (_screens.Count > 0)
      Pop();
  }

  public void Draw(GameUI ui)
  {
    ui.Clear();

    // Start at the topmost screen that isn't an overlay and draw upwards,
    // so overlays appear on top of whatever they're covering.
    int first = Math.Max(0, _screens.FindLastIndex(s => !s.IsOverlay));
    for (int i = first; i < _screens.Count; i++)
      _screens[i].Draw(ui);
  }
}
