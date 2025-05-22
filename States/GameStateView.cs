using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;

namespace bowtie
{
    public abstract class GameStateView : IGameState
    {
        protected GraphicsDeviceManager graphics;
        protected BasicEffect effect;
        protected SpriteBatch spriteBatch;
        protected KeyboardInput keyboard;
        protected Storage storage;
        protected TwitchSocket socket;

        public void initialize(GraphicsDevice graphicsDevice, GraphicsDeviceManager graphics)
        {
            this.graphics = graphics;
            this.spriteBatch = new SpriteBatch(graphicsDevice);
        }

        public void setSocket(TwitchSocket socket)
        {
            this.socket = socket;
        }

        public abstract void setupInput(KeyboardInput keyboard, Storage storage);
        public abstract void loadContent(ContentManager contentManager);
        public abstract GameStateEnum processInput(GameTime gameTime);
        public abstract void render(GameTime gameTime);
        public abstract void update(GameTime gameTime);
    }
}