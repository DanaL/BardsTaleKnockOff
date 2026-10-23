using SDL;
using static SDL.SDL3;
using static SDL.SDL3_ttf;

namespace BardsTaleKnockOff;

// Controls: Up/W forward, Down/S back, Left/A turn left, Right/D turn right,
// Q/E sidestep, 1/2/3 set view range, M toggle debug map, F12 screenshot,
// Esc quit.
//
// Command line (handy for testing):
//   --seed N              which random map to build
//   --pos X Y DIR         starting square and facing (DIR = N, E, S or W)
//   --shot FILE.png       draw one frame, save it and exit
//   --pillars             use the pillar-hall test map instead of a maze
//   --grove               use an open field with scattered trees instead
//   --range N             how many squares ahead you can see (1-3)
//   --town / --night      outdoor town style by day / by night
//   --map                 start with the debug map showing
unsafe class Program
{
  // The screen is GameUI.ScreenW x ScreenH logical pixels, scaled up to the
  // window by a whole-number factor, which keeps the pixel art crisp.
  const int WindowScale = 4;

  static int Main(string[] args)
  {
    int seed = Environment.TickCount;
    int px = 0, py = 1;
    Dir facing = Dir.North;
    bool pillars = false;
    bool grove = false;
    bool showMap = false;
    int? viewRangeArg = null;
    var style = LevelStyle.Dungeon;
    var unknownArgs = new List<string>();

    for (int i = 0; i < args.Length; i++)
    {
      switch (args[i].ToLowerInvariant())
      {
        case "--seed":
          seed = int.Parse(args[++i]);
          break;
        case "--pos":
          px = int.Parse(args[++i]);
          py = int.Parse(args[++i]);
          facing = args[++i].ToUpperInvariant() switch
          {
            "E" => Dir.East,
            "S" => Dir.South,
            "W" => Dir.West,
            _ => Dir.North
          };
          break;        
        case "--range":
          viewRangeArg = Math.Clamp(int.Parse(args[++i]), 1, 3);
          break;
        case "--town":
          style = LevelStyle.TownDay;
          break;
        case "--night":
          style = LevelStyle.TownNight;
          break;
        case "--grove":
          grove = true;
          break;
        case "--pillars":
          pillars = true;
          break;
        case "--map":
          showMap = true;
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

    var assetDir = Path.Combine(AppContext.BaseDirectory, "assets");
    var ui = new GameUI(renderer,
      new ViewRenderer(renderer, assetDir),
      Path.Combine(assetDir, "m5x7.ttf"));

    var state = new GameState
    {
      Map = pillars ? MazeBuilder.Pillars()
        : grove ? MazeBuilder.Grove(seed)
        : MazeBuilder.Build(seed),
      Style = style,
      Party = new Party(), // Party.Sample(),
      X = px,
      Y = py,
      Facing = facing,
      ViewRange = viewRangeArg ?? style.DefaultViewRange,
      ShowMap = showMap
    };
    state.Say("Welcome to Dana's Bard's Tale Knockoff!");
    state.Say("");
    state.Say("B) Begin new Adventure!");
    state.Say(" - you will be taken the Adventurers' Guild where you can create new characters.");
    state.Say("");
    state.Say("L) Load saved game");
    state.Say("C) Credits");

    foreach (var arg in unknownArgs)
      state.Say($"(Unknown command-line option ignored: {arg})");
    Console.WriteLine($"Seed {seed}");

    bool running = true;
    while (running)
    {
      SDL_Event e;
      while (SDL_PollEvent(&e))
      {
        if (e.type == (uint)SDL_EventType.SDL_EVENT_QUIT)
        {
          running = false;
        }
        else if (e.type == (uint)SDL_EventType.SDL_EVENT_KEY_DOWN)
        {
          switch (e.key.scancode)
          {
            case SDL_Scancode.SDL_SCANCODE_ESCAPE:
              running = false;
              break;
            case SDL_Scancode.SDL_SCANCODE_UP:
            case SDL_Scancode.SDL_SCANCODE_W:
              state.TryMove(state.Facing);
              break;
            case SDL_Scancode.SDL_SCANCODE_DOWN:
            case SDL_Scancode.SDL_SCANCODE_S:
              state.TryMove(state.Facing.Opposite());
              break;
            case SDL_Scancode.SDL_SCANCODE_LEFT:
            case SDL_Scancode.SDL_SCANCODE_A:
              state.Facing = state.Facing.TurnLeft();
              break;
            case SDL_Scancode.SDL_SCANCODE_RIGHT:
            case SDL_Scancode.SDL_SCANCODE_D:
              state.Facing = state.Facing.TurnRight();
              break;
            case SDL_Scancode.SDL_SCANCODE_Q:
              state.TryMove(state.Facing.TurnLeft());
              break;
            case SDL_Scancode.SDL_SCANCODE_E:
              state.TryMove(state.Facing.TurnRight());
              break;
            case SDL_Scancode.SDL_SCANCODE_1:
            case SDL_Scancode.SDL_SCANCODE_2:
            case SDL_Scancode.SDL_SCANCODE_3:
              state.ViewRange = e.key.scancode - SDL_Scancode.SDL_SCANCODE_1 + 1;
              break;
            case SDL_Scancode.SDL_SCANCODE_M:
              state.ShowMap = !state.ShowMap;
              break;            
          }
        }
      }

      SDL_SetRenderDrawColor(renderer, 0, 0, 0, 255);
      SDL_RenderClear(renderer);
      ui.Draw(state);

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
