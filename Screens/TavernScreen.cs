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

class TavernScreen(App app) : IScreen
{
  string _message = "";

  public void OnKey(SDL_KeyboardEvent key)
  {
    switch (key.key)
    {
      case SDL_Keycode.SDLK_B:
        BuyDrink();
        break;
      case SDL_Keycode.SDLK_ESCAPE:
      case SDL_Keycode.SDLK_E:
        app.GameState!.Facing = app.GameState.Facing.Opposite();
        var (dx, dy) = app.GameState.Facing.Delta();
        app.GameState.X += dx;
        app.GameState.Y += dy;
        app.Screens.Pop();
        break;
    }
  }

  void BuyDrink()
  {
    if (app.GameState.Party.Members[0] is not Character c)  
      return;

    _message = "";

    if (c.Condition == "DEAD")
      _message = $"{c.Name} is in no shape for drinking.";
    else if (c.Gold < 2)
      _message = "Sorry, we stopped allowing adventurers to run up tabs.";
    else
    {
      c.Gold -= 2;
      app.GameState.Turn += GameState.TURNS_PER_HOUR;
      var txt = File.ReadAllText(ResourcePath.GetDataFilePath("bartender.txt"));
      var bartenderLines = txt.Split('#');
      _message = bartenderLines[app.Rng.Next(bartenderLines.Length)].Trim();
    }
  }

  public void Draw(GameUI ui)
  {
    List<string> lines = [ "Welcome to the Pickled Elf!", "", "Karnov the bartender lazily wipes counter. Care for a drink?", "", "B) Buy a drink 2 gp.", "E) Exit" ];
    if (_message != "")
      lines.AddRange("", _message);

    ui.DrawNoBackground("Tavern");
    ui.DrawTextPanel(lines);
    ui.DrawRoster(app.GameState);
  }
}