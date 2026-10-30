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

class ChooseMonsterGroupScreen(App app, Character c, List<List<Monster>> foes, IMonsterPickScreen src) : IScreen
{
  public string Image { get; set; } = "";
  public string Caption { get; set; } = "";
  public bool AllGroups { get; set; } = false;

  HashSet<SDL_Scancode> _groups = [];

  public void OnKey(SDL_KeyboardEvent key)
  {
    if (!_groups.Contains(key.scancode))
        return;

    int groupNum = key.scancode - SDL_Scancode.SDL_SCANCODE_A;
    src.MonsterGroupChosen(groupNum);
    app.Screens.Pop();
  }

  public void Draw(GameUI ui)
  {
    List<string> lines = [ "Which group:", ""];

    int n = foes[0].Count;
    string m = n == 1 ? foes[0][0].Name : foes[0][0].Name.Pluralize();
    lines.Add($"a) {n} {m}");

    n = foes[1].Count;
    m = n == 1 ? foes[1][0].Name : foes[1][0].Name.Pluralize();
    lines.Add($"b) {n} {m}");

    _groups = [ SDL_Scancode.SDL_SCANCODE_A, SDL_Scancode.SDL_SCANCODE_B ];

    if (AllGroups || c.Buffs.Any(b => b.Buff == Buff.Hiding))
    {
      if (foes.Count > 2)
      {
        n = foes[2].Count;
        m = n == 1 ? foes[2][0].Name : foes[2][0].Name.Pluralize();
        lines.Add($"c) {n} {m}");
        _groups.Add(SDL_Scancode.SDL_SCANCODE_C);
      }
      if (foes.Count > 3) 
      {
        n = foes[3].Count;
        m = n == 1 ? foes[3][0].Name : foes[3][0].Name.Pluralize();
        lines.Add($"d) {n} {m}");
        _groups.Add(SDL_Scancode.SDL_SCANCODE_D);
      }
    }

    ui.DrawImage(Image, Caption, app.GameState);
    ui.DrawTextPanel(lines);
    ui.DrawRoster(app.GameState.Party);
  }
}