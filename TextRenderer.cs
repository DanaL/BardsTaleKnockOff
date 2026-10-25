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
using static SDL.SDL3_ttf;

namespace BardsTaleKnockOff;

// Draws text in a pixel font. m5x7 is designed for 16 pt, where one font pixel
// is exactly one screen pixel; at other sizes it gets lumpy. Text is rendered
// without anti-aliasing ("Solid") so it stays crisp when the screen is scaled up.
//
// Rendered strings are cached as textures, since most text on screen (labels,
// roster entries) is the same from frame to frame.
//
// Scale draws the text smaller (or larger) than one font pixel per logical
// pixel. At 0.5, each font pixel covers half a logical pixel, i.e. 2x2 window
// pixels when the screen is scaled up 4x, so text stays crisp as long as the
// window scale is even.
unsafe class TextRenderer
{
  // Glyphs are 7 px tall plus 2 px of descender, so 10 px leaves a 1 px gap.
  // (In font pixels; multiply by Scale for logical pixels.)
  public const int BaseLineHeight = 10;

  // m5x7 renders with 4 blank rows above the capitals. We position text by the
  // top of its capitals, which makes layout maths simpler.
  const int InkTop = 4;

  const int MaxCached = 500;

  readonly SDL_Renderer* _renderer;
  readonly TTF_Font* _font;
  readonly Dictionary<(string text, Rgb colour), (nint tex, int w, int h)> _cache = [];

  public float Scale { get; }
  public float LineHeight => BaseLineHeight * Scale;

  public TextRenderer(SDL_Renderer* renderer, string fontPath, float scale = 1f, float size = 16)
  {
    _renderer = renderer;
    Scale = scale;
    _font = TTF_OpenFont(fontPath, size);
    if (_font == null)
      throw new Exception($"Couldn't load font {fontPath}: {SDL_GetError()}");
  }

  public float Measure(string text)
  {
    if (text.Length == 0)
      return 0;
    int w, h;
    TTF_GetStringSize(_font, text, 0, &w, &h);
    return w * Scale;
  }

  public void Draw(string text, float x, float y, Rgb colour)
  {
    if (text.Length == 0)
      return;

    if (!_cache.TryGetValue((text, colour), out var entry))
    {
      if (_cache.Count >= MaxCached)
        ClearCache();

      var fg = new SDL_Color { r = colour.R, g = colour.G, b = colour.B, a = 255 };
      SDL_Surface* surface = TTF_RenderText_Solid(_font, text, 0, fg);
      if (surface == null)
        return;
      SDL_Texture* tex = SDL_CreateTextureFromSurface(_renderer, surface);
      entry = ((nint)tex, surface->w, surface->h);
      SDL_DestroySurface(surface);
      if (tex == null)
        return;
      SDL_SetTextureScaleMode(tex, SDL_ScaleMode.SDL_SCALEMODE_NEAREST);
      _cache[(text, colour)] = entry;
    }

    var dst = new SDL_FRect { x = x, y = y - InkTop * Scale, w = entry.w * Scale, h = entry.h * Scale };
    SDL_RenderTexture(_renderer, (SDL_Texture*)entry.tex, null, &dst);
  }

  // Split text into lines no wider than width pixels, breaking between words.
  // Leading spaces are kept as an indent on the first line.
  public List<string> Wrap(string text, float width)
  {
    var lines = new List<string>();
    var body = text.TrimStart(' ');
    var line = text[..(text.Length - body.Length)];
    bool lineHasWords = false;
    foreach (var word in body.Split(' '))
    {
      var candidate = lineHasWords ? line + " " + word : line + word;
      if (lineHasWords && Measure(candidate) > width)
      {
        lines.Add(line);
        line = word;
      }
      else
      {
        line = candidate;
      }
      lineHasWords = true;
    }
    lines.Add(line);
    return lines;
  }

  void ClearCache()
  {
    foreach (var (tex, _, _) in _cache.Values) 
    {
      SDL_DestroyTexture((SDL_Texture*)tex);
    }
    _cache.Clear();
  }
}
