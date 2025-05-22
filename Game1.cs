using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using bowtie;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
namespace Twitch_Galaga
{

    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private GameServiceContainer _service;
        private SpriteBatch _spriteBatch;
        private Dictionary<GameStateEnum, IGameState> gameStates;
        public KeyboardInput keyboard;
        private IGameState currentState;
        private bool loading = false;
        private bool saving = false;
        private TwitchSocket socket;
        private Storage storage;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

        }

        protected override void Initialize()
        {
            if (File.Exists(".env"))
            {
                foreach (var line in File.ReadAllLines(".env"))
                {
                    var parts = line.Split('=', StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length != 2)
                    {
                        continue;
                    }
                    Environment.SetEnvironmentVariable(parts[0], parts[1]);
                }
            }
            else
            {
                System.Console.WriteLine("Error: Unable to load Environment Variables");
            }

            // This size is WAY too big for my computer font sizes were build for such.
            // Font sizes will be small for the given larger screen.
            _graphics.PreferredBackBufferWidth = 1920;
            _graphics.PreferredBackBufferHeight = 1080;
            _graphics.ApplyChanges();

            keyboard = new KeyboardInput();
            socket = new TwitchSocket();
            // TODO: Figure out Storage / TwitchSocket
            storage = new Storage(keyboard, socket);

            gameStates = new Dictionary<GameStateEnum, IGameState>
            {
                {GameStateEnum.MainMenu, new MainMenuView()},
                {GameStateEnum.Chat, new ChatView()}
            };


            foreach (var state in gameStates)
            {
                state.Value.initialize(this.GraphicsDevice, _graphics);
                state.Value.setupInput(keyboard, storage);
                state.Value.setSocket(socket);
            }

            currentState = gameStates[GameStateEnum.MainMenu];

            lock (this)
            {
                if (!this.saving)
                {
                    this.saving = true;
                    var result = finalizeLoadAsync();
                    result.Wait();
                }
            }

            loadState();
            System.Console.WriteLine("Getting Storage");
            storage.loadCommands();
            System.Console.WriteLine("Loaded Commands");
            storage.loadToken();
            System.Console.WriteLine("Loaded Token  ");

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            foreach (var item in gameStates)
            {
                item.Value.loadContent(this.Content);

            }

            // TODO: use this.Content to load your game content here
        }

        protected override void Update(GameTime gameTime)
        {
            // if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            //     Exit();
            // TODO: Add your update logic here

            GameStateEnum nextGameState = currentState.processInput(gameTime);
            // Console.WriteLine(nextGameState);

            if (nextGameState == GameStateEnum.Exit)
            {
                Exit();
            }
            else
            {
                currentState.update(gameTime);
                currentState = gameStates[nextGameState];
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // TODO: Add your drawing code here
            currentState.render(gameTime);

            base.Draw(gameTime);
        }


        // TODO: Fix Storage
        private void saveState()
        {
            lock (this)
            {
                if (!this.saving)
                {
                    this.saving = true;
                    finalizeSaveAsync(storage);
                }
            }
        }

        private async Task finalizeSaveAsync(Storage state)
        {
            await Task.Run(() =>
            {
                using (IsolatedStorageFile storageFile = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    try
                    {
                        using (IsolatedStorageFileStream fs = storageFile.OpenFile("TwitchTesting.json", System.IO.FileMode.Create))
                        {
                            System.Console.WriteLine("Writing Storage file");
                                                        if (fs != null)
                                                        {
                                                            DataContractJsonSerializer mySerializer = new DataContractJsonSerializer(typeof(Storage));
                                                            mySerializer.WriteObject(fs, state);
                                                        }
                        }
                    }
                    catch (IsolatedStorageException err)
                    {
                        System.Console.WriteLine("There was an error writing to storage\n{0}", err);
                    }
                }

                this.saving = false;
            });
        }

        private void loadState()
        {
            lock (this)
            {
                if (!this.loading)
                {
                    this.loading = true;
                    var result = finalizeLoadAsync();
                    result.Wait();
                }
            }
        }

                private async Task finalizeLoadAsync()
                {
                    await Task.Run(() =>
                    {
                        using (IsolatedStorageFile storageFile = IsolatedStorageFile.GetMachineStoreForApplication())
                        {
                            try
                            {
                                if (storageFile.FileExists("TwitchTesting.json"))
                                {
                                    System.Console.WriteLine("File Exists");
                                    using (IsolatedStorageFileStream fs = storageFile.OpenFile("TwitchTesting.json", FileMode.Open))
                                    {
                                        if (fs != null)
                                        {
                                            System.Console.WriteLine($"Reading storage");
                                            DataContractJsonSerializer mySerializer = new DataContractJsonSerializer(typeof(Storage));
                                            storage = (Storage)mySerializer.ReadObject(fs);
                                        }
                                    }
                                }
                                else
                                {
                                    System.Console.WriteLine("File doesn't exist yet! Building storage and saving it now");
                                    saveState();
                                }
                            }
                            catch (IsolatedStorageException err)
                            {
                                System.Console.WriteLine("Something broke: {0}", err);
                            }
                        }
                        this.loading = false;
                    });
                }

    }
}