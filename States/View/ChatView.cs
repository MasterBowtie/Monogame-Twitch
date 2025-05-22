using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace bowtie
{
    public class ChatView : GameStateView
    {

        private TwitchSocket socket;
        private GameStateEnum nextState;
        private SpriteFont mainFont;

        public void setSocket(TwitchSocket socket)
        {
            this.socket = socket;
        }

        public override void loadContent(ContentManager contentManager)
        {
            mainFont = contentManager.Load<SpriteFont>("Fonts/CourierPrime");
        }

        public override GameStateEnum processInput(GameTime gameTime)
        {
            keyboard.Update(gameTime, GameStateEnum.Chat);

            if (nextState != GameStateEnum.Chat)
            {
                GameStateEnum nextState = this.nextState;
                this.nextState = GameStateEnum.Chat;
                return nextState;
            }
            return GameStateEnum.Chat;
        }

        public override void render(GameTime gameTime)
        {
             spriteBatch.Begin();

            string testString = "This is a really long message and we are testing the length";

            Vector2 biggest = mainFont.MeasureString(testString);
            float x = graphics.PreferredBackBufferWidth / 2 - biggest.X / 2 - 25;

            float bottom = drawMessage(mainFont, $"Twitch Chat", graphics.PreferredBackBufferHeight * .4f, x, biggest.X + 25);

            spriteBatch.End();
        }

        public override void setupInput(KeyboardInput keyboard, Storage storage)
        {
            this.keyboard = keyboard;
            this.storage = storage;

            storage.registerCommand(Keys.Escape, true, new IInputDevice.CommandDelegate(quit), GameStateEnum.Chat, Actions.exit);
        }

        public override void update(GameTime gameTime)
        {
            //Pending
        }

        private void quit(GameTime gameTime, float value)
        {
            nextState = GameStateEnum.MainMenu;
        }

        private float drawMessage(SpriteFont font, string text, float y, float x, float xSize)
        {
            Vector2 stringSize = font.MeasureString(text);
            spriteBatch.DrawString(font, text, new Vector2(100, y), Color.Black);
            return y + stringSize.Y;
        }
    }
}