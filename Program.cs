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

using System.Runtime.InteropServices;
using SDL;
using static SDL.SDL3;
using static SDL.SDL3_ttf;

namespace BardsTaleKnockOff;

// The program is a stack of screens (see Screens/). The main loop below
// never changes: it hands each keyboard event to the screen on top, then
// draws, until there are no screens left.
unsafe class Program
{
  // The screen is GameUI.ScreenW x ScreenH logical pixels, scaled up to the
  // window by a whole-number factor, which keeps the pixel art crisp. The
  // starting window takes up about this fraction of the display, so it grows
  // on high-resolution monitors.
  const double WindowFraction = 0.75;

  static int Main()
  {
    if (!SDL_Init(SDL_InitFlags.SDL_INIT_VIDEO))
    {
      Console.Error.WriteLine($"SDL_Init failed: {SDL_GetError()}");
      return 1;
    }
    if (!TTF_Init())
    {
      Console.Error.WriteLine($"TTF_Init failed: {SDL_GetError()}");
      SDL_Quit();
      return 1;
    }

    //LevelGenerator.GenLevel();
    
    // The largest whole-number scale whose window fills no more than
    // WindowFraction of the display, leaving some room for the title bar.
    int scale = 4;
    int maxScale = 16;
    SDL_Rect usable;
    if (SDL_GetDisplayUsableBounds(SDL_GetPrimaryDisplay(), &usable))
    {
      const int DecorationAllowance = 64;
      int availW = usable.w;
      int availH = usable.h - DecorationAllowance;
      int fit = (int)Math.Min(availW * WindowFraction / GameUI.ScreenW, availH * WindowFraction / GameUI.ScreenH);
      scale = Math.Max(fit, 1);
      maxScale = Math.Max(Math.Min(availW / GameUI.ScreenW, availH / GameUI.ScreenH), 1);
    }

    // A size chosen with Ctrl+ / Ctrl- on this computer overrides the guess.
    if (LoadSavedScale() is int saved)
      scale = Math.Clamp(saved, 1, maxScale);

    // High pixel density gives us the full Retina resolution on macOS, where
    // window sizes are in points and each point is 2x2 pixels.
    SDL_Window* window;
    SDL_Renderer* renderer;
    if (!SDL_CreateWindowAndRenderer("Bard's Tale Knock-Off", GameUI.ScreenW * scale, GameUI.ScreenH * scale,
          SDL_WindowFlags.SDL_WINDOW_RESIZABLE | SDL_WindowFlags.SDL_WINDOW_HIGH_PIXEL_DENSITY, &window, &renderer))
    {
      Console.Error.WriteLine($"Couldn't create window: {SDL_GetError()}");
      SDL_Quit();
      return 1;
    }
    // Half-size text and art are only sharp when the scale in real pixels is
    // even, and the pixel density is only known once the window exists.
    float density = SDL_GetWindowPixelDensity(window);
    scale = CrispScale(scale, density);

    // Some window managers (e.g. on Pop!_OS) cap the size of a new window but
    // allow it to be resized larger afterwards, so ask again now. If it still
    // comes out smaller, use the largest crisp scale that fits the window we
    // got, so there are no black borders.
    SDL_SetWindowSize(window, GameUI.ScreenW * scale, GameUI.ScreenH * scale);
    SDL_SyncWindow(window);
    int actualW, actualH;
    if (SDL_GetWindowSize(window, &actualW, &actualH))
    {
      int fitted = Math.Min(actualW / GameUI.ScreenW, actualH / GameUI.ScreenH);
      if (fitted < scale)
      {
        scale = CrispScale(fitted, density);
        SDL_SetWindowSize(window, GameUI.ScreenW * scale, GameUI.ScreenH * scale);
        SDL_SyncWindow(window);
      }
    }

    SDL_SetRenderLogicalPresentation(renderer, GameUI.ScreenW, GameUI.ScreenH,
      SDL_RendererLogicalPresentation.SDL_LOGICAL_PRESENTATION_INTEGER_SCALE);

    // SDL starts with text input on; screens switch it on only when they
    // want typed characters (see App.StartTextInput).
    SDL_StopTextInput(window);

    var assetDir = Path.Combine(AppContext.BaseDirectory, "assets");
    var ui = new GameUI(renderer,
      new ViewRenderer(renderer, assetDir),
      Path.Combine(assetDir, "m5x7.ttf"));

    int seed = DateTime.UtcNow.GetHashCode();
    seed = -1827369015;
    Console.WriteLine(seed);
    
    Rng rng = new(seed);

    var app = new App(window, rng);    
    app.Screens.Push(new MainMenuScreen(app));

    ulong lastTick = SDL_GetTicks();
    while (!app.Screens.IsEmpty)
    {
      SDL_Event e;
      while (!app.Screens.IsEmpty && SDL_PollEvent(&e))
      {
        switch ((SDL_EventType)e.type)
        {
          case SDL_EventType.SDL_EVENT_QUIT:
            app.Screens.Clear();
            break;
          case SDL_EventType.SDL_EVENT_KEY_DOWN:
            if (ZoomStep(e.key) is int step and not 0)
              scale = ChangeScale(window, scale, step, density, maxScale);
            else
              app.Screens.Top?.OnKey(e.key);
            break;
          case SDL_EventType.SDL_EVENT_TEXT_INPUT:
            app.Screens.Top?.OnText(Marshal.PtrToStringUTF8((nint)e.text.text) ?? "");
            break;
        }
      }
      if (app.Screens.IsEmpty)
        break;

      ulong now = SDL_GetTicks();
      app.Screens.Update(Math.Min(now - lastTick, 250));
      lastTick = now;
      
      SDL_SetRenderDrawColor(renderer, 0, 0, 0, 255);
      SDL_RenderClear(renderer);
      app.Screens.Draw(ui);
      SDL_RenderPresent(renderer);
      SDL_Delay(16);
    }

    SDL_DestroyRenderer(renderer);
    SDL_DestroyWindow(window);
    TTF_Quit();
    SDL_Quit();
    
    return 0;
  }

  // Ctrl and + (or =, so Shift isn't needed) grows the window; Ctrl and -
  // shrinks it. Returns +1, -1, or 0 for any other key.
  static int ZoomStep(SDL_KeyboardEvent key)
  {
    if ((key.mod & SDL_Keymod.SDL_KMOD_CTRL) == 0)
      return 0;

    return key.key switch
    {
      SDL_Keycode.SDLK_EQUALS or SDL_Keycode.SDLK_PLUS or SDL_Keycode.SDLK_KP_PLUS => 1,
      SDL_Keycode.SDLK_MINUS or SDL_Keycode.SDLK_KP_MINUS => -1,
      _ => 0
    };
  }

  // Resizes the window to the next crisp scale in the given direction, if
  // there is one that fits the display, and remembers it for next time. The
  // renderer's logical presentation rescales the game to the new size.
  static int ChangeScale(SDL_Window* window, int scale, int step, float density, int maxScale)
  {
    for (int s = scale + step; s >= 1 && s <= maxScale; s += step)
    {
      if (CrispScale(s, density) != s)
        continue;

      SDL_RestoreWindow(window);  // a maximized window ignores SetWindowSize
      SDL_SetWindowSize(window, GameUI.ScreenW * s, GameUI.ScreenH * s);
      SDL_SyncWindow(window);
      SaveScale(s);
      return s;
    }

    return scale;
  }

  static string ScaleFile => Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    Util.GameFolder, "options.txt");

  static int? LoadSavedScale()
  {
    try
    {
      return int.TryParse(File.ReadAllText(ScaleFile).Trim(), out int s) ? s : null;
    }
    catch (Exception)
    {
      return null;
    }
  }

  static void SaveScale(int scale)
  {
    try
    {
      Directory.CreateDirectory(Path.GetDirectoryName(ScaleFile)!);
      File.WriteAllText(ScaleFile, scale.ToString());
    }
    catch (Exception)
    {
      // Not worth stopping the game over; the size just won't be remembered.
    }
  }

  // The largest scale up to maxScale whose size in real pixels (scale *
  // density) is even, so each half-size source pixel covers whole pixels.
  // Falls back to 1 if there is none, e.g. a tiny display.
  static int CrispScale(int maxScale, float density)
  {
    for (int s = maxScale; s >= 1; s--)
    {
      float pixels = s * density;
      if (pixels == MathF.Round(pixels) && (int)pixels % 2 == 0)
        return s;
    }
    return 1;
  }
}
