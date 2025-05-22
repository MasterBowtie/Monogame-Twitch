using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace bowtie
{
    public interface IGameState
    {
        void initialize(GraphicsDevice graphicsDevice, GraphicsDeviceManager graphics);

        void setSocket(TwitchSocket socket);

        void loadContent(ContentManager contentManager);

        void setupInput(KeyboardInput keyboard, Storage storage);

        GameStateEnum processInput(GameTime gameTime);

        void update(GameTime gameTime);

        void render(GameTime gameTime);
    }
}