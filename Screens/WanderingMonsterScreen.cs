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

class WanderingMonsterScreen(App app, Monster monster) : IScreen
{
  string _caption = monster.Name.IndefArticle().Capitalize();

  public void OnKey(SDL_KeyboardEvent key)
  {
    switch (key.scancode)
    {
      case SDL_Scancode.SDL_SCANCODE_L:
      case SDL_Scancode.SDL_SCANCODE_ESCAPE:
        app.Screens.Pop();
        break;
      case SDL_Scancode.SDL_SCANCODE_F:
        MonsterGroup grp = new() { Monsters = [ monster ], Distance = 10 };
        app.Screens.Replace(new BattleScreen(app, [ grp ], false));
        break;
      case SDL_Scancode.SDL_SCANCODE_A:
        app.GameState.Party.Special = monster;        
        app.Screens.Pop();
        app.Screens.Push(new PopupScreen(app, $"{monster.FullName.Capitalize()} joins your party!"));
        break;
    }
  }

  public void Draw(GameUI ui)
  {
    List<string> lines = [ "A wandering monster offers to join your party!", "", "A) Allow it to join", "F) Fight it", "L) Leave it in peace"];
    
    ui.DrawImage(monster.Image, _caption);
    ui.DrawTextPanel(lines);
    ui.DrawRoster(app.GameState);
  }  
}