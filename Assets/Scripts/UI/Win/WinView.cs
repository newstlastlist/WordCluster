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

        public event Action OnMainMenuClicked;
        public event Action OnNextLevelClicked;

        private void Awake()
        {
            if (_mainMenuButton != null)
            {
                _mainMenuButton.onClick.AddListener(OnMainMenuClickedInternalHandler);
            }

            if (_nextLevelButton != null)
            {
                _nextLevelButton.onClick.AddListener(OnNextLevelClickedInternalHandler);
            }
        }

        private void OnDestroy()
        {
            if (_mainMenuButton != null)
            {
                _mainMenuButton.onClick.RemoveListener(OnMainMenuClickedInternalHandler);
            }

            if (_nextLevelButton != null)
            {
                _nextLevelButton.onClick.RemoveListener(OnNextLevelClickedInternalHandler);
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

        private void OnMainMenuClickedInternalHandler()
        {
            OnMainMenuClicked?.Invoke();
        }

        private void OnNextLevelClickedInternalHandler()
        {
            OnNextLevelClicked?.Invoke();
        }
    }
}