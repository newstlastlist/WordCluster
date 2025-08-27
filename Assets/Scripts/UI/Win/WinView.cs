using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Win
{
    public sealed class WinView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _resultsTitle;
        [SerializeField] private RectTransform _wordsContainer;
        [SerializeField] private Button _mainMenuButton;
        [SerializeField] private Button _nextLevelButton;

        public event Action MainMenuClicked;
        public event Action NextLevelClicked;

        private void Awake()
        {
            if (_mainMenuButton != null)
            {
                _mainMenuButton.onClick.AddListener(OnMainMenuClickedInternal);
            }

            if (_nextLevelButton != null)
            {
                _nextLevelButton.onClick.AddListener(OnNextLevelClickedInternal);
            }
        }

        private void OnDestroy()
        {
            if (_mainMenuButton != null)
            {
                _mainMenuButton.onClick.RemoveListener(OnMainMenuClickedInternal);
            }

            if (_nextLevelButton != null)
            {
                _nextLevelButton.onClick.RemoveListener(OnNextLevelClickedInternal);
            }
        }

        public void SetResultsTitle(string text)
        {
            if (_resultsTitle != null)
            {
                _resultsTitle.text = text;
            }
        }

        public RectTransform GetWordsContainer()
        {
            return _wordsContainer;
        }

        private void OnMainMenuClickedInternal()
        {
            MainMenuClicked?.Invoke();
        }

        private void OnNextLevelClickedInternal()
        {
            NextLevelClicked?.Invoke();
        }
    }
}