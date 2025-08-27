using System;
using UnityEngine;

namespace App
{
    public sealed class ScreenController : MonoBehaviour
    {
        [SerializeField] private GameObject _mainMenuPanel;
        [SerializeField] private GameObject _gamePanel;
        [SerializeField] private GameObject _winPanel;
        
        public event Action<ScreenId> OnScreenShown;

        public void Show(ScreenId id)
        {
            _mainMenuPanel.SetActive(id == ScreenId.Main);
            _gamePanel.SetActive(id == ScreenId.Game);
            _winPanel.SetActive(id == ScreenId.Win);
            
            OnScreenShown?.Invoke(id);
        }
        
        public T GetViewOnPanel<T>(ScreenId id) where T : Component
        {
            GameObject panel = id switch
            {
                ScreenId.Main => _mainMenuPanel,
                ScreenId.Game => _gamePanel,
                ScreenId.Win  => _winPanel,
                _             => null
            };

            if (panel == null)
            {
                return null;
            }

            return panel.GetComponentInChildren<T>(true);
        }
    }

    public enum ScreenId
    {
        Main,
        Game,
        Win
    }

}