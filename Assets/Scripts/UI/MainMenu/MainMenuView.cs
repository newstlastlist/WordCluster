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

        private MainMenuPresenter _presenter;

        private void Awake()
        {
            if (_playButton != null)
            {
                _playButton.onClick.AddListener(OnPlayClicked);
            }
        }

        private void OnEnable()
        {
            _presenter = new MainMenuPresenter(this);
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
            if (_playButton != null)
            {
                _playButton.onClick.RemoveListener(OnPlayClicked);
            }
        }

        public void SetTitle(string text)
        {
            if (_title != null)
            {
                _title.text = text;
            }
        }

        public void SetProgressText(string text)
        {
            if (_progressText != null)
            {
                _progressText.text = text;
            }
        }

        private void OnPlayClicked()
        {
            if (_presenter != null)
            {
                _presenter.StartGame();
            }
        }
    }
}