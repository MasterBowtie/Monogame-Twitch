using System;
using System.Collections.Generic;
using System.Dynamic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace bowtie {
  public class KeyboardInput: IInputDevice {
    private KeyboardState previousState;

    private Dictionary<GameStateEnum, Dictionary<Actions, CommandEntry>> stateCommands = new Dictionary<GameStateEnum, Dictionary<Actions, CommandEntry>>(); 

    public struct CommandEntry {
      public Keys key;
      public bool keyPressOnly;
      public IInputDevice.CommandDelegate callback;
      public Actions action;

      public CommandEntry(Keys key, bool keyPressOnly, IInputDevice.CommandDelegate callback, Actions action) {
        this.key = key;
        this.keyPressOnly = keyPressOnly;
        this.callback = callback;
        this.action = action;
      }
    }
    
    public Dictionary<GameStateEnum, Dictionary<Actions, CommandEntry>> getStateCommands() {
      return stateCommands;
    }

    public void registerCommand(Keys key, bool keyPressOnly, IInputDevice.CommandDelegate callback, GameStateEnum state, Actions action) {
      if (stateCommands.ContainsKey(state)) {
        var commandEntries = stateCommands[state];
        Actions remove = Actions.none;
        foreach (var item in commandEntries) {
          if (item.Value.callback == callback) {
            remove = item.Key;
          }
        }
        commandEntries.Remove(remove);

        if (commandEntries.ContainsKey(action)) {
          commandEntries.Remove(action);
        }

        commandEntries.Add(action, new CommandEntry(key, keyPressOnly, callback, action));
      }
      else {
        stateCommands.Add(state, new Dictionary<Actions, CommandEntry>());
        stateCommands[state].Add(action, new CommandEntry(key, keyPressOnly, callback, action));
      }
    }

    public void Update(GameTime gameTime, GameStateEnum state) {
      KeyboardState keyState = Keyboard.GetState();
      if (state != GameStateEnum.Exit) {
      Dictionary<Actions,CommandEntry> commandEntries = stateCommands[state];
        foreach (CommandEntry entry in commandEntries.Values){
          if (entry.keyPressOnly && keyPressed(entry.key)) {
            entry.callback(gameTime, 1.0f);
          }
          else if (!entry.keyPressOnly && keyState.IsKeyDown(entry.key)) {
            entry.callback(gameTime, 1.0f);
          }
        }
      
        previousState = keyState;
      }
    }

    private bool keyPressed(Keys key) {
      return (Keyboard.GetState().IsKeyDown(key) && !previousState.IsKeyDown(key));
    }
  }
}