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

  bool IsOverlay => false;
  void Update(ulong elapsedMs) { }
}

// These are interfaces for various UI controls. The class that receives the
// result will implement the interfaces

interface IInventorySelectionScreen
{
  void ItemSelected(InvSlot slot, Item item);
  void AbortSelection();
}

interface ICastSpellScreen
{
  void SpellChosen(SpellInfo spell);
  void AbortSelection();
}

interface IMonsterPickScreen
{
  void MonsterGroupChosen(int groupNum);
  void AbortSelection();
}

interface IPartyPickScreen
{
  void CharacterChosen(int partySlot);
  void AbortSelection();
}