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
using static SDL.SDL3;

namespace BardsTaleKnockOff;

// Lays out and draws the whole screen, Bard's Tale style:
//
//   +-------------+          +-------------+
//   | 3D view     |          | Text panel  |
//   +-------------+          +-------------+
//   +--------------------------------------+
//   | Party roster                         |
//   +--------------------------------------+
//
// Everything is in logical pixels; SDL scales the whole screen up to the
// window by a whole-number factor.
//
// GameUI doesn't decide what goes on screen; screens (see Screens/) do, by
// calling the building blocks below.
unsafe class GameUI
{
  public const int ScreenW = 344;
  public const int ScreenH = 226;

  const int Margin = 6;
  const int Padding = 5;

  // Space between the 3D view and the text panel, reserved for status icons
  // (compass, active spells, ...).
  const int IconGap = 24;

  static readonly SDL_Rect ViewRect = new() { x = Margin, y = Margin, w = ViewRenderer.ViewW, h = ViewRenderer.ViewH };
  static readonly SDL_Rect InfoRect = new()
  {
    x = ViewRect.x + ViewRect.w + IconGap,
    y = Margin,
    w = ScreenW - Margin - (ViewRect.x + ViewRect.w + IconGap),
    h = ViewRenderer.ViewH
  };
  static readonly SDL_Rect RosterRect = new()
  {
    x = Margin,
    y = Margin + ViewRenderer.ViewH + Margin,
    w = ScreenW - 2 * Margin,
    h = 2 * Padding + 8 * TextRenderer.BaseLineHeight - 1
  };

  static readonly Rgb Background = new(64, 64, 68);
  static readonly Rgb PanelBorder = new(110, 110, 116);
  static readonly Rgb InfoBackground = new(255, 255, 255);
  static readonly Rgb InfoText = new(0, 0, 0);
  static readonly Rgb Highlight = new(255, 242, 168);  // pale yellow: selected row
  static readonly Rgb RosterBackground = new(44, 44, 48);
  static readonly Rgb RosterHeading = new(232, 200, 120);
  static readonly Rgb RosterText = new(236, 236, 236);
  static readonly Rgb RosterDim = new(140, 140, 146);
  static readonly Rgb PictureBackground = new(20, 20, 24);
  static readonly Rgb PictureCaption = new(200, 200, 206);

  // Roster column positions, relative to the left of the roster's text area.
  const int ColName = 12;
  const int ColAC = 112;
  const int ColHP = 136;
  const int ColMP = 176;
  const int ColConditions = 216;
  const int ColClass = 276;

  readonly SDL_Renderer* _renderer;
  readonly ViewRenderer _view;
  readonly TextRenderer _text;       // roster: one font pixel per logical pixel
  readonly TextRenderer _smallText;  // text panel: half size, to fit more in

  public GameUI(SDL_Renderer* renderer, ViewRenderer view, string fontPath)
  {
    _renderer = renderer;
    _view = view;
    _text = new TextRenderer(renderer, fontPath);
    _smallText = new TextRenderer(renderer, fontPath, scale: 0.5f);
  }

  // --- Building blocks for screens ---------------------------------------------
  //
  // Each screen draws itself by combining these. A typical screen calls
  // DrawDungeonView or DrawPicture, then DrawTextPanel, then DrawRoster.

  // Fill the whole screen with the background colour. ScreenStack calls this
  // before drawing, so screens don't need to.
  public void Clear() => FillRect(Background, new SDL_Rect { x = 0, y = 0, w = ScreenW, h = ScreenH });

  // The first-person view of the map.
  public void DrawDungeonView(GameState state)
  {
    Frame(ViewRect);
    _view.Draw(state, state.X, state.Y, state.Facing, ViewRect.x, ViewRect.y);
  }

  // Something other than the map in the picture area (the Guild, a shop, a
  // title screen...). Until there's art for these, it's a dark panel with a
  // caption in the middle.
  public void DrawPicture(string caption)
  {
    Frame(ViewRect);
    FillRect(PictureBackground, ViewRect);
    DrawCaption(caption, ViewRect);
  }

  // A picture from assets/ in the picture area, with a caption along the
  // bottom like Bard's Tale's location names. Anything the image doesn't
  // cover (or transparent parts of it) shows the picture background.
  public void DrawImage(string fileName, string caption)
  {
    Frame(ViewRect);
    FillRect(PictureBackground, ViewRect);
    _view.DrawImage(fileName, ViewRect.x, ViewRect.y);

    var band = new SDL_Rect { x = ViewRect.x, y = ViewRect.y + ViewRect.h - CaptionBandH, w = ViewRect.w, h = CaptionBandH };
    FillRect(PictureBackground, band);
    DrawCaption(caption, band);
  }

  const int CaptionBandH = 11;

  // Caption text centred in r. Glyphs are 7 pixels tall.
  void DrawCaption(string caption, SDL_Rect r)
  {
    float x = r.x + (r.w - _text.Measure(caption)) / 2;
    float y = r.y + (r.h - 7) / 2;
    _text.Draw(caption, MathF.Round(x), MathF.Round(y), PictureCaption);
  }

