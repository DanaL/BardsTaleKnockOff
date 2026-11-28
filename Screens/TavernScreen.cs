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

class TavernScreen : IScreen
{
  readonly App _app;
  string _message = "";

  Monster? _recruit = null;

  public TavernScreen(App app)
  {
    _app = app;

    _recruit = _app.Rng.Next(3) switch
    {
      0 => MonsterCatalog.Get(Monsters.Nomad, _app.Rng),
      1 => MonsterCatalog.Get(Monsters.Conjurer0, _app.Rng),
      _ => MonsterCatalog.Get(Monsters.Magician0, _app.Rng)
    };
  }

  public void OnKey(SDL_KeyboardEvent key)
  {
    switch (key.key)
    {
      case SDL_Keycode.SDLK_B:
        BuyDrink();
        break;
      case SDL_Keycode.SDLK_ESCAPE:
      case SDL_Keycode.SDLK_E:
        _app.GameState!.Facing = _app.GameState.Facing.Opposite();
        var (dx, dy) = _app.GameState.Facing.Delta();
        _app.GameState.X += dx;
        _app.GameState.Y += dy;
        _app.Screens.Pop();
        break;
      case SDL_Keycode.SDLK_R:
        RecruitHireling();
        break;
    }
  }

  void RecruitHireling()
  {
    if (_recruit is null)
      return;
      
    int totalGold = _app.GameState.Party.Members.OfType<Character>().Sum(c => c.Gold);
    if (totalGold < 50)
      _app.Screens.Push(new PopupScreen(_app, "None of the sellswords here are willing to work on credit."));
    else
    {
      string msg = $"There is {_recruit.Name.IndefArticle()} here willing to join your band for 50 gold pieces. Hire them?";
      _app.Screens.Push(new ConfirmScreen(_app, msg, HireRecruit));
    }
  }

  void HireRecruit()
  {
    if (_recruit is not null)
    {
      _app.GameState.Party.Special = _recruit;      
      _app.Screens.Push(new PopupScreen(_app, $"{_recruit.FullName.Capitalize()} joins your party!"));
      
      // Give the recruit a few extra HP so the party gets their money worth
      int bonusHP = _app.Rng.Next(10, 21);
      _recruit.MaxHP += bonusHP;
      _recruit.HP += bonusHP;
      
      int invoice = 50;
      foreach (var c in _app.GameState.Party.Members.OfType<Character>())
      {
        int g = int.Min(invoice, c.Gold);
        c.Gold -= g;
        invoice -= g;
        if (invoice <= 0)
          break;
      }

      _recruit = null;
    }    
  }

  void BuyDrink()
  {
    if (_app.GameState.Party.Members[0] is not Character c)  
      return;

    _message = "";

    if (c.Condition == "DEAD")
      _message = $"{c.Name} is in no shape for drinking.";
    else if (c.Gold < 2)
      _message = "Sorry, we stopped allowing adventurers to run up tabs.";
    else
    {
      c.Gold -= 2;
      _app.GameState.Turn += GameState.TURNS_PER_HOUR;
      _app.GameState.Party.CheckBuffs(_app.GameState.Turn);
      var txt = File.ReadAllText(ResourcePath.GetDataFilePath("bartender.txt"));
      var bartenderLines = txt.Split('#');
      _message = bartenderLines[_app.Rng.Next(bartenderLines.Length)].Trim();
    }
  }

  public void Draw(GameUI ui)
  {
    List<string> lines = [ "Welcome to the Pickled Elf!", "", "Karnov the bartender lazily wipes counter. Care for a drink?", "", "B) Buy a drink 2 gp" ];
    if (_recruit is not null)
      lines.Add("R) Recruit a hireling");
    lines.Add( "E) Exit");

    if (_message != "")
      lines.AddRange("", _message);

    ui.DrawNoBackground("Tavern");
    ui.DrawTextPanel(lines);
    ui.DrawRoster(_app.GameState);
  }
}