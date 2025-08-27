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

        private WinPresenter _presenter;

        private void Awake()
        {
            if (_mainMenuButton != null)
            {
                _mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            }

            if (_nextLevelButton != null)
            {
                _nextLevelButton.onClick.AddListener(OnNextLevelClicked);
            }
        }

        private void OnEnable()
        {
            _presenter = new WinPresenter(this);
            _presenter.OnOpen();
        }

        private void OnDisable()
        {
            if (_presenter != null)
            {
                _presenter.OnClose();
                _presenter = null;
            }
        }

        private void OnDestroy()
        {
            if (_mainMenuButton != null)
            {
                _mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
            }

            if (_nextLevelButton != null)
            {
                _nextLevelButton.onClick.RemoveListener(OnNextLevelClicked);
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

        private void OnMainMenuClicked()
        {
            if (_presenter != null)
            {
                _presenter.GoToMainMenu();
            }
        }

        private void OnNextLevelClicked()
        {
            if (_presenter != null)
            {
                _presenter.GoToNextLevel();
            }
        }
    }
}