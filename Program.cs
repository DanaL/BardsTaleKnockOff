using System.Runtime.InteropServices;
using SDL;
using static SDL.SDL3;
using static SDL.SDL3_ttf;

namespace BardsTaleKnockOff;

// Command line (handy for testing):
//   --quickstart          skip the menus: straight into the town with a party
//   --seed N              which random map to build
//   --pos X Y DIR         starting square and facing (DIR = N, E, S or W)
//   --pillars             use the pillar-hall test map instead of a maze
//   --grove               use an open field with scattered trees instead
//   --range N             how many squares ahead you can see (1-3)
//   --town / --night      outdoor town style by day / by night
//   --map                 start with the debug map showing
//
// The program is a stack of screens (see Screens/). The main loop below
// never changes: it hands each keyboard event to the screen on top, then
// draws, until there are no screens left.
unsafe class Program
{
  // The screen is GameUI.ScreenW x ScreenH logical pixels, scaled up to the
  // window by a whole-number factor, which keeps the pixel art crisp.
  const int WindowScale = 4;

  static int Main(string[] args)
  {
    var options = new NewGameOptions();
    bool quickstart = false;
    var unknownArgs = new List<string>();

    for (int i = 0; i < args.Length; i++)
    {
      switch (args[i].ToLowerInvariant())
      {
        case "--quickstart":
          quickstart = true;
          break;
        case "--seed":
          options.Seed = int.Parse(args[++i]);
          break;
        case "--pos":
          options.X = int.Parse(args[++i]);
          options.Y = int.Parse(args[++i]);
          options.Facing = args[++i].ToUpperInvariant() switch
          {
            "E" => Dir.East,
            "S" => Dir.South,
            "W" => Dir.West,
            _ => Dir.North
          };
          break;
        case "--range":
          options.ViewRange = Math.Clamp(int.Parse(args[++i]), 1, 3);
          break;
        case "--town":
          options.Style = LevelStyle.TownDay;
          break;
        case "--night":
          options.Style = LevelStyle.TownNight;
          break;
        case "--grove":
          options.Grove = true;
          break;
        case "--pillars":
          options.Pillars = true;
          break;
        case "--map":
          options.ShowMap = true;
          break;
        default:
          unknownArgs.Add(args[i]);
          break;
      }
    }

    foreach (var arg in unknownArgs)
      Console.Error.WriteLine($"Unknown option: {arg}");

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

    var app = new App(window, options);
    if (quickstart)
    {
      app.NewGame();
      foreach (var c in app.Roster.Take(Party.MaxMembers))
        app.Game!.Party.Add(c);
      app.Screens.Push(new TownScreen(app));
    }
    else
    {
      app.Screens.Push(new MainMenuScreen(app,
        unknownArgs.Select(a => $"(Unknown command-line option ignored: {a})")));
    }

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
