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

using System.Text.Json;

namespace BardsTaleKnockOff;

record struct PartyInfo(int Slot, string Name);
record SaveFileInfo(string CampaignName, string Path);
record SaveGameInfo(List<Character> Roster, List<PartyInfo> Party, ulong Turn, Dictionary<string, Fact>? Facts);

// A campaign is saved as a folder named after it, holding <campaign>.dat
// (roster, party, facts...) and the generated dungeon levels as
// dungeon01.txt, dungeon02.txt... in the same format as the hand-made maps.
internal class Serialize
{
  const string DungeonRegion = "dungeon";

  static string CampaignDir(string campaign) => Path.Combine(Util.SavePath, campaign);
  static string DatPath(string campaign) => Path.Combine(CampaignDir(campaign), $"{campaign}.dat");
  static string LevelPath(string campaign, int level) =>
    Path.Combine(CampaignDir(campaign), $"{DungeonRegion}{level + 1:D2}.txt");

  public static bool CampaignExists(string campaign) => Directory.Exists(CampaignDir(campaign));

  // The saved campaigns, by name.
  public static List<string> CampaignNames() =>
    [.. new DirectoryInfo(Util.SavePath).GetDirectories()
                 .Where(d => File.Exists(DatPath(d.Name)))
                 .Select(d => d.Name)
                 .Order()];

  public static void WriteSaveGame(App app)
  {
    List<PartyInfo> party = [];
    for (int s = 0; s < app.GameState.Party.Members.Length; s++)
    {
      if (app.GameState.Party.Members[s] is Character c)
        party.Add(new PartyInfo(s, c.Name));
    }

    SaveGameInfo sgi = new(app.Roster, party, app.GameState.Turn, app.GameState.FactDb);
    var bytes = JsonSerializer.SerializeToUtf8Bytes(sgi, new JsonSerializerOptions { WriteIndented = false, IncludeFields = true });

    // In the future, when this is a real game, I'm going to have check player names for invalid characters or build a
    // little database of players vs saved games
    string campaign = app.CampaignName;
    try
    {
      Directory.CreateDirectory(CampaignDir(campaign));
    }
    catch (Exception)
    {
      throw new Exception("Unable to create or access the Save Game folder. Your game was not saved successfully!");
    }

    try
    {
      File.WriteAllBytes(DatPath(campaign), bytes);
      WriteDungeon(app.GameState, campaign);
    }
    catch (Exception)
    {
      throw new Exception("Save failed! Your game was not saved successfully!");
    }
  }

  static void WriteDungeon(GameState gs, string campaign)
  {
    if (!gs.Regions.TryGetValue(DungeonRegion, out var levels))
      return;

    for (int i = 0; i < levels.Count; i++)
      MapDiskUtils.Save(levels[i], LevelPath(campaign, i));
  }

  // Reads dungeon01.txt, dungeon02.txt... until one is missing.
  static void LoadDungeon(GameState gs, string campaign)
  {
    List<DungeonLevel> levels = [];
    for (int i = 0; File.Exists(LevelPath(campaign, i)); i++)
      levels.Add(MapDiskUtils.Load(LevelPath(campaign, i)));

    gs.Regions[DungeonRegion] = levels;
  }

  public static void LoadSaveGame(App app)
  {
    string path = DatPath(app.CampaignName);

    var bytes = File.ReadAllBytes(path);
    var sgi = JsonSerializer.Deserialize<SaveGameInfo>(bytes) ?? throw new Exception("Failure to deserialize save file :(");
    foreach (var c in sgi.Roster.OrderBy(ch => ch.Name))
      app.Roster.Add(c);

    foreach (var pi in sgi.Party.OrderBy(p => p.Slot))
    {
      Character c = app.Roster.First(ch => ch.Name == pi.Name);
      app.GameState!.Party.Add(c);
    }

    app.GameState.Turn = sgi.Turn;
    app.GameState.FactDb = sgi.Facts ?? [];

    LoadDungeon(app.GameState, app.CampaignName);
  }
} 