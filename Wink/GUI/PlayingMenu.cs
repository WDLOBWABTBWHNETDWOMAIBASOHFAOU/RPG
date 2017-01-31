﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Wink
{
    class PlayingMenu : Window
    {
        Button backButton;
        Button restartButton;
        Button optionsButton;
        Button quitButton;

        public PlayingMenu() : base(350, 500, true, false)
        {
            SpriteFont sf = GameEnvironment.AssetManager.GetFont("Arial26");
            
            backButton = new Button("button", "Main Menu", sf, Color.Black);
            backButton.Position = new Vector2(25, 25);
            backButton.Action = () =>
            {
                GameEnvironment.GameStateManager.SwitchTo("mainMenuState");
                GameEnvironment.AssetManager.PlayMusic("Sounds/Welcome Screen");
            };
            Add(backButton);

            Vector2 buttonDistance = new Vector2(0, backButton.Sprite.Height + 15);

            restartButton = new Button("button", "Restart", sf, Color.Black);
            restartButton.Position = backButton.Position + buttonDistance;
            restartButton.Action = () =>
            {
                GameSetupState gss = GameEnvironment.GameStateManager.GetGameState("gameSetupState") as GameSetupState;
                PlayingState ps = GameEnvironment.GameStateManager.GetGameState("playingState") as PlayingState;
                ps.Reset();
                gss.InitializeGameMode(ps.CurrentGameMode);
                GameEnvironment.GameStateManager.SwitchTo("gameSetupState");
            };
            Add(restartButton);

            optionsButton = new Button("button", "Options", sf, Color.Black);
            optionsButton.Action = () => { };
            optionsButton.Position = restartButton.Position + buttonDistance;
            Add(optionsButton);

            quitButton = new Button("button", "Quit", sf, Color.Black);
            quitButton.Position = optionsButton.Position + buttonDistance;
            quitButton.Action = () =>
            {
                Treehugger.ExitGame();
            };
            Add(quitButton);
        }
    }
}
