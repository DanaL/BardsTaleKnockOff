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

namespace BardsTaleKnockOff;

interface IScreen
{
  // Called when the screen is put on the stack / taken off it.
  void OnEnter() { }
  void OnExit() { }

  void OnKey(SDL_KeyboardEvent key);
  void OnText(string text) { }

  void Draw(GameUI ui);

  // An overlay (e.g. a yes/no question) is drawn on top of the screen
  // beneath it rather than replacing it.
  bool IsOverlay => false;
}

// A screen for picking a spell; this is the method that receives the selection
interface ICastSpellScreen
{
  void SpellChosen(SpellInfo spell);
}

// A screen for picking a group of monsters; this is the method that receives
// the selection
interface IMonsterPickScreen
{
  void MonsterGroupChosen(int groupNum);
}

// A screen for picking a party member as a target; the method that receives
// the choice
interface IPartyPickScreen
{
  void CharacterChosen(int partySlot);
}