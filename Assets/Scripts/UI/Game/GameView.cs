using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Game
{
    public sealed class GameView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _header;
        [SerializeField] private Button _debugWinButton;

        public event Action DebugWinClicked;

        private void Awake()
        {
            if (_debugWinButton != null)
            {
                _debugWinButton.onClick.AddListener(OnDebugWinClickedInternal);
            }
        }

        private void OnDestroy()
        {
            if (_debugWinButton != null)
            {
                _debugWinButton.onClick.RemoveListener(OnDebugWinClickedInternal);
            }
        }

        public void SetHeader(string text)
        {
            if (_header != null)
            {
                _header.text = text;
            }
        }

        private void OnDebugWinClickedInternal()
        {
            DebugWinClicked?.Invoke();
        }
    }
}