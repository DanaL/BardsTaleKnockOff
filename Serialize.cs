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

using System.Text.Json;

namespace BardsTaleKnockOff;

record struct PartyInfo(int Slot, string Name);
record SaveFileInfo(string CampaignName, string Path);
record SaveGameInfo(List<Character> Roster, List<PartyInfo> Party);

internal class Serialize
{
  public static void WriteSaveGame(App app)
  {
    List<PartyInfo> party = [];
    for (int s = 0; s < app.GameState.Party.Members.Length; s++)
    {
      if (app.GameState.Party.Members[s] is not null)
        party.Add(new PartyInfo(s, app.GameState.Party.Members[s].Name));
    }

    SaveGameInfo sgi = new(app.Roster, party);
    var bytes = JsonSerializer.SerializeToUtf8Bytes(sgi, new JsonSerializerOptions { WriteIndented = false, IncludeFields = true });

    DirectoryInfo saveDir = new(Util.SavePath);
    try
    {
      if (!saveDir.Exists)
        saveDir.Create();
    }
    catch (Exception)
    {
      throw new Exception("Unable to create or access the Save Game folder. Your game was not saved successfully!");
    }

    try
    {
      // In the future, when this is a real game, I'm going to have check player names for invalid characters or build a 
      // little database of players vs saved games
      string filename = $"{app.CampaignName}.dat";
      string fullpath = Path.Combine(saveDir.FullName, filename);
      File.WriteAllBytes(fullpath, bytes);
    }
    catch (Exception)
    {
      throw new Exception("Save failed! Your game was not saved successfully!");
    }
  }

  public static void LoadSaveGame(App app)
  {
    DirectoryInfo saveDir = new(Util.SavePath);
    string path = Path.Combine(saveDir.FullName, $"{app.CampaignName}.dat");

    var bytes = File.ReadAllBytes(path);
    var sgi = JsonSerializer.Deserialize<SaveGameInfo>(bytes) ?? throw new Exception("Failure to deserialize save file :(");
    foreach (var c in sgi.Roster.OrderBy(ch => ch.Name))
      app.Roster.Add(c);

    foreach (var pi in sgi.Party.OrderBy(p => p.Slot))
    {
      Character c = app.Roster.First(ch => ch.Name == pi.Name);
      app.GameState!.Party.Add(c);
    }      
  }  
} 