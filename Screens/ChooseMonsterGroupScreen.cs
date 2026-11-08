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

class ChooseMonsterGroupScreen(App app, Character c, List<MonsterGroup> foes, IMonsterPickScreen src) : IScreen
{
  public string Image { get; set; } = "";
  public string Caption { get; set; } = "";
  public int Range { get; set; } = 10;

  HashSet<SDL_Scancode> _groups = [];

  public void OnKey(SDL_KeyboardEvent key)
  {
    if (key.scancode == SDL_Scancode.SDL_SCANCODE_ESCAPE)
    {
      src.AbortSelection();
      app.Screens.Pop();
      return;
    }

    if (!_groups.Contains(key.scancode))
        return;

    int groupNum = key.scancode - SDL_Scancode.SDL_SCANCODE_A;
    src.MonsterGroupChosen(groupNum);
    app.Screens.Pop();
  }

  public void Draw(GameUI ui)
  {
    List<string> lines = [ "Which group:", ""];

    bool hiding = c.Buffs.Any(b => b.Buff == Buff.Hiding);

    for (int j = 0; j < foes.Count; j++)
    {
      if (foes[j].Distance > Range && !hiding)
        break;
      char ch = (char) ('a' + j);
      int n = foes[j].Count;
      string m = n == 1 ? foes[j].Name : foes[j].Name.Pluralize();
      lines.Add($"{ch}) {n} {m} ({foes[j].Distance}')");      
      _groups.Add(SDL_Scancode.SDL_SCANCODE_A + j);
    }
    
    ui.DrawImage(Image, Caption, null);
    ui.DrawTextPanel(lines);
    ui.DrawRoster(app.GameState);
  }
}