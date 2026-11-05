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

class SpendAbilityPointsScreen(App app, Character c) : IScreen
{
  public void OnKey(SDL_KeyboardEvent key)
  {
    bool canRaiseStat = c.Gold >= 100 && c.AbilityPoints > 0;

    switch (key.scancode)
    {
      case SDL_Scancode.SDL_SCANCODE_P:
        for (int j = 0; j < Party.MAX_PARTY_SIZE; j++)
        {
          if (app.GameState.Party.Members[j] == c)
          {
            app.GameState.Party.PoolGold(j);
            break;
          }
        }        
        break;
      case SDL_Scancode.SDL_SCANCODE_1:
        if (canRaiseStat)
        {
          c.Str += 1;
          c.Gold -= 100;
          c.AbilityPoints -= 1;
        }
        break;
      case SDL_Scancode.SDL_SCANCODE_2:
        if (canRaiseStat)
        {
          c.Dex += 1;
          c.Gold -= 100;
          c.AbilityPoints -= 1;
        }
        break;
      case SDL_Scancode.SDL_SCANCODE_3:
        if (canRaiseStat)
        {
          int hpDelta = c.MaxHP - c.HP;
          c.Con += 1;
          c.Gold -= 100;
          c.AbilityPoints -= 1;
          c.HP = c.MaxHP - hpDelta;
        }
        break;
      case SDL_Scancode.SDL_SCANCODE_4:
        if (canRaiseStat)
        {
          int mpDelta = c.MaxMP - c.MP;
          c.Lore += 1;
          c.Gold -= 100;
          c.AbilityPoints -= 1;
          c.MP = c.MaxMP - mpDelta;
        }
        break;
      case SDL_Scancode.SDL_SCANCODE_5:
        if (canRaiseStat)
        {
          c.Luck += 1;
          c.Gold -= 100;
          c.AbilityPoints -= 1;
        }
        break;
      case SDL_Scancode.SDL_SCANCODE_ESCAPE:
        app.Screens.Pop();
        break;
    }
  }

  public void Draw(GameUI ui)
  {
    List<string> lines = [ $"{c.Name} has {c.AbilityPoints} ability points to spend." ];
    
    lines.Add($"Strength: {c.Str}  Dexterity: {c.Dex}  Constitution: {c.Con}");
    lines.Add($"Lore: {c.Lore}  Luck: {c.Luck}");
    lines.Add($"Gold: {c.Gold}");

    lines.AddRange("", "P) Pool gold");

    if (c.Gold < 100)
    {
      lines.AddRange("", "", "Tuition fees to raise an ability are 100 gp.");
    }
    else
    {
      lines.AddRange("", "", "", "Raise which stat?");
      lines.Add("1) Strength");
      lines.Add("2) Dexterity");
      lines.Add("3) Constitution");
      lines.Add("4) Lore");
      lines.Add("5) Luck");
    }
    

    ui.DrawNoBackground("Training Hall");
    ui.DrawTextPanel(lines);
    ui.DrawRoster(app.GameState.Party);
  }
}

class ChangeClassScreen(App app, Character c, HashSet<Role> options) : IScreen
{
  HashSet<SDL_Scancode> _roleChoices = [];

  public void OnKey(SDL_KeyboardEvent key)
  {
    switch (key.scancode)
    {
      case SDL_Scancode.SDL_SCANCODE_ESCAPE:
        app.Screens.Pop();
        break;
      case SDL_Scancode.SDL_SCANCODE_H:
        ChangeToHunter();
        app.Screens.Pop();
        app.Screens.Push(new PopupScreen(app, $"{c.Name} has taken up the path of the Hunter!"));
        break;
      case SDL_Scancode.SDL_SCANCODE_T:
        ChangeToTrickster();
        app.Screens.Pop();
        app.Screens.Push(new PopupScreen(app, $"{c.Name} has taken up the path of the Trickster!"));
        break;
    }
  }

  void ChangeToTrickster()
  {
    c.Role = Role.Trickster;
    c.BaseMP = app.Rng.Next(4, 9);
    c.MP = c.MaxMP;
    c.TricksterSpellLevel = 1;
  }

  void ChangeToHunter()
  {
    c.Role = Role.Hunter;
    c.CritChance = 51;
    c.BaseHP += 5;
    c.HP += 5;
  }

  public void Draw(GameUI ui)
  {
    List<string> lines = [ "Change to which class (this cannot be undone):", "" ];

    foreach (Role role in options)
    {
      (SDL_Scancode opt, string s) = role switch
      {
        Role.Hunter => (SDL_Scancode.SDL_SCANCODE_H, "H) Hunter"),
        Role.Trickster => (SDL_Scancode.SDL_SCANCODE_T, "T) Trickster"),
        Role.Paladin => (SDL_Scancode.SDL_SCANCODE_P, "P) Paladin"),
        Role.Archer => (SDL_Scancode.SDL_SCANCODE_A, "A) Archer"),
        Role.Conjurer => (SDL_Scancode.SDL_SCANCODE_C, "C) Conjurer"),
        Role.Magician => (SDL_Scancode.SDL_SCANCODE_M, "M) Magician"),
        Role.Wizard => (SDL_Scancode.SDL_SCANCODE_W, "W) Wizard"),
        Role.Sorcerer => (SDL_Scancode.SDL_SCANCODE_S, "S) Sorcerer"),
        _ => throw new Exception($"Hmm {role} can't chance classes...")
      };
      _roleChoices.Add(opt);
      lines.Add(s);
    }

    ui.DrawNoBackground("Training Hall");
    ui.DrawTextPanel(lines);
    ui.DrawRoster(app.GameState.Party);
  }
}

class TrainingHallServicesScreen(App app, Character c) : IScreen
{
  const int MAIN = 0;
  const int MEDITATION = 1;

  const char MEDITATION_OPT = 'm';
  const char LEVEL_UP_OPT = 'l';
  const char ABILITY_PT_OPT = 'a';
  const char CHANGE_CLASS_OPT = 'c';

  const char YES_OPT = 'y';
  const char NO_OPT = 'n';

  int _state = MAIN;
   
  HashSet<char> _options = [];

  public void OnKey(SDL_KeyboardEvent key)
  {
    if (_state == MAIN)
    {
      switch (key.scancode)
      {
        case SDL_Scancode.SDL_SCANCODE_1:
        case SDL_Scancode.SDL_SCANCODE_2:
        case SDL_Scancode.SDL_SCANCODE_3:
        case SDL_Scancode.SDL_SCANCODE_4:
        case SDL_Scancode.SDL_SCANCODE_5:
        case SDL_Scancode.SDL_SCANCODE_6:
          int slot = key.scancode - SDL_Scancode.SDL_SCANCODE_1;
          if (slot <= app.GameState.Party.Count && app.GameState.Party.Members[slot] is Character nc)
          {
            c = nc;           
          }          
          break;
        case SDL_Scancode.SDL_SCANCODE_M:
          if (_options.Contains(MEDITATION_OPT))
            _state = MEDITATION;
          break;
        case SDL_Scancode.SDL_SCANCODE_L:
          if (_options.Contains(LEVEL_UP_OPT)) 
          {
            c.LevelUp(app.Rng);
            app.Screens.Push(new PopupScreen(app, $"{c.Name} is now level {c.Level}!"));
          }
          break;
        case SDL_Scancode.SDL_SCANCODE_A:
          if (_options.Contains(ABILITY_PT_OPT))
          {
            app.Screens.Push(new SpendAbilityPointsScreen(app, c));
          }
          break;
        case SDL_Scancode.SDL_SCANCODE_C:
          if (_options.Contains(CHANGE_CLASS_OPT))
          {
            app.Screens.Push(new ChangeClassScreen(app, c, ClassesAvailable()));
          }
          break;
        case SDL_Scancode.SDL_SCANCODE_ESCAPE:
          app.Screens.Pop();
          break;
      }  
    }
    else if (_state == MEDITATION)
    {
      switch (key.scancode)
      {
        case SDL_Scancode.SDL_SCANCODE_N:
          if (_options.Contains(NO_OPT))
          {
            _options.Clear();
            _state = MAIN;  
          }
          break;
        case SDL_Scancode.SDL_SCANCODE_Y:
          if (_options.Contains(NO_OPT))
          {
            int fee = 15 * (c.MaxMP - c.MP);
            c.Gold -= fee;
            c.MP = c.MaxMP;
            app.Screens.Push(new PopupScreen(app, $"{c.Name} medidates and restores their magic energy!"));
            _options.Clear();
            _state = MAIN;  
          }
          break;
        case SDL_Scancode.SDL_SCANCODE_ESCAPE:
          _options.Clear();
          _state = MAIN;
          break;
      }
    }
  }

  public void Draw(GameUI ui)
  {
    List<string> lines = [ $"What can we do for {c.Name} today?", "" ];
    List<string> menuOptions = [];

    if (_state == MAIN)
    {
      int xpNeeded = XPTable.Needed(c.Role, c.Level + 1);
      if (c.XP < xpNeeded) 
      {
        lines.Add($"They require {xpNeeded - c.XP} more XP to reach the next level.");
      }
      else 
      {
        lines.Add($"They are ready to advance to level {c.Level + 1}!");
        menuOptions.Add("L) Level up");
        _options.Add(LEVEL_UP_OPT);
      }

      if (c.AbilityPoints > 0)
      {
        lines.AddRange("They have ability points to spend.");
        _options.Add(ABILITY_PT_OPT);
        menuOptions.Add("A) Use ability points");
      }
      
      if (ClassesAvailable().Count > 0)
      {
        _options.Add(CHANGE_CLASS_OPT);
        menuOptions.Add("C) Change class.");
        if (c.Role == Role.Warrior || c.Role == Role.Rogue)
          menuOptions.AddRange("", "(Warriors and Rogues may only change classes at level 3. After that, they are committed to their path)");
      }

      if (c.MP < c.MaxMP)
      {
        menuOptions.Add("M) Relax in the meditation hall.");
        _options.Add(MEDITATION_OPT);
      }

      lines.Add("");
      lines.AddRange(menuOptions);
    }
    else if (_state == MEDITATION)
    {
      int fee = 15 * (c.MaxMP - c.MP);
      lines.Add($"Some contemplation and breath work will indeed refocus {c.Name}'s mind and restore their MP.");
      if (fee > c.Gold)
      {
        lines.AddRange($"Unfortunately, they lack the {fee} gp we require to book a session.");
      }
      else
      {
        lines.AddRange("", $"Renting time in the meditation hall will cost {fee} gp.", "", "Shall they pay? (y/n)");
        _options.Add(YES_OPT);
        _options.Add(NO_OPT);
      }
    }

    ui.DrawNoBackground("Training Hall");
    ui.DrawTextPanel(lines);
    ui.DrawRoster(app.GameState.Party);
  }

  HashSet<Role> ClassesAvailable()
  {
    HashSet<Role> roles = [];

    switch (c.Role)
    {
      case Role.Warrior:
        if (c.Level == 3 && c.Lore >= 12)
          roles.Add(Role.Paladin);
        if (c.Level == 3 && c.Dex >= 12)
          roles.Add(Role.Archer);
        break;
      case Role.Rogue:
        if (c.Level == 3 && c.Lore >= 12)
          roles.Add(Role.Trickster);
        if (c.Level == 3 && c.Con >= 12)
          roles.Add(Role.Hunter);
        break;
      case Role.Conjurer:
        if (c.ConjurerSpellLevel >= 3 && c.MagicianSpellLevel == 0)
          roles.Add(Role.Magician);
        if (c.ConjurerSpellLevel >= 3 && c.SorcererSpellLevel == 0)
          roles.Add(Role.Sorcerer);
        if (c.ConjurerSpellLevel >= 3 && c.WizardSpellLevel == 0)
          roles.Add(Role.Wizard);
        break;
      case Role.Magician:        
        if (c.MagicianSpellLevel >= 3 && c.ConjurerSpellLevel == 0)
          roles.Add(Role.Conjurer);
        if (c.MagicianSpellLevel >= 3 && c.SorcererSpellLevel == 0)
          roles.Add(Role.Sorcerer);
        if (c.MagicianSpellLevel >= 3 && c.WizardSpellLevel == 0)
          roles.Add(Role.Wizard);
        break;
      case Role.Sorcerer:
        if (c.SorcererSpellLevel >= 3 && c.MagicianSpellLevel == 0)
          roles.Add(Role.Magician);
        if (c.SorcererSpellLevel >= 3 && c.ConjurerSpellLevel == 0)
          roles.Add(Role.Conjurer);
        if (c.SorcererSpellLevel >= 3 && c.WizardSpellLevel == 0)
          roles.Add(Role.Wizard);
        break;
      case Role.Wizard:
        if (c.WizardSpellLevel >= 3 && c.MagicianSpellLevel == 0)
          roles.Add(Role.Magician);
        if (c.WizardSpellLevel >= 3 && c.ConjurerSpellLevel == 0)
          roles.Add(Role.Conjurer);
        if (c.WizardSpellLevel >= 3 && c.SorcererSpellLevel == 0)
          roles.Add(Role.Sorcerer);
        break;
    }

    return roles;
  }
}

class TrainingHallScreen(App app) : IScreen
{
  const int MAIN = 0;

  int _state = MAIN;

  public void OnKey(SDL_KeyboardEvent key)
  {
    if (_state == MAIN)
    {
      switch (key.scancode)
      {
        case SDL_Scancode.SDL_SCANCODE_1:
        case SDL_Scancode.SDL_SCANCODE_2:
        case SDL_Scancode.SDL_SCANCODE_3:
        case SDL_Scancode.SDL_SCANCODE_4:
        case SDL_Scancode.SDL_SCANCODE_5:
        case SDL_Scancode.SDL_SCANCODE_6:
          int slot = key.scancode - SDL_Scancode.SDL_SCANCODE_1;
          if (slot <= app.GameState.Party.Count)
          {
            Character? c = app.GameState.Party.Members[slot];
            if (c is not null)
              app.Screens.Push(new TrainingHallServicesScreen(app, c));
          }          
          break;
        case SDL_Scancode.SDL_SCANCODE_ESCAPE:
          app.GameState.Facing = app.GameState.Facing.Opposite();
          var (dx, dy) = app.GameState.Facing.Delta();
          app.GameState.X += dx;
          app.GameState.Y += dy;
          app.Screens.Pop();
          break;
      }  
    }    
  }

  public void Draw(GameUI ui)
  {
    List<string> lines = [ "Welcome to the Training Hall!", ""];

    if (_state == MAIN)
    {
      lines.Add("Who among you wishes to avail themselves of our services?");
    }

    ui.DrawNoBackground("Training Hall");
    ui.DrawTextPanel(lines);
    ui.DrawRoster(app.GameState.Party);
  }
}