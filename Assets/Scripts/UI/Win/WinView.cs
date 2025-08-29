using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Win
{
    public sealed class WinView : MonoBehaviour
    {
        [Header("Header")]
        [SerializeField] private TMP_Text _resultTitleText;

        [Header("Words")]
        [SerializeField] private RectTransform _wordsContainer;
        [SerializeField] private GameObject _wordItemPrefab;

        [Header("Buttons")]
        [SerializeField] private Button _mainMenuButton;
        [SerializeField] private Button _nextButton;

        public event Action OnMainMenuClicked;
        public event Action OnNextLevelClicked;

        private void Awake()
        {
            if (_mainMenuButton != null)
            {
                _mainMenuButton.onClick.AddListener(OnMainMenuClickedHandler);
            }

            if (_nextButton != null)
            {
                _nextButton.onClick.AddListener(OnNextLevelClickedHandler);
            }
        }

        private void OnDestroy()
        {
            if (_mainMenuButton != null)
            {
                _mainMenuButton.onClick.RemoveListener(OnMainMenuClickedHandler);
            }

            if (_nextButton != null)
            {
                _nextButton.onClick.RemoveListener(OnNextLevelClickedHandler);
            }
        }

        public void SetResultsTitle(string text)
        {
            if (_resultTitleText != null)
            {
                _resultTitleText.text = text ?? string.Empty;
            }
        }

        public void RenderWords(IReadOnlyList<string> wordsInOrder)
        {
            ClearWordsInternal();

            if (_wordsContainer == null || _wordItemPrefab == null || wordsInOrder == null)
            {
                return;
            }

            for (int i = 0; i < wordsInOrder.Count; i++)
            {
                string word = wordsInOrder[i] ?? string.Empty;

                GameObject go = Instantiate(_wordItemPrefab, _wordsContainer);
                if (go == null)
                {
                    continue;
                }

                TMP_Text label = go.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                {
                    label.text = word;
                }
            }
        }

        private void OnMainMenuClickedHandler()
        {
            OnMainMenuClicked?.Invoke();
        }

        private void OnNextLevelClickedHandler()
        {
            OnNextLevelClicked?.Invoke();
        }

        private void ClearWordsInternal()
        {
            if (_wordsContainer == null)
            {
                return;
            }

            for (int i = _wordsContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_wordsContainer.GetChild(i).gameObject);
            }
        }
    }
}