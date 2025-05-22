using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Input;

namespace bowtie
{
    [DataContract(Name = "Storage")]
    public class Storage
    {
        private TwitchSocket socket;
        private KeyboardInput keyboard;
        public Storage(KeyboardInput keyboard, TwitchSocket socket)
        {
            this.keyboard = keyboard;
            this.socket = socket;
        }

        [DataMember()]
        public Dictionary<string, string> tokenStorage = null;


        public bool saveToken()
        {
            AccessToken token = socket.getAccessToken();
            if (token == null)
            {
                System.Console.WriteLine("No Token!");
                return false;
            }
            tokenStorage = new Dictionary<string, string>();
            tokenStorage.Add("access_token", token.access_token);
            tokenStorage.Add("refresh_token", token.refresh_token);
            tokenStorage.Add("userId", token.userId.ToString());
            tokenStorage.Add("expires", token.expires.ToString());
            return true;
        }

        public void loadToken()
        {
            if (tokenStorage != null)
            {
                AccessToken token = new AccessToken()
                {
                    access_token = tokenStorage["access_token"],
                    refresh_token = tokenStorage["refresh_token"],
                    userId = uint.Parse(tokenStorage["userId"]),
                    expires = long.Parse(tokenStorage["expires"])
                };
                Task.Run(async () =>
                {

                    await socket.setAccessToken(token);
                    socket.connect();
                });
            }
        }


        [DataMember()]
        public List<(uint, ushort)> HighScore = new List<(uint, ushort)>();

        public void submitScore(uint score, ushort level)
        {
            HighScore.Add((score, level));
            HighScore.Sort(compare);
            if (HighScore.Count > 5)
            {
                HighScore.RemoveAt(5);
            }
        }
        public int compare((uint, ushort) item1, (uint, ushort) item2)
        {
            if (item1.Item2 > item2.Item2)
            {
                return -1;
            }
            else if (item1.Item2 == item2.Item2)
            {
                if (item1.Item1 > item2.Item1)
                {
                    return -1;
                }
            }
            return 1;
        }
        [DataMember()]
        Dictionary<string, Dictionary<string, CommandString>> bindings = new Dictionary<string, Dictionary<string, CommandString>>();

        public struct CommandString
        {
            public string key;
            public bool keyPressOnly;
            public string action;

            public CommandString(Keys key, bool keyPressOnly, Actions action)
            {
                this.key = key.ToString();
                this.keyPressOnly = keyPressOnly;
                this.action = action.ToString();
            }

        }
        public void registerCommand(Keys key, bool keyPressOnly, IInputDevice.CommandDelegate callback, GameStateEnum state, Actions action)
        {
            KeyboardInput.CommandEntry commandEntry = new KeyboardInput.CommandEntry(key, keyPressOnly, callback, action);
            keyboard.registerCommand(key, keyPressOnly, callback, state, action);
            if (bindings.ContainsKey(state.ToString()))
            {
                if (bindings[state.ToString()].ContainsKey(key.ToString()))
                {
                    bindings[state.ToString()][key.ToString()] = new CommandString(key, keyPressOnly, action);
                }
            }
            else
            {
                bindings.Add(state.ToString(), new Dictionary<string, CommandString>());
                bindings[state.ToString()].Add(key.ToString(), new CommandString(key, keyPressOnly, action));

            }
        }

        public void loadCommands()
        {
            var stateCommands = keyboard.getStateCommands();
            foreach (var state in Enum.GetValues(typeof(GameStateEnum)))
            {
                if (!bindings.ContainsKey(state.ToString()))
                {
                    continue;
                }
                var stateBindings = bindings[state.ToString()];
                foreach (var action in Enum.GetValues(typeof(Actions)))
                {
                    if (stateBindings.ContainsKey(action.ToString()))
                    {
                        foreach (var key in Enum.GetValues(typeof(Keys)))
                        {
                            if (stateBindings[key.ToString()].action == action.ToString())
                            {
                                KeyboardInput.CommandEntry commandEntry = stateCommands[(GameStateEnum)state][(Actions)action];
                                commandEntry.key = (Keys)key;
                                commandEntry.action = (Actions)action;
                            }
                        }
                    }
                }
            }
        }
    }
}