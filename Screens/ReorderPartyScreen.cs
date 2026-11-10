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

using BardsTaleKnockOff;

class ReorderPartyScreen(App app, string? image, string caption) : IScreen
{
  readonly List<Character> _newOrder = [];
  readonly Character?[] _currentOrder = [.. app.GameState.Party.Members];

  public void OnKey(SDL_KeyboardEvent key)
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
        if (_currentOrder[slot] is Character c)
        {
          _newOrder.Add(c);
          _currentOrder[slot] = null;
        }

        if (_currentOrder.OfType<Character>().Count() == 0)
        {
          for (int j = 0; j < Party.MAX_PARTY_SIZE; j++)
          {
            app.GameState.Party.Members[j] = j < _newOrder.Count ? _newOrder[j] : null;
          }
          app.Screens.Pop();
        }
        break;
      case SDL_Scancode.SDL_SCANCODE_ESCAPE:
        app.Screens.Pop();
        break;
    }
  }

  public void Draw(GameUI ui)
  {
    List<string> lines = [ "New party order:"];
    
    for (int j = 0; j < _newOrder.Count; j++)
      lines.Add($"{j+1}) {_newOrder[j].Name}");
    lines.AddRange("", "", "", "", "", "", "", "", "", "", "", "", "", "", "");

    for (int j = 0; j < _currentOrder.Length; j++)
    {
      if (_currentOrder[j] is Character c)
        lines.Add($"{j+1}) {c.Name}");
    }

    if (image is null)
      ui.DrawDungeonView(app.GameState);
    else if (image == "")
      ui.DrawNoBackground(caption);
    else
      ui.DrawImage(image, caption);

    ui.DrawTextPanel(lines);
    ui.DrawRoster(app.GameState);
  }
}