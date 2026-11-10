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
using static SDL.SDL3;

namespace BardsTaleKnockOff;

// Squares in view are addressed relative to the party:
//   depth   0 = the party's square, 1 = one ahead, 2 = two ahead
//   lateral negative = left, 0 = straight ahead, positive = right
unsafe class ViewRenderer
{
  public const int ViewW = 160;
  public const int ViewH = 120;

  // A vertical strip of a tile sheet, from column x0 to x1 inclusive, in the
  // top (row 0) or bottom (row 1) half of the sheet.
  readonly record struct Piece(int X0, int X1, int Row);

  // Far wall (the one facing the party) of the square at (lateral, depth).
  static readonly Dictionary<(int lat, int depth), Piece> FrontWalls = new()
  {
    [(-1, 0)] = new(0, 40, 1),
    [(0, 0)] = new(201, 278, 1),
    [(1, 0)] = new(119, 159, 1),

    [(-2, 1)] = new(0, 10, 0),
    [(-1, 1)] = new(172, 216, 0),
    [(0, 1)] = new(377, 422, 0),
    [(1, 1)] = new(263, 307, 0),
    [(2, 1)] = new(149, 159, 0),
  };

  // The side wall of the square at (lateral, depth) that faces the centre of the
  // view: its left wall if lateral <= 0, its right wall if lateral >= 0. (At
  // lateral 0 there is one of each, hence the extra flag.)
  static readonly Dictionary<(int lat, int depth, bool right), Piece> SideWalls = new()
  {
    [(0, 0, false)] = new(320, 361, 1),
    [(0, 0, true)] = new(438, 479, 1),

    [(-1, 1, false)] = new(480, 492, 0),
    [(0, 1, false)] = new(41, 57, 1),
    [(0, 1, true)] = new(102, 118, 1),
    [(1, 1, true)] = new(627, 639, 0),

    [(-1, 2, false)] = new(11, 27, 0),
    [(0, 2, false)] = new(217, 222, 0),
    [(0, 2, true)] = new(257, 262, 0),
    [(1, 2, true)] = new(132, 148, 0),
  };

  // Floor and ceiling of the square at (lateral, depth).
  static readonly Dictionary<(int lat, int depth), Piece> Floors = new()
  {
    [(-1, 0)] = new(320, 361, 1),
    [(0, 0)] = new(501, 618, 1),
    [(1, 0)] = new(438, 479, 1),

    [(-2, 1)] = new(480, 492, 0),
    [(-1, 1)] = new(0, 57, 1),
    [(0, 1)] = new(201, 278, 1),
    [(1, 1)] = new(102, 159, 1),
    [(2, 1)] = new(627, 639, 0),

    [(-2, 2)] = new(0, 27, 0),
    [(-1, 2)] = new(172, 222, 0),
    [(0, 2)] = new(377, 422, 0),
    [(1, 2)] = new(257, 307, 0),
    [(2, 2)] = new(132, 159, 0),
  };

  // Sprites standing in the square at (lateral, depth), e.g. trees. Same slots
  // as Floors, but some of these images are a pixel or two wider than the
  // floor tiles, so they get their own measurements. There's no entry for the
  // party's own square: features there block movement, so you can't be in one.
  static readonly Dictionary<(int lat, int depth), Piece> FeatureSprites = new()
  {
    [(-1, 0)] = new(320, 362, 1),
    [(1, 0)] = new(437, 479, 1),

    [(-2, 1)] = new(480, 492, 0),
    [(-1, 1)] = new(0, 57, 1),
    [(0, 1)] = new(200, 278, 1),
    [(1, 1)] = new(102, 159, 1),
    [(2, 1)] = new(626, 639, 0),

    [(-2, 2)] = new(0, 27, 0),
    [(-1, 2)] = new(171, 223, 0),
    [(0, 2)] = new(377, 422, 0),
    [(1, 2)] = new(255, 308, 0),
    [(2, 2)] = new(132, 159, 0),
  };

  // Painter's algorithm: draw far squares first so nearer ones cover them.
  // Within a depth band, outer squares go before inner ones.
  // (At depth 4 the view is wide enough to show squares 3 to each side.)
  static readonly int[] OuterToInner = [-3, 3, -2, 2, -1, 1, 0];

  readonly SDL_Renderer* _renderer;
  readonly string _assetDir;

  // Tile sheets by file name, loaded the first time a style asks for them.
  // (Pointers can't be generic type arguments, hence nint.)
  readonly Dictionary<string, nint> _sheets = [];

