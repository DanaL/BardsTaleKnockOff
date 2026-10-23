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
  static readonly int[] OuterToInner = [-2, 2, -1, 1, 0];

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
  // generate pieces using the same perspective the artist used. Measuring the
  // centre front faces gives: vanishing point (80, 60); the face one square
  // ahead is 78 px wide, the face two squares ahead is 46x58. Width is
  // proportional to 1/z, so with wall lines at z = CamOffset + n:
  //   78 = Fx / (CamOffset + 1)  and  46 = Fx / (CamOffset + 2)
  // which gives CamOffset = 0.4375 and Fx = 112.125.
  const float CenterX = 80f;
  const float CenterY = 60f;
  const float CamOffset = 0.4375f;
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

  public void Draw(Dungeon map, int px, int py, Dir facing, int viewRange, LevelStyle style, int originX, int originY)
  {
    viewRange = Math.Clamp(viewRange, 1, 3);
    _fade = style.Fade;
    var (fx, fy) = facing.Delta();
    var (rx, ry) = facing.TurnRight().Delta();

    (int x, int y) Square(int lat, int depth) =>
      (px + depth * fx + lat * rx, py + depth * fy + lat * ry);

    SDL_Texture* EdgeTexture(Edge edge) =>
      style.EdgeSheets.TryGetValue(edge, out var name) ? Sheet(name) : null;

    var floorTex = Sheet(style.FloorSheet);
    var ceilingTex = Sheet(style.CeilingSheet);

    void DrawFloorAndCeiling(SDL_Texture* tex, int lat, int depth, byte light)
    {
      if (tex == null)
        return;
      if (depth <= 2 && Floors.TryGetValue((lat, depth), out var piece))
        Blit(tex, piece, originX, originY, light);
      else if (depth == 3 && Floors.TryGetValue((lat, 2), out var farPiece))
        BlitScaled(tex, farPiece, Depth3Scale, originX, originY, light);
    }

    // Generated pieces can hang off the edge of the view; keep them out of
    // whatever is drawn beside it.
    var clip = new SDL_Rect { x = originX, y = originY, w = ViewW, h = ViewH };
    SDL_SetRenderClipRect(_renderer, &clip);

    DrawBackground(style, originX, originY);

    // View range r: squares 0..r-1 are fully lit. Square r is a dim "twilight"
    // square: its floor and side walls show, but its far wall is in the dark.
    // Range 2 is exactly what the art provides.
    //
    // Each square draws the walls you can see from inside it (its far wall and
    // the side wall(s) away from the centre of the view), then its floor and
    // ceiling. The floor/ceiling art is drawn as slabs with some thickness that
    // deliberately overlap the bottom and top few rows of those walls, so they
    // must be drawn after the walls. Otherwise the walls stick out below the
    // floor and doors look like they're floating.
    for (int depth = viewRange; depth >= 0; depth--)
    {
      bool twilight = depth == viewRange;
      byte light = Brightness(depth, twilight);

      foreach (int lat in OuterToInner)
      {
        var (x, y) = Square(lat, depth);

        if (!twilight)
        {
          var tex = EdgeTexture(map.GetEdge(x, y, facing));
          if (tex != null)
          {
            if (FrontWalls.TryGetValue((lat, depth), out var front))
              Blit(tex, front, originX, originY, light);
            else if (depth == 2)
              DrawFarFace(tex, lat, originX, originY, FarWallLight);
          }
        }

        if (lat <= 0)
          DrawSide(EdgeTexture(map.GetEdge(x, y, facing.TurnLeft())), lat, depth, false, originX, originY, light);
        if (lat >= 0)
          DrawSide(EdgeTexture(map.GetEdge(x, y, facing.TurnRight())), lat, depth, true, originX, originY, light);

        DrawFloorAndCeiling(floorTex, lat, depth, light);
        DrawFloorAndCeiling(ceilingTex, lat, depth, light);

        // Features stand on the floor, so they go after it. (Tree sprites
        // include their own patch of ground.)
        var feature = map.GetFeature(x, y);
        if (feature != Feature.None && style.FeatureSheets.TryGetValue(feature, out var featureSheet))
        {
          var tex = Sheet(featureSheet);
          if (depth <= 2 && FeatureSprites.TryGetValue((lat, depth), out var sprite))
            Blit(tex, sprite, originX, originY, light);
          else if (depth == 3 && FeatureSprites.TryGetValue((lat, 2), out var farSprite))
            BlitScaled(tex, farSprite, Depth3Scale, originX, originY, light);
        }
      }
    }

    SDL_SetRenderClipRect(_renderer, null);
  }

  // Sky bands from the top of the view down to the horizon, ground below it.
  // Whatever isn't covered by squares in view shows this.
  void DrawBackground(LevelStyle style, int originX, int originY)
  {
    int bands = style.SkyBands.Length;
    for (int i = 0; i < bands; i++)
    {
      int top = (int)(CenterY * i / bands), bottom = (int)(CenterY * (i + 1) / bands);
      FillRect(style.SkyBands[i], originX, originY + top, ViewW, bottom - top);
    }
    FillRect(style.Ground, originX, originY + (int)CenterY, ViewW, ViewH - (int)CenterY);
  }

  void FillRect(Rgb colour, int x, int y, int w, int h)
  {
    var rect = new SDL_FRect { x = x, y = y, w = w, h = h };
    SDL_SetRenderDrawColor(_renderer, colour.R, colour.G, colour.B, 255);
    SDL_RenderFillRect(_renderer, &rect);
  }

  // The art already darkens squares two ahead, so we only dim what it doesn't
  // cover: the twilight square at range 1, and everything generated beyond.
  const byte FarWallLight = 190;

  static byte Brightness(int depth, bool twilight) => depth switch
  {
    3 => 140,
    2 => 255,
    _ => twilight ? (byte)170 : (byte)255
  };

  void DrawSide(SDL_Texture* tex, int lat, int depth, bool right, int originX, int originY, byte light)
  {
    if (tex == null)
      return;
    if (depth <= 2)
    {
      if (SideWalls.TryGetValue((lat, depth, right), out var piece))
        Blit(tex, piece, originX, originY, light);
    }
    else
    {
      DrawGeneratedSide(tex, lat, depth, right, originX, originY, light);
    }
  }

  // The far wall of a square two ahead: the depth-2 front face, scaled down
  // and shifted sideways for its lateral position.
  void DrawFarFace(SDL_Texture* tex, int lat, int originX, int originY, byte light)
  {
    float s = Depth3Scale;
    float left = FarFaceScreenX + lat * FarFaceSrc.w;
    float right = left + FarFaceSrc.w;
    float top = FarFaceSrc.y, bottom = FarFaceSrc.y + FarFaceSrc.h;

    int x0 = Round(CenterX + (left - CenterX) * s), x1 = Round(CenterX + (right - CenterX) * s);
    int y0 = Round(CenterY + (top - CenterY) * s), y1 = Round(CenterY + (bottom - CenterY) * s);

    var src = FarFaceSrc;
    var dst = new SDL_FRect { x = originX + x0, y = originY + y0, w = x1 - x0, h = y1 - y0 };
    SetLight(tex, light);
    SDL_RenderTexture(_renderer, tex, &src, &dst);
    SetLight(tex, 255);
  }

  // A side wall with no art for it, drawn a column at a time with
  // perspective-correct texturing (the technique Wolfenstein 3D used). For
  // each screen column, undo the projection to find how far away the wall is
  // there, then draw a 1-pixel-wide slice of the texture at that height.
  void DrawGeneratedSide(SDL_Texture* tex, int lat, int depth, bool right, int originX, int originY, byte light)
  {
    float wallX = right ? lat + 0.5f : lat - 0.5f;
    float zNear = WallZ(depth), zFar = WallZ(depth + 1);
    float sxNear = CenterX + Fx * wallX / zNear;
    float sxFar = CenterX + Fx * wallX / zFar;

    int from = Math.Max(0, (int)MathF.Floor(MathF.Min(sxNear, sxFar)));
    int to = Math.Min(ViewW, (int)MathF.Ceiling(MathF.Max(sxNear, sxFar)));

    SetLight(tex, light);
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
      SDL_RenderTexture(_renderer, tex, &src, &dst);
    }
    SetLight(tex, 255);
  }

  void Blit(SDL_Texture* tex, Piece piece, int originX, int originY, byte light)
  {
    int w = piece.X1 - piece.X0 + 1;
    var src = new SDL_FRect { x = piece.X0, y = piece.Row * ViewH, w = w, h = ViewH };
    var dst = new SDL_FRect { x = originX + piece.X0 % ViewW, y = originY, w = w, h = ViewH };
    SetLight(tex, light);
    SDL_RenderTexture(_renderer, tex, &src, &dst);
    SetLight(tex, 255);
  }

  // Draw a piece scaled by s about the vanishing point, i.e. as if it were
  // further away. Edges are rounded so neighbouring pieces still meet exactly.
  void BlitScaled(SDL_Texture* tex, Piece piece, float s, int originX, int originY, byte light)
  {
    int w = piece.X1 - piece.X0 + 1;
    float left = piece.X0 % ViewW;
    int x0 = Round(CenterX + (left - CenterX) * s), x1 = Round(CenterX + (left + w - CenterX) * s);
    int y0 = Round(CenterY - CenterY * s), y1 = Round(CenterY + (ViewH - CenterY) * s);

    var src = new SDL_FRect { x = piece.X0, y = piece.Row * ViewH, w = w, h = ViewH };
    var dst = new SDL_FRect { x = originX + x0, y = originY + y0, w = x1 - x0, h = y1 - y0 };
    SetLight(tex, light);
    SDL_RenderTexture(_renderer, tex, &src, &dst);
    SetLight(tex, 255);
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
