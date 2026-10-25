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

class RollStatsScreen : IScreen
{
  int _str, _dex, _con, _lore, _luck, _hp, _mp;
  readonly App _app;
  readonly string _name;
  readonly Lineage _lineage;
  readonly Role _role;

  public RollStatsScreen(App app, string name, Lineage lineage, Role role)
  {
    _app = app;
    _name = name;
    _lineage = lineage;
    _role = role;

    Roll();
  }

  void Roll()
  {
    _str = _app.Rng.Next(5) + _app.Rng.Next(5) + _app.Rng.Next(5) + 3;
    _dex = _app.Rng.Next(5) + _app.Rng.Next(5) + _app.Rng.Next(5) + 3;
    _con = _app.Rng.Next(5) + _app.Rng.Next(5) + _app.Rng.Next(5) + 3;
    _lore = _app.Rng.Next(5) + _app.Rng.Next(5) + _app.Rng.Next(5) + 3;
    _luck = _app.Rng.Next(5) + _app.Rng.Next(5) + _app.Rng.Next(5) + 3;

    switch (_role)
    {
      case Role.Warrior:
        _str = 8 + _app.Rng.Next(9);
        _con = 8 + _app.Rng.Next(9);
        _hp = 11 + _app.Rng.Next(5);
        break;
      case Role.Rogue:
        _dex = 8 + _app.Rng.Next(9);
        _luck = 8 + _app.Rng.Next(9);
        _hp = 11 + _app.Rng.Next(3);
        break;
      case Role.Hunter:
        _con = 8 + _app.Rng.Next(9);
        _hp = 11 + _app.Rng.Next(4);
        break;
      case Role.Magician:
      case Role.Conjurer:
        _lore = 8 + _app.Rng.Next(9);
        _hp = 11 + _app.Rng.Next(2);       
        break;
    }

    switch (_lineage)
    {
      case Lineage.Human:
        _luck += 2;
        break;
      case Lineage.Orc:
        _str += 1;
        _con += 1;
        break;
      case Lineage.Dwarf:
        _con += 2;
        break;
      case Lineage.Gnome:
        _lore += 2;
        break;
      case Lineage.Elf:
        _dex += 1;
        _lore += 1;
        break;
      case Lineage.Goblin:
        _dex += 1;
        _con += 1;
        break;
    }

    _hp += Character.StatToMod(_con);

    if (_role == Role.Conjurer || _role == Role.Magician)
      _mp = 10 + Character.StatToMod(_lore); 
  }

  public void OnKey(SDL_KeyboardEvent key)
  {
    switch (key.key)
    {
      case SDL_Keycode.SDLK_R:
        Roll();
        break;
      case SDL_Keycode.SDLK_RETURN:
      case SDL_Keycode.SDLK_KP_ENTER:
        Character c = new()
        {
          Name = _name, Role = _role, Lineage = _lineage,
          Str = _str, Dex = _dex, Con = _con, Lore = _lore, Luck = _luck,
          AC = 10, HP = _hp, MaxHP = _hp, MP = _mp, MaxMP = _mp
        };
        _app.Roster.Add(c);
        while (_app.Screens.Top is not GuildScreen)
          _app.Screens.Pop();
        return;
      case SDL_Keycode.SDLK_ESCAPE:
        while (_app.Screens.Top is not GuildScreen)
          _app.Screens.Pop();
        return;
    }
  }

  public void Draw(GameUI ui)
  {
    List<string> lines = ["Create a character", ""];

    string l = _lineage.ToString().ToLower();
    string r = _role.ToString().ToLower();
    lines.AddRange($"{_name}, {l} {r}", "");

    lines.Add($"Strength: {_str}");
    lines.Add($"Dexterity: {_dex}");
    lines.Add($"Constitution: {_con}");
    lines.Add($"Lore: {_lore}");
    lines.Add($"Luck: {_luck}");
    lines.Add("");
    lines.Add($"HP: {_hp}");
    if (_role == Role.Conjurer || _role == Role.Magician)
      lines.Add($"MP: {_mp}");

    lines.AddRange("", "(r) reroll or -enter- to accept");
    lines.AddRange("", "Esc to cancel.");

    ui.DrawPicture("Adventurers' Guild");
    ui.DrawTextPanel(lines);
    ui.DrawRoster(_app.GameState?.Party);
  }
}