  DistanceFade _fade = DistanceFade.ToBlack;

  public ViewRenderer(SDL_Renderer* renderer, string assetDir)
  {
    _renderer = renderer;
    _assetDir = assetDir;
  }

  SDL_Texture* Sheet(string? name)
  {
    if (name is null)
      return null;
    if (!_sheets.TryGetValue(name, out var tex))
    {
      tex = (nint)LoadTexture(Path.Combine(_assetDir, name));
      _sheets[name] = tex;
    }
    return (SDL_Texture*)tex;
  }

  SDL_Texture* LoadTexture(string path)
  {
    SDL_Surface* surface = SDL_LoadPNG(path);
    if (surface == null)
      throw new Exception($"Couldn't load {path}: {SDL_GetError()}");
    SDL_Texture* tex = SDL_CreateTextureFromSurface(_renderer, surface);
    SDL_DestroySurface(surface);
    if (tex == null)
      throw new Exception($"Couldn't create texture for {path}: {SDL_GetError()}");
    SDL_SetTextureScaleMode(tex, SDL_ScaleMode.SDL_SCALEMODE_NEAREST);
    return tex;
  }

  // --- Perspective, measured from the art -----------------------------------
  //
  // The art only goes as far as squares two ahead. To show a third square we
  // generate pieces in perspective, and they have to meet the depth-2 art
  // exactly. The vanishing point is (80, 60) and the face two squares ahead is
  // 46x58. The centre side walls two squares ahead end at screen x 62 and 97,
  // so the face three squares ahead spans 63..96, 34 px. Width is
  // proportional to 1/z, so with wall lines at z = CamOffset + n:
  //   46 = Fx / (CamOffset + 2)  and  34 = Fx / (CamOffset + 3)
  // which gives CamOffset = 5/6 and Fx = 130.33. (The art isn't perfectly
  // consistent: fitting the depth-1 and depth-2 faces instead gives 0.4375,
  // which left a pixel gap where generated pieces met the depth-2 side walls.)
  const float CenterX = 80f;
  const float CenterY = 60f;
  const float CamOffset = 10f / 12f;
  const float Fx = 46f * (CamOffset + 2);
  const float Fy = 58f * (CamOffset + 2);

  // Distance (z) of the wall line n squares ahead (0 = the back of the
  // party's square, 1 = its far wall, ...).
  static float WallZ(int n) => CamOffset + n;

  // Scaling a picture about the vanishing point by s is the same as pushing it
  // further away (z / s), so depth-2 art scaled by this stands in for depth 3.
  static readonly float Depth3Scale = WallZ(2) / WallZ(3);

  // The front face two squares ahead (the "E" piece), used as a flat texture
  // for generated walls: sheet x 377..422, y 31..88, drawn on screen at x 57.
  static readonly SDL_FRect FarFaceSrc = new() { x = 377, y = 31, w = 46, h = 58 };
  const float FarFaceScreenX = 57f;

  // A full-view picture (a building interior, a title card...) from assets/,
  // drawn at its own size, centred in the view and clipped to it.
  public void DrawImage(string name, int originX, int originY)
  {
    var tex = Sheet(name);
    float w, h;
    SDL_GetTextureSize(tex, &w, &h);

    // Double-resolution art (320x240) is shrunk to fit the view
    if (w == ViewW * 2 && h == ViewH * 2)
    {
      w = ViewW;
      h = ViewH;
    }

    var dst = new SDL_FRect
    {
      x = originX + MathF.Floor((ViewW - w) / 2),
      y = originY + MathF.Floor((ViewH - h) / 2),
      w = w,
      h = h
    };

    var clip = new SDL_Rect { x = originX, y = originY, w = ViewW, h = ViewH };
    SDL_SetRenderClipRect(_renderer, &clip);
    SDL_RenderTexture(_renderer, tex, null, &dst);
    SDL_SetRenderClipRect(_renderer, null);
  }

  // A small picture from assets/ (e.g. a status icon), drawn at (x, y) at the
  // given size. Art drawn at more than one pixel per logical pixel keeps its
  // detail, since SDL scales the whole screen up.
  public void DrawIcon(string name, int x, int y, int size)
  {
    var tex = Sheet(name);
    var dst = new SDL_FRect { x = x, y = y, w = size, h = size };
    SDL_RenderTexture(_renderer, tex, null, &dst);
  }