  // Text in the panel to the right of the picture, word-wrapped. If there's
  // more than fits, the last lines are shown, so this works both for menus
  // (which fit) and for the message log (newest at the bottom).
  //
  // highlight is the index of a paragraph to draw on a highlighted
  // background (e.g. the selected row of a list), or -1 for none.
  public void DrawTextPanel(IEnumerable<string> paragraphs, int highlight = -1)
  {
    Frame(InfoRect);
    FillRect(InfoBackground, InfoRect);

    const int InfoPadding = 3;
    int width = InfoRect.w - 2 * InfoPadding;
    int maxLines = (int)((InfoRect.h - 2 * InfoPadding + 1) / _smallText.LineHeight);

    // Remember which paragraph each wrapped line came from, for highlighting.
    var lines = paragraphs
      .SelectMany((p, index) => _smallText.Wrap(p, width).Select(line => (line, index)))
      .ToList();
    int first = Math.Max(0, lines.Count - maxLines);
    float y = InfoRect.y + InfoPadding;
    for (int i = first; i < lines.Count; i++)
    {
      if (lines[i].index == highlight)
      {
        // The line's text starts at y (top of the capitals); the band covers
        // the whole line height, from half a pixel above.
        var band = new SDL_FRect { x = InfoRect.x + 1, y = y - 0.5f, w = InfoRect.w - 2, h = _smallText.LineHeight };
        SDL_SetRenderDrawColor(_renderer, Highlight.R, Highlight.G, Highlight.B, 255);
        SDL_RenderFillRect(_renderer, &band);
      }
      _smallText.Draw(lines[i].line, InfoRect.x + InfoPadding, y, InfoText);
      y += _smallText.LineHeight;
    }
  }

  // A boxed message in the middle of the screen, on top of whatever is
  // already drawn. For questions like "Really quit?".
  public void DrawPrompt(IEnumerable<string> paragraphs)
  {
    const int BoxW = 200;
    const int BoxPadding = 6;
    List<string> lines = [.. paragraphs.SelectMany(p => _text.Wrap(p, BoxW - 2 * BoxPadding))];
    int boxH = 2 * BoxPadding + lines.Count * TextRenderer.BaseLineHeight - 3;
    var box = new SDL_Rect { x = (ScreenW - BoxW) / 2, y = (ScreenH - boxH) / 2, w = BoxW, h = boxH };

    Frame(box);
    FillRect(RosterBackground, box);
    int y = box.y + BoxPadding;
    foreach (var line in lines)
    {
      _text.Draw(line, box.x + BoxPadding, y, RosterText);
      y += TextRenderer.BaseLineHeight;
    }
  }

  // The party roster along the bottom. Null draws the empty slots (e.g. on
  // the main menu, before there is a party).
  public void DrawRoster(Party? party = null)
  {
    Frame(RosterRect);
    FillRect(RosterBackground, RosterRect);

    int left = RosterRect.x + Padding;
    int y = RosterRect.y + Padding;

    _text.Draw("Character", left, y, RosterHeading);
    _text.Draw("AC", left + ColAC, y, RosterHeading);
    _text.Draw("HP", left + ColHP, y, RosterHeading);
    _text.Draw("MP", left + ColMP, y, RosterHeading);
    _text.Draw("Conditions", left + ColConditions, y, RosterHeading);
    _text.Draw("Class", left + ColClass, y, RosterHeading);
    y += TextRenderer.BaseLineHeight;

    DrawRosterLine("S)", party?.Special, left, y);
    y += TextRenderer.BaseLineHeight;
    for (int i = 0; i < Party.MaxMembers; i++)
    {
      DrawRosterLine($"{i + 1})", party?.Members[i], left, y);
      y += TextRenderer.BaseLineHeight;
    }
  }

  void DrawRosterLine(string slot, Character? c, int left, int y)
  {
    _text.Draw(slot, left, y, RosterDim);
    if (c is null)
      return;

    _text.Draw(c.Name, left + ColName, y, RosterText);
    _text.Draw(c.AC.ToString(), left + ColAC, y, RosterText);
    _text.Draw($"{c.HP}/{c.MaxHP}", left + ColHP, y, RosterText);
    _text.Draw(c.MaxMP > 0 ? $"{c.MP}/{c.MaxMP}" : "-", left + ColMP, y, RosterText);
    _text.Draw(c.Condition, left + ColConditions, y, RosterText);
    _text.Draw(c.Role.ToString(), left + ColClass, y, RosterText);
  }

  void FillRect(Rgb colour, SDL_Rect r)
  {
    var rect = new SDL_FRect { x = r.x, y = r.y, w = r.w, h = r.h };
    SDL_SetRenderDrawColor(_renderer, colour.R, colour.G, colour.B, 255);
    SDL_RenderFillRect(_renderer, &rect);
  }

  // A 1-pixel border just outside a panel.
  void Frame(SDL_Rect r) =>
    FillRect(PanelBorder, new SDL_Rect { x = r.x - 1, y = r.y - 1, w = r.w + 2, h = r.h + 2 });
}
