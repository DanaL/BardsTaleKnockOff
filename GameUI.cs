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
    _view.Draw(state.Map, state.X, state.Y, state.Facing, state.ViewRange, state.Style, ViewRect.x, ViewRect.y);
  }

  // Something other than the map in the picture area (the Guild, a shop, a
  // title screen...). Until there's art for these, it's a dark panel with a
  // caption in the middle.
  public void DrawPicture(string caption)
  {
    Frame(ViewRect);
    FillRect(PictureBackground, ViewRect);
    float x = ViewRect.x + (ViewRect.w - _text.Measure(caption)) / 2;
    float y = ViewRect.y + (ViewRect.h - 7) / 2;
    _text.Draw(caption, MathF.Round(x), MathF.Round(y), PictureCaption);
  }

  // Text in the panel to the right of the picture, word-wrapped. If there's
  // more than fits, the last lines are shown, so this works both for menus
  // (which fit) and for the message log (newest at the bottom).
  public void DrawTextPanel(IEnumerable<string> paragraphs)
  {
    Frame(InfoRect);
    FillRect(InfoBackground, InfoRect);

    const int InfoPadding = 3;
    int width = InfoRect.w - 2 * InfoPadding;
    int maxLines = (int)((InfoRect.h - 2 * InfoPadding + 1) / _smallText.LineHeight);

    var lines = paragraphs.SelectMany(p => _smallText.Wrap(p, width)).ToList();
    int first = Math.Max(0, lines.Count - maxLines);
    float y = InfoRect.y + InfoPadding;
    for (int i = first; i < lines.Count; i++)
    {
      _smallText.Draw(lines[i], InfoRect.x + InfoPadding, y, InfoText);
      y += _smallText.LineHeight;
    }
  }

  // A boxed message in the middle of the screen, on top of whatever is
  // already drawn. For questions like "Really quit?".
  public void DrawPrompt(IEnumerable<string> paragraphs)
  {
    const int BoxW = 200;
    const int BoxPadding = 6;
    var lines = paragraphs.SelectMany(p => _text.Wrap(p, BoxW - 2 * BoxPadding)).ToList();
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
  public void DrawRoster(Party? party)
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
    _text.Draw(c.ClassName, left + ColClass, y, RosterText);
  }

  // --- Debug overhead map, drawn in the text panel -----------------------------

  const int MapCell = 5;

  public void DrawMinimap(GameState state)
  {
    Frame(InfoRect);
    FillRect(InfoBackground, InfoRect);

    var map = state.Map;
    int mapX = InfoRect.x + (InfoRect.w - Dungeon.Size * MapCell) / 2;
    int mapY = InfoRect.y + (InfoRect.h - Dungeon.Size * MapCell) / 2;
    FillRect(new Rgb(30, 30, 40), new SDL_Rect { x = mapX, y = mapY, w = Dungeon.Size * MapCell, h = Dungeon.Size * MapCell });

    for (int x = 0; x < Dungeon.Size; x++)
    {
      for (int y = 0; y < Dungeon.Size; y++)
      {
        float left = mapX + x * MapCell, top = mapY + y * MapCell;
        float right = left + MapCell, bottom = top + MapCell;
        if (map.GetFeature(x, y) == Feature.Tree)
          FillRect(new Rgb(40, 140, 50), new SDL_Rect { x = (int)left + 1, y = (int)top + 1, w = MapCell - 2, h = MapCell - 2 });

        // Each square draws its north and west edges. The map wraps, so the
        // last row/column's south/east edges are the first row/column's north/west.
        DrawMapEdge(map.GetEdge(x, y, Dir.North), left, top, right, top);
        DrawMapEdge(map.GetEdge(x, y, Dir.West), left, top, left, bottom);
        if (y == Dungeon.Size - 1)
          DrawMapEdge(map.GetEdge(x, y, Dir.South), left, bottom, right, bottom);
        if (x == Dungeon.Size - 1)
          DrawMapEdge(map.GetEdge(x, y, Dir.East), right, top, right, bottom);
      }
    }

    // Party: a dot in the square plus a pixel showing which way it faces.
    int cx = mapX + state.X * MapCell + 2, cy = mapY + state.Y * MapCell + 2;
    var (fx, fy) = state.Facing.Delta();
    FillRect(new Rgb(255, 230, 60), new SDL_Rect { x = cx, y = cy, w = 1, h = 1 });
    FillRect(new Rgb(255, 80, 60), new SDL_Rect { x = cx + fx, y = cy + fy, w = 1, h = 1 });
  }

  void DrawMapEdge(Edge edge, float x1, float y1, float x2, float y2)
  {
    if (edge == Edge.Open)
      return;
    if (edge == Edge.Door)
      SDL_SetRenderDrawColor(_renderer, 200, 120, 40, 255);
    else
      SDL_SetRenderDrawColor(_renderer, 170, 170, 180, 255);
    SDL_RenderLine(_renderer, x1, y1, x2, y2);
  }

  // --- Helpers -----------------------------------------------------------------

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