  public void Draw(GameState gs, int originX, int originY)
  {
    int px = gs.X;
    int py = gs.Y;
    Dir facing = gs.Facing;
    int viewRange = gs.ViewRange;
    DungeonLevel map = gs.CurrMap;
    _fade = DistanceFade.ToBlack;
    
    var (fx, fy) = facing.Delta();
    var (rx, ry) = facing.TurnRight().Delta();

    (int x, int y) Square(int lat, int depth) =>
      (px + depth * fx + lat * rx, py + depth * fy + lat * ry);

    var ceilingTex = map.Indoors ? Sheet("ceiling.png") : null;

    // Generated pieces can hang off the edge of the view; keep them out of
    // whatever is drawn beside it.
    var clip = new SDL_Rect { x = originX, y = originY, w = ViewW, h = ViewH };
    SDL_SetRenderClipRect(_renderer, &clip);

    DrawBackground(map, gs, originX, originY);

    var stoneFloorTexture = Sheet("floor.png");
    var dirtTexture = Sheet("dirt.png");

    // View range r: squares 0..r are drawn, fading into the dark towards the
    // far edge of square r (the "twilight" square). Its far wall sits right at
    // the edge, where the fog is black, so it gets a fixed dim light instead:
    // a wall there shows faintly, while an open corridor fades out.
    //
    // Each square draws the walls you can see from inside it (its far wall and
    // the side wall(s) away from the centre of the view), then its floor and
    // ceiling. The floor/ceiling art is drawn as slabs with some thickness that
    // deliberately overlap the bottom and top few rows of those walls, so they
    // must be drawn after the walls. Otherwise the walls stick out below the
    // floor and doors look like they're floating.
    _fogFar = WallZ(viewRange + 1);

    for (int depth = viewRange; depth >= 0; depth--)
    {
      bool twilight = depth == viewRange;

      foreach (int lat in OuterToInner)
      {
        var (x, y) = Square(lat, depth);
        Tile tile = map.TileAt(x, y);

        var farTex = EdgeTexture(tile.EdgeForDir(facing));
        if (farTex != null)
        {
          var farShade = twilight ? Shade.Fixed(EdgeWallLight) : Shade.Flat(WallZ(depth + 1));
          if (FrontWalls.TryGetValue((lat, depth), out var front))
            Blit(farTex, front, originX, originY, farShade);
          else if (depth >= 2)
            DrawFarFace(farTex, lat, depth, originX, originY, farShade);
        }

        if (lat <= 0)
          DrawSide(EdgeTexture(tile.EdgeForDir(facing.TurnLeft())), lat, depth, false, originX, originY);
        if (lat >= 0)
          DrawSide(EdgeTexture(tile.EdgeForDir(facing.TurnRight())), lat, depth, true, originX, originY);

        var floorTex = tile.Floor switch
        {
          Floor.Dirt => dirtTexture,
          _ => stoneFloorTexture
        };
        DrawFloorAndCeiling(floorTex, lat, depth, false);
        DrawFloorAndCeiling(ceilingTex, lat, depth, true);

        // Features stand on the floor, so they go after it. (Tree sprites
        // include their own patch of ground.)
        var feature = tile.Feature;
        if (feature != Feature.None && feature.FeatureSheet() is string name)
        {
          var tex = Sheet(name);
          var shade = Shade.Flat(WallZ(depth) + 0.5f);
          if (depth <= 2 && FeatureSprites.TryGetValue((lat, depth), out var sprite))
            Blit(tex, sprite, originX, originY, shade);
          else if (depth == 3 && FeatureSprites.TryGetValue((lat, 2), out var farSprite))
            BlitScaled(tex, farSprite, Depth3Scale, originX, originY, shade);
        }
      }
    }

    SDL_SetRenderClipRect(_renderer, null);

    SDL_Texture* EdgeTexture(Edge edge)
    {
      string? name = edge.EdgeSheet();
      return name is not null ? Sheet(name) : null;
    }

    void DrawFloorAndCeiling(SDL_Texture* tex, int lat, int depth, bool ceiling)
    {
      if (tex == null)
        return;
      if (depth <= 2)
      {
        if (Floors.TryGetValue((lat, depth), out var piece))
          Blit(tex, piece, originX, originY, Shade.Floor);
      }
      else
      {
        DrawGeneratedFloor(tex, lat, depth, ceiling, originX, originY);
      }
    }
  }

  // Sky bands from the top of the view down to the horizon, ground below it.
  // Whatever isn't covered by squares in view shows this.
  void DrawBackground(DungeonLevel map, GameState gs, int originX, int originY)
  {
    Rgb[] bands;
    Rgb ground;

    if (map.Indoors) {
      bands = [new(0, 0, 0)];
      ground = new(0, 0, 0);
    }
    else if (gs.DayTime) 
    {
      bands = [new(64, 104, 184), new(86, 128, 204), new(112, 154, 220), new(146, 184, 232), new(178, 206, 238)];
      ground = new(122, 84, 56);
    }
    else 
    {
      bands = [new(4, 6, 20), new(8, 11, 32), new(14, 18, 46), new(22, 28, 62)];
      ground = new(0, 0, 0);
    }

    for (int i = 0; i < bands.Length; i++)
    {
      int top = (int)(CenterY * i / bands.Length), bottom = (int)(CenterY * (i + 1) / bands.Length);
      FillRect(bands[i], originX, originY + top, ViewW, bottom - top);
    }
    FillRect(ground, originX, originY + (int)CenterY, ViewW, ViewH - (int)CenterY);
  }

  void FillRect(Rgb colour, int x, int y, int w, int h)
  {
    var rect = new SDL_FRect { x = x, y = y, w = w, h = h };
    SDL_SetRenderDrawColor(_renderer, colour.R, colour.G, colour.B, 255);
    SDL_RenderFillRect(_renderer, &rect);
  }

  // --- Distance fog --------------------------------------------------------
  //
  // Light falls off with distance and reaches zero at the far edge of the view
  // range, so floors and walls fade into the dark instead of stopping at a
  // hard edge (which reads as a black wall). Floors and ceilings get further
  // away row by row and side walls column by column, so those are lit in
  // strips; everything else is at one distance.

  // How many squares' distance the fade takes, ending at the far edge of the
  // view range.
  const float FogLength = 1.5f;

  // Distance at which everything is black. Set per frame from the view range.
  float _fogFar;

  // The depth-2 far wall is made from nearer, brighter art, so it's dimmed to
  // match the art's own darkening of squares two ahead.
  const byte FarWallLight = 190;

  // Light for the far wall of the twilight square, at the very edge of the
  // view range, where the fog would otherwise make it black.
  const byte EdgeWallLight = 70;

  enum ShadeKind { Flat, Floor, Side, Fixed }

  // How to work out how far away each part of a piece is. Flat: all at
  // distance Value. Floor: floor/ceiling, distance from the screen row. Side:
  // a side wall at x = Value, distance from the screen column. Fixed: no fog,
  // just Base. Base is the light before fog.
  readonly record struct Shade(ShadeKind Kind, float Value, byte Base = 255)
  {
    public static Shade Flat(float z, byte light = 255) => new(ShadeKind.Flat, z, light);
    public static Shade Fixed(byte light) => new(ShadeKind.Fixed, 0, light);
    public static readonly Shade Floor = new(ShadeKind.Floor, 0);
    public static Shade Side(float wallX) => new(ShadeKind.Side, wallX);
  }

  byte Fogged(byte light, float z) =>
    (byte)MathF.Round(light * Math.Clamp((_fogFar - z) / FogLength, 0f, 1f));

  // Floors are half a square below eye level (ceilings half above), so a row
  // d pixels from the horizon is at z = Fy / 2d.
  static float FloorZ(float y)
  {
    float d = MathF.Abs(y - CenterY);
    return d > 0 ? Fy / (2 * d) : float.MaxValue;
  }

  static float SideZ(float wallX, float x)
  {
    float z = Fx * wallX / (x - CenterX);
    return z > 0 ? z : float.MaxValue;
  }

  byte LightAt(Shade shade, int x0, int y0, int i) => shade.Kind switch
  {
    ShadeKind.Floor => Fogged(shade.Base, FloorZ(y0 + i + 0.5f)),
    ShadeKind.Side => Fogged(shade.Base, SideZ(shade.Value, x0 + i + 0.5f)),
    ShadeKind.Fixed => shade.Base,
    _ => Fogged(shade.Base, shade.Value)
  };

  // Draw src into the view rectangle x0..x1, y0..y1, fogged. Floors are drawn
  // in rows and side walls in columns; runs with the same light go together.
  void DrawLit(SDL_Texture* tex, SDL_FRect src, int x0, int y0, int x1, int y1, int originX, int originY, Shade shade)
  {
    if (x1 <= x0 || y1 <= y0)
      return;

    if (shade.Kind is ShadeKind.Flat or ShadeKind.Fixed)
    {
      var dst = new SDL_FRect { x = originX + x0, y = originY + y0, w = x1 - x0, h = y1 - y0 };
      SetLight(tex, LightAt(shade, x0, y0, 0));
      SDL_RenderTexture(_renderer, tex, &src, &dst);
      SetLight(tex, 255);
      return;
    }

    bool rows = shade.Kind == ShadeKind.Floor;
    int n = rows ? y1 - y0 : x1 - x0;
    int start = 0;
    byte light = LightAt(shade, x0, y0, 0);
    for (int i = 1; i <= n; i++)
    {
      byte next = i < n ? LightAt(shade, x0, y0, i) : (byte)0;
      if (i < n && next == light)
        continue;

      SDL_FRect s = src;
      var d = new SDL_FRect { x = originX + x0, y = originY + y0, w = x1 - x0, h = y1 - y0 };
      if (rows)
      {
        s.y = src.y + src.h * start / n;
        s.h = src.h * (i - start) / n;
        d.y = originY + y0 + start;
        d.h = i - start;
      }
      else
      {
        s.x = src.x + src.w * start / n;
        s.w = src.w * (i - start) / n;
        d.x = originX + x0 + start;
        d.w = i - start;
      }
      SetLight(tex, light);
      SDL_RenderTexture(_renderer, tex, &s, &d);

      start = i;
      light = next;
    }
    SetLight(tex, 255);
  }

  void DrawSide(SDL_Texture* tex, int lat, int depth, bool right, int originX, int originY)
  {
    if (tex == null)
      return;
    if (depth <= 2)
    {
      if (SideWalls.TryGetValue((lat, depth, right), out var piece))
        Blit(tex, piece, originX, originY, Shade.Side(right ? lat + 0.5f : lat - 0.5f));
    }
    else
    {
      DrawGeneratedSide(tex, lat, depth, right, originX, originY);
    }
  }

  // The far wall of a square two or more ahead, which has no art of its own:
  // the face two squares ahead, pushed back to the square's far wall line and
  // shifted sideways for its lateral position.
  void DrawFarFace(SDL_Texture* tex, int lat, int depth, int originX, int originY, Shade shade)
  {
    float s = WallZ(2) / WallZ(depth + 1);
    float left = FarFaceScreenX + lat * FarFaceSrc.w;
    float right = left + FarFaceSrc.w;
    float top = FarFaceSrc.y, bottom = FarFaceSrc.y + FarFaceSrc.h;

    int x0 = Round(CenterX + (left - CenterX) * s), x1 = Round(CenterX + (right - CenterX) * s);
    int y0 = Round(CenterY + (top - CenterY) * s), y1 = Round(CenterY + (bottom - CenterY) * s);

    // The outer depth-2 side walls end a pixel short of where the perspective
    // puts them, so widen the face by a pixel on its outer side. If that side
    // wall is there it's drawn afterwards and covers the extra column.
    if (lat < 0)
      x0--;
    else if (lat > 0)
      x1++;

    // The source art is brighter than the art's own squares two ahead, so dim it to match.
    shade = shade with { Base = (byte)(shade.Base * FarWallLight / 255) };
    DrawLit(tex, FarFaceSrc, x0, y0, x1, y1, originX, originY, shade);
  }

  // A side wall with no art for it, drawn a column at a time with
  // perspective-correct texturing (the technique Wolfenstein 3D used). For
  // each screen column, undo the projection to find how far away the wall is
  // there, then draw a 1-pixel-wide slice of the texture at that height.
  void DrawGeneratedSide(SDL_Texture* tex, int lat, int depth, bool right, int originX, int originY)
  {
    float wallX = right ? lat + 0.5f : lat - 0.5f;
    float zNear = WallZ(depth), zFar = WallZ(depth + 1);
    float sxNear = CenterX + Fx * wallX / zNear;
    float sxFar = CenterX + Fx * wallX / zFar;

    int from = Math.Max(0, (int)MathF.Floor(MathF.Min(sxNear, sxFar)));
    int to = Math.Min(ViewW, (int)MathF.Ceiling(MathF.Max(sxNear, sxFar)));

    for (int sx = from; sx < to; sx++)
    {
      float z = Fx * wallX / (sx + 0.5f - CenterX);
      if (z < zNear || z > zFar)
        continue;
      float u = z - zNear; // 0 at the near end of the wall, 1 at the far end
      int texCol = Math.Clamp((int)(u * FarFaceSrc.w), 0, (int)FarFaceSrc.w - 1);
      float h = Fy / z;

      var src = new SDL_FRect { x = FarFaceSrc.x + texCol, y = FarFaceSrc.y, w = 1, h = FarFaceSrc.h };
      var dst = new SDL_FRect { x = originX + sx, y = originY + MathF.Round(CenterY - h / 2), w = 1, h = MathF.Round(h) };
      SetLight(tex, Fogged(255, z));
      SDL_RenderTexture(_renderer, tex, &src, &dst);
    }
    SetLight(tex, 255);
  }

  // The floor (or ceiling) of a square with no art for it, drawn a row at a
  // time like the generated side walls. Each screen row is a line across the
  // floor at one distance. It's textured from the matching row of the depth-2
  // floor art straight ahead, which spans exactly one square's width at every
  // row, so the generated floor lines up with the walls standing on it.
  void DrawGeneratedFloor(SDL_Texture* tex, int lat, int depth, bool ceiling, int originX, int originY)
  {
    float zNear = WallZ(depth), zFar = WallZ(depth + 1);
    int from = (int)MathF.Floor(Fy / (2 * zFar)), to = (int)MathF.Ceiling(Fy / (2 * zNear));

    var art = Floors[(0, 2)];
    float sheetOffsetX = art.X0 - art.X0 % ViewW; // sheet x minus screen x for this piece

    for (int d = from; d < to; d++) // rows below (or above) the horizon
    {
      float z = Fy / (2 * (d + 0.5f));
      if (z < zNear || z > zFar)
        continue;

      // The same spot in the depth-2 square, and its row in the art.
      float zArt = WallZ(2) + (z - zNear);
      int artD = (int)(Fy / (2 * zArt));
      float artHalfW = Fx * 0.5f / zArt;
      float srcY = art.Row * ViewH + (ceiling ? CenterY - artD - 1 : CenterY + artD);
      var src = new SDL_FRect { x = sheetOffsetX + CenterX - artHalfW, y = srcY, w = 2 * artHalfW, h = 1 };

      int x0 = Round(CenterX + Fx * (lat - 0.5f) / z), x1 = Round(CenterX + Fx * (lat + 0.5f) / z);
      if (x1 <= x0)
        continue;
      float y = ceiling ? CenterY - d - 1 : CenterY + d;
      var dst = new SDL_FRect { x = originX + x0, y = originY + y, w = x1 - x0, h = 1 };
      SetLight(tex, Fogged(255, z));
      SDL_RenderTexture(_renderer, tex, &src, &dst);
    }
    SetLight(tex, 255);
  }

  void Blit(SDL_Texture* tex, Piece piece, int originX, int originY, Shade shade)
  {
    int w = piece.X1 - piece.X0 + 1;
    int x = piece.X0 % ViewW;
    var src = new SDL_FRect { x = piece.X0, y = piece.Row * ViewH, w = w, h = ViewH };
    DrawLit(tex, src, x, 0, x + w, ViewH, originX, originY, shade);
  }

  // Draw a piece scaled by s about the vanishing point, i.e. as if it were
  // further away. Edges are rounded so neighbouring pieces still meet exactly.
  void BlitScaled(SDL_Texture* tex, Piece piece, float s, int originX, int originY, Shade shade)
  {
    int w = piece.X1 - piece.X0 + 1;
    float left = piece.X0 % ViewW;
    int x0 = Round(CenterX + (left - CenterX) * s), x1 = Round(CenterX + (left + w - CenterX) * s);
    int y0 = Round(CenterY - CenterY * s), y1 = Round(CenterY + (ViewH - CenterY) * s);

    var src = new SDL_FRect { x = piece.X0, y = piece.Row * ViewH, w = w, h = ViewH };
    DrawLit(tex, src, x0, y0, x1, y1, originX, originY, shade);
  }

  // How visible something is, 255 = fully. Depending on the level's style
  // that either darkens it or makes it see-through so the sky/ground shows.
  void SetLight(SDL_Texture* tex, byte light)
  {
    if (_fade == DistanceFade.ToBlack)
    {
      SDL_SetTextureColorMod(tex, light, light, light);
      SDL_SetTextureAlphaMod(tex, 255);
    }
    else
    {
      SDL_SetTextureColorMod(tex, 255, 255, 255);
      SDL_SetTextureAlphaMod(tex, light);
    }
  }

  static int Round(float v) => (int)MathF.Floor(v + 0.5f);
}
