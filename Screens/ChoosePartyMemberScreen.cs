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

class ChoosePartyMemberScreen(App app, IPartyPickScreen src, string prompt, bool specialSlot, int ignore = -1) : IScreen
{
  public string Image { get; set; } = "";
  public string Caption { get; set; } = "";
  public bool DungeonView { get; set; } = true;

  string _status = "";
  List<string> _lines = [];

  public void OnKey(SDL_KeyboardEvent key)
  {
    _status = "";
    if (key.scancode == SDL_Scancode.SDL_SCANCODE_ESCAPE)
    {
      src.AbortSelection();
      app.Screens.Pop();

      return;
    }

    int slot = key.scancode - SDL_Scancode.SDL_SCANCODE_1 + 1;
    if (slot != ignore && slot >= 1 && slot <= Party.MAX_PARTY_SIZE)
    {
      app.Screens.Pop();
      src.CharacterChosen((PartySlot) slot);
    }
    else if (specialSlot && key.scancode == SDL_Scancode.SDL_SCANCODE_S && app.GameState.Party.Special is not null)
    {
      app.Screens.Pop();
      src.CharacterChosen(PartySlot.Special);
    }
    else
    {
      _status = "There's no character in that slot.";
    }
  }

  public void Draw(GameUI ui)
  {
    if (Image != "")
      ui.DrawImage(Image, Caption, null);
    else if (DungeonView)
      ui.DrawDungeonView(app.GameState);
    else
      ui.DrawNoBackground("");
    
    string s = "1";
    if (app.GameState.Party.Count > 1)
      s += $"-{app.GameState.Party.Count}";
    if (specialSlot && app.GameState.Party.Special is not null)
      s += " or S";

    _lines.Clear();
    _lines.Add($"{prompt}: {s}");
    if (_status != "")
      _lines.AddRange("", _status);
      
    ui.DrawTextPanel(_lines);
    ui.DrawRoster(app.GameState);
  }
}