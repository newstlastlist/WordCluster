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
        
        public event Action PlayClicked;

        private void Awake()
        {
            if (_playButton != null)
            {
                _playButton.onClick.AddListener(OnPlayClicked);
            }
        }

        private void OnDestroy()
        {
            if (_playButton != null)
            {
                _playButton.onClick.RemoveListener(OnPlayClicked);
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

        private void OnPlayClicked()
        {
            PlayClicked?.Invoke();
        }
    }
}