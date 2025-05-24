using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Threading;
using Microsoft.VisualBasic;
using System.Threading.Tasks;

namespace bowtie
{
    public class MainMenuView : GameStateView
    {
        private enum MenuState
        {
            Twitch,
            Quit,
        }

        private SpriteFont mainFont;
        private Texture2D selector;
        private DateTime timer;
        private string usernameInput = "";
        private bool visible = false;
        private bool connecting = false;

        private MenuState currentSelection = MenuState.Twitch;
        private GameStateEnum nextState = GameStateEnum.MainMenu;
        private bool waitForKeyRelease = true;



        public override void setupInput(KeyboardInput keyboard, Storage storage)
        {
            this.storage = storage;
            this.keyboard = keyboard;
            storage.registerCommand(Keys.Up, waitForKeyRelease, new IInputDevice.CommandDelegate(moveUp), GameStateEnum.MainMenu, Actions.up);
            storage.registerCommand(Keys.Down, waitForKeyRelease, new IInputDevice.CommandDelegate(moveDown), GameStateEnum.MainMenu, Actions.down);
            storage.registerCommand(Keys.Enter, waitForKeyRelease, new IInputDevice.CommandDelegate(selectItem), GameStateEnum.MainMenu, Actions.select);
        }

        public override void loadContent(ContentManager contentManager)
        {
            mainFont = contentManager.Load<SpriteFont>("Fonts/CourierPrime");
            selector = contentManager.Load<Texture2D>("Images/MenuSelector");
            timer = new DateTime();
        }

        public override GameStateEnum processInput(GameTime gameTime)
        {
            keyboard.Update(gameTime, GameStateEnum.MainMenu);
            if (nextState != GameStateEnum.MainMenu)
            {
                GameStateEnum nextState = this.nextState;
                this.nextState = GameStateEnum.MainMenu;
                timer = new DateTime();
                return nextState;
            }
            return GameStateEnum.MainMenu;
        }

        public override void update(GameTime gameTime)
        {
            timer += gameTime.ElapsedGameTime;
        }

        public override void render(GameTime gameTime)
        {
            spriteBatch.Begin();

            string testString = $"{timer:HH:mm:ss}";
            Vector2 biggest = mainFont.MeasureString(testString);
            float x = graphics.PreferredBackBufferWidth / 2 - biggest.X / 2 - 25;
            string twitchString = socket.isConnected() ? "Chat" : "Connect Twitch";

            float bottom = drawMenuItem(mainFont, $"{timer:HH:mm:ss}", graphics.PreferredBackBufferHeight * .4f, x, biggest.X + 25, false);
            bottom = drawMenuItem(mainFont, twitchString, bottom, x, biggest.X + 25, currentSelection == MenuState.Twitch);
            drawMenuItem(mainFont, "Exit", bottom, x, biggest.X + 25, currentSelection == MenuState.Quit);

            spriteBatch.End();

        }

        private float drawMenuItem(SpriteFont font, string text, float y, float x, float xSize, bool selected)
        {
            Vector2 stringSize = font.MeasureString(text);
            if (text.Length == 0)
            {
                stringSize = font.MeasureString("Blank");
            }

            if (selected)
            {
                spriteBatch.Draw(selector, new Rectangle((int)x, (int)y, (int)xSize, (int)stringSize.Y), Color.White);
            }
            spriteBatch.DrawString(font, text, new Vector2(graphics.PreferredBackBufferWidth / 2 - stringSize.X / 2, y), Color.White);
            return y + stringSize.Y;

        }

        public void moveUp(GameTime gameTime, float value)
        {
            if (currentSelection != MenuState.Twitch)
            {
                currentSelection--;
            }
        }

        public void moveDown(GameTime gameTime, float value)
        {
            if (currentSelection != MenuState.Quit)
            {
                currentSelection++;
            }
        }

        public void selectItem(GameTime gameTime, float value)
        {
            switch (currentSelection)
            {
                case MenuState.Twitch:
                    {
                        if (socket.isConnected())
                        {
                            nextState = GameStateEnum.Chat;
                        }
                        else if (!connecting)
                        {
                            connectTwitch();
                        }
                        break;
                    }
                case MenuState.Quit:
                    {
                        nextState = GameStateEnum.Exit;
                        break;
                }
            }
        }

        private KeyboardState previousState;
        private void getInput(GameTime gameTime)
        {
            if ((uint)timer.Second % 2 == 0)
            {
                visible = !visible;
            }

            var keyState = Keyboard.GetState();
            var keys = keyState.GetPressedKeys();

            bool shift = keyState.IsKeyDown(Keys.RightShift) || keyState.IsKeyDown(Keys.LeftShift);

            foreach (Keys key in keys)
            {
                if (keyPressed(key))
                {    
                    if ((int)key > 64 && (int)key < 91)
                    {
                        var keyValue = key.ToString();
                        if (!shift)
                        {
                            keyValue = keyValue.ToLower();
                        }
                        usernameInput += keyValue;
                    }
                    else if ((int)key > 47 && (int)key < 58)
                    {
                        var keyValue = key.ToString();
                        keyValue = keyValue.TrimStart('D');

                        usernameInput += keyValue;
                    }
                    else if (key == Keys.Back && usernameInput.Length > 0)
                    {
                        usernameInput = usernameInput.Remove(usernameInput.Length - 1);
                    }
                }
            }
            previousState = keyState;
        }

        private bool keyPressed(Keys key)
        {
            return (Keyboard.GetState().IsKeyDown(key) && !previousState.IsKeyDown(key));
        }

        private void connectTwitch()
        {           
            connecting = true;
            Task.Run(async () =>
            {
                await socket.getAuthorized();
                storage.saveToken();
                lock (this)
                {
                    connecting = false;
                }
            });
        }
    }
}