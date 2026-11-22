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

namespace BardsTaleKnockOff;

sealed class StatueScreen : IScreen
{
  readonly App _app;
  readonly StatueFact _statueFact;
  readonly bool _partyHasKey;
  bool _popupShown = false;

  public StatueScreen(App app, StatueFact statueFact)
  {    
    _app = app;
    _statueFact = statueFact;
    _partyHasKey = _app.GameState.Party.QuestItems.Contains(_statueFact.UnlockedById);
  }

  public void OnKey(SDL_KeyboardEvent key)
  {
    switch (key.key)
    {
      default:
        _app.Screens.Pop();
        break;
    }
  }

  public void Draw(GameUI ui)
  {
    List<string> lines = [ _statueFact.Desc, "" ];

    if (_partyHasKey && !_popupShown)
    {
      Item key = ItemCatalog.Item(_statueFact.UnlockedById);
      ConfirmScreen cs = new(_app, $"Place {key.Name.DefArticle()} on the statue?", UnlockStatue );
      _app.Screens.Push(cs);
      _popupShown = true;  
    }

    ui.DrawNoBackground("A Statue");
    ui.DrawTextPanel(lines);
    ui.DrawRoster(_app.GameState);
  }

  void UnlockStatue()
  {
    _app.GameState.Party.QuestItems.Remove(_statueFact.UnlockedById);
    string s = "As you place the item on the statue, cracks begin to form. In a moment, the statue has crumbled entirely to rubble, revealing a stairway leading deeper!";
    PopupScreen popup = new(_app, s) 
    { 
      // pop out of the Statue screen and call CheckLocation() to trigger
      // the stairs interaction      
      OnDismiss = () => 
      { 
        _app.Screens.Pop();
        if (_app.Screens.Top is AdventureScreen ascr)
          ascr.CheckLocation();
      } 
    }; 
    _app.Screens.Push(popup);

    // Unlike Bard's Tale, we want to permanently alter the map
    Tile tile = _app.GameState.CurrMap.TileAt(_app.GameState.X, _app.GameState.Y);
    _app.GameState.CurrMap.SetTile(_app.GameState.X, _app.GameState.Y, tile with { Feature = Feature.StairsDown });
    string savePath = Serialize.LevelPath(_app.CampaignName, _app.GameState.CurrLevel);
    MapDiskUtils.Save(_app.GameState.CurrMap, savePath);
  }
}