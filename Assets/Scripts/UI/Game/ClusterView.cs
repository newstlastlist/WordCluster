using UnityEngine;
using UnityEngine.UI;

namespace UI.Game
{
    public class ClusterView : MonoBehaviour
    {
        private const float _alpha = 0.5f;
        
        [SerializeField] private Image _image;
        [SerializeField] private Color _defaultColor;
        [SerializeField] private Color _goodPlaceColor;
        [SerializeField] private Color _badPlaceColor;

        public void ChangeFrameColor(FrameState state)
        {
            if (_image == null)
            {
                return;
            }

            switch (state)
            {
                case FrameState.Good:
                    _image.color = new Color(_goodPlaceColor.r, _goodPlaceColor.g, _goodPlaceColor.b, 1);
                    break;
                case FrameState.Bad:
                    _image.color = new Color(_badPlaceColor.r, _badPlaceColor.g, _badPlaceColor.b, 1);
                    break;
                default:
                    _image.color = new Color(_defaultColor.r, _defaultColor.g, _defaultColor.b, _alpha);
                    break;
            }
        }
        
        public enum FrameState
        {
            Default = 0,
            Good = 1,
            Bad = 2
        }
    }
}