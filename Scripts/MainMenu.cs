using UnityEngine;
using UnityEngine.UIElements;
    public class MainMenu : MonoBehaviour
    {
        public UIDocument uiDocument;
        
        private VisualElement Root => uiDocument.rootVisualElement;

        public void Start()
        {
            var playButton = Root.Q<Button>("play-button");
            var optionsButton = Root.Q<Button>("options-button");
            var quitButton = Root.Q<Button>("quit-button");
            
            Time.timeScale = 0;
            
            playButton.clicked += PlayGame;
            quitButton.clicked += QuitGame;
        }

        private static void QuitGame()
        {
            print("Exiting game...");
            Application.Quit();
        }

        private void PlayGame()
        {
            Time.timeScale = 1;
            Root.style.display = DisplayStyle.None;
        }
    }
