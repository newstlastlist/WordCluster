using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.MainMenu
{
    public sealed class MainMenuView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _progressText;
        [SerializeField] private Button _playButton;
        
        public event Action OnPlayClicked;

        private void Awake()
        {
            if (_playButton != null)
            {
                _playButton.onClick.AddListener(OnPlayClickedHandler);
            }
        }

        private void OnDestroy()
        {
            if (_playButton != null)
            {
                _playButton.onClick.RemoveListener(OnPlayClickedHandler);
            }
        }

        public void SetProgressText(string text)
        {
            if (_progressText != null)
            {
                _progressText.text = text;
            }
        }

        public void SetTitle(string text)
        {
            if (_title != null)
            {
                _title.text = text;
            }
        }

        private void OnPlayClickedHandler()
        {
            OnPlayClicked?.Invoke();
        }
    }
}