using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace bowtie
{
    public class ChatView : GameStateView
    {
        private GameStateEnum nextState = GameStateEnum.Chat;
        private SpriteFont mainFont;
        private SpriteFont titleFont;
        private List<Message> messages = new List<Message>();

        public override void loadContent(ContentManager contentManager)
        {
            mainFont = contentManager.Load<SpriteFont>("Fonts/CourierPrime");
            titleFont = contentManager.Load<SpriteFont>("Fonts/CourierPrimeLg");
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
            float x = 100;

            float bottom = drawTitle(titleFont, $"Twitch Chat", 200, x, biggest.X + 25);
            foreach (Message msg in messages)
            {
                bottom = drawMessage(mainFont, msg, bottom, x, biggest.X);
            }

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
            List<Message> newMessages = socket.GetMessages();
            foreach (Message msg in newMessages)
            {
                messages.Insert(0, msg);
            }
        }

        private void quit(GameTime gameTime, float value)
        {
            nextState = GameStateEnum.MainMenu;
        }

        private float drawMessage(SpriteFont font, Message msg, float y, float x, float xSize) {
            Vector2 measured = font.MeasureString($"{msg.displayName}: {msg.message}");
            Vector2 displayMeasure = font.MeasureString($"{msg.displayName}: ");

            spriteBatch.DrawString(font, $"{msg.displayName}: ", new Vector2(x, y), msg.color);
            spriteBatch.DrawString(font, msg.message, new Vector2(x + displayMeasure.X, y), Color.Black);

            return measured.Y + y;
        }

        private float drawTitle(SpriteFont font, string text, float y, float x, float xSize)
        {
            Vector2 stringSize = font.MeasureString(text);
            spriteBatch.DrawString(font, text, new Vector2(x, y), Color.Black);
            return y + stringSize.Y;
        }
    }
}