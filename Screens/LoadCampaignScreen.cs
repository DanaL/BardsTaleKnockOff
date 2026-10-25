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

class LoadCampaignScreen(App app) : IScreen
{
  const int VisibleRows = 10;
  int _cursor;
  int _scrollTop;

  List<FileInfo> _saveFiles = [.. new DirectoryInfo(Util.SavePath).GetFiles()
                                  .Where(f => f.Extension == ".dat")];

  void MoveCursor(int delta)
  {
    _cursor = Math.Clamp(_cursor + delta, 0, _saveFiles.Count - 1);
    if (_cursor < _scrollTop)
      _scrollTop = _cursor;
    else if (_cursor >= _scrollTop + VisibleRows)
      _scrollTop = _cursor - VisibleRows + 1;
    _scrollTop = Math.Clamp(_scrollTop, 0, Math.Max(0, _saveFiles.Count - VisibleRows));
  }

  public void OnKey(SDL_KeyboardEvent key)
  {
    switch (key.key)
    {
      case SDL_Keycode.SDLK_UP:
        MoveCursor(-1);
        break;
      case SDL_Keycode.SDLK_DOWN:
        MoveCursor(1);
        break;
      case SDL_Keycode.SDLK_PAGEUP:
        MoveCursor(-VisibleRows);
        break;
      case SDL_Keycode.SDLK_PAGEDOWN:
        MoveCursor(VisibleRows);
        break;
      case SDL_Keycode.SDLK_RETURN:
      case SDL_Keycode.SDLK_KP_ENTER:
        app.CampaignName = _saveFiles[_cursor].Name[..^4];
        app.NewGame();
        Serialize.LoadSaveGame(app);
        app.Screens.Replace(new GuildScreen(app));
        break;
      case SDL_Keycode.SDLK_ESCAPE:
        app.Screens.Replace(new MainMenuScreen(app));
        break;
    }
  }

  public void Draw(GameUI ui)
  {
    var lines = new List<string> { "Welcome to the Adventurers' Guild.", "" };
    int highlight = -1;
      
    if (_saveFiles.Count == 0)
    {
      lines.Add("Hmm...there seems to be no saved games available!");
    }
    else
    {
      lines.Add(_scrollTop > 0 ? "  (more above)" : "");
      int end = Math.Min(_scrollTop + VisibleRows, _saveFiles.Count);
      for (int i = _scrollTop; i < end; i++)
      {
        if (i == _cursor)
          highlight = lines.Count;
        lines.Add($"  {_saveFiles[i].Name[..^4]}");
      }
      lines.Add(end < _saveFiles.Count ? "  (more below)" : "");
    }

    ui.DrawTextPanel(lines, highlight);
    ui.DrawPicture("");
    ui.DrawRoster();
  }
}