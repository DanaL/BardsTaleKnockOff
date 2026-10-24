// {BardsTaleKnockoff} - A roguelike computer RPG
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
  // window by a whole-number factor, which keeps the pixel art crisp.
  const int WindowScale = 4;

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

    SDL_Window* window;
    SDL_Renderer* renderer;
    if (!SDL_CreateWindowAndRenderer("Bard's Tale Knock-Off", GameUI.ScreenW * WindowScale, GameUI.ScreenH * WindowScale,
          SDL_WindowFlags.SDL_WINDOW_RESIZABLE, &window, &renderer))
    {
      Console.Error.WriteLine($"Couldn't create window: {SDL_GetError()}");
      SDL_Quit();
      return 1;
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

    var app = new App(window);
    
      app.Screens.Push(new MainMenuScreen(app));
    
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
            app.Screens.Top?.OnKey(e.key);
            break;
          case SDL_EventType.SDL_EVENT_TEXT_INPUT:
            app.Screens.Top?.OnText(Marshal.PtrToStringUTF8((nint)e.text.text) ?? "");
            break;
        }
      }
      if (app.Screens.IsEmpty)
        break;

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
}
