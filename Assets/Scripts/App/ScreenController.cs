using UnityEngine;

namespace App
{
    public sealed class ScreenController : MonoBehaviour
    {
        [SerializeField] private GameObject _mainMenuPanel;
        [SerializeField] private GameObject _gamePanel;
        [SerializeField] private GameObject _winPanel;

        public void Show(ScreenId id)
        {
            _mainMenuPanel.SetActive(id == ScreenId.Main);
            _gamePanel.SetActive(id == ScreenId.Game);
            _winPanel.SetActive(id == ScreenId.Win);
        }
    }

    public enum ScreenId
    {
        Main,
        Game,
        Win
    }

}