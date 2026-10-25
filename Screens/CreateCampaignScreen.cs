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

class CreateCampaignScreen(App app) : IScreen
{
  const int MAX_NAME_LENGTH = 15;
  string _name = "";
  string _status = "";

  public void OnEnter() => app.StartTextInput();
  public void OnExit() => app.StopTextInput();

  public void OnText(string text)
  {    
    foreach (char ch in text)
    {
      if (_name.Length < MAX_NAME_LENGTH && (char.IsLetterOrDigit(ch) || ch is ' ' or '_' or '-'))
        _name += ch;
    }

    _status = "";
  }

  bool AlreadyExists() => new DirectoryInfo(Util.SavePath).GetFiles().Any(f => f.Name == $"{_name.Trim()}.dat");

  public void OnKey(SDL_KeyboardEvent key)
  {
    switch (key.key)
    {
      case SDL_Keycode.SDLK_BACKSPACE:
        if (_name.Length > 0)
          _name = _name[..^1];
        break;
      case SDL_Keycode.SDLK_RETURN:
      case SDL_Keycode.SDLK_KP_ENTER:
        if (AlreadyExists())
        {
          _status = "There is already a save file with that name!";
        }
        else if (string.IsNullOrEmpty(_name))
        {
          _status = "Please enter a name for the save file!";
        }
        else
        {
          app.CampaignName = _name.Trim();
          app.NewGame();
          Serialize.WriteSaveGame(app);
          app.Screens.Replace(new GuildScreen(app));
        }
        //app.Screens.Push(new PickLineageScreen(app, _name));
        break;
      case SDL_Keycode.SDLK_ESCAPE:
        app.Screens.Replace(new MainMenuScreen(app));
        break;
    }
  }

  public void Draw(GameUI ui)
  {
    List<string> lines = [ "Create a new campaign", "", "This will generate a new dungeon to explore! Enter a name for the campaign:", 
        "", $"  {_name}_", "", "Esc to cancel." ];
    if (_status.Length > 0)
      lines.AddRange(["", _status]);

    ui.DrawPicture("");
    ui.DrawTextPanel(lines);
    ui.DrawRoster();
  }  
}