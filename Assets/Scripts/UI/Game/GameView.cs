using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Game
{
    public sealed class GameView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _header;
        [SerializeField] private Button _debugWinButton;

        private GamePresenter _presenter;

        private void Awake()
        {
            if (_debugWinButton != null)
            {
                _debugWinButton.onClick.AddListener(OnDebugWinClicked);
            }
        }

        private void OnEnable()
        {
            _presenter = new GamePresenter(this);
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
            if (_debugWinButton != null)
            {
                _debugWinButton.onClick.RemoveListener(OnDebugWinClicked);
            }
        }

        public void SetHeader(string text)
        {
            if (_header != null)
            {
                _header.text = text;
            }
        }

        private void OnDebugWinClicked()
        {
            if (_presenter != null)
            {
                _presenter.DebugWin();
            }
        }
    }
}