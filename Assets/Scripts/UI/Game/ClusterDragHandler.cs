using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI.Game
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class ClusterDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private int _clusterId;
        private CanvasGroup _canvasGroup;
        private RectTransform _rectTransform;
        private Transform _originalParent;
        private Transform _rootCanvasTransform;
        private bool _skipReturnToOriginalParentOnce;
        private RectTransform _lettersContainer;
        private float _cellWidth;
        private float _cellSpacing;
        private bool _dropAcceptedOnce;

        public int ClusterId => _clusterId;

        public event Action<int, Vector2> OnDragged;
        public event Action<int, Vector2> OnDragEnded;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _rectTransform = GetComponent<RectTransform>();
            _originalParent = transform.parent;

            var canvas = GetComponentInParent<Canvas>();
            _rootCanvasTransform = canvas != null && canvas.rootCanvas != null
                ? canvas.rootCanvas.transform
                : _originalParent;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.alpha = 0.6f;
            }

            if (_rootCanvasTransform != null)
            {
                transform.SetParent(_rootCanvasTransform);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_rectTransform != null)
            {
                _rectTransform.anchoredPosition += eventData.delta;
            }

            if (OnDragged != null)
            {
                OnDragged.Invoke(_clusterId, _rectTransform != null ? _rectTransform.anchoredPosition : Vector2.zero);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.blocksRaycasts = true;
                _canvasGroup.alpha = 1f;
            }

            if (_skipReturnToOriginalParentOnce)
            {
                _skipReturnToOriginalParentOnce = false;
                OnDragEnded?.Invoke(_clusterId, eventData.position);
                return;
            }

            if (_originalParent != null)
            {
                transform.SetParent(_originalParent);
            }

            if (_rectTransform != null)
            {
                _rectTransform.anchoredPosition = Vector2.zero;
            }

            OnDragEnded?.Invoke(_clusterId, eventData.position);
        }

        public void SetClusterId(int clusterId)
        {
            _clusterId = clusterId;
        }
        
        public void MarkDropAcceptedOnce()
        {
            _dropAcceptedOnce = true;
            _skipReturnToOriginalParentOnce = true;
        }
        
        public bool ConsumeDropAcceptedFlag()
        {
            bool v = _dropAcceptedOnce;
            _dropAcceptedOnce = false;
            return v;
        }
        
        public void SetLettersContainer(RectTransform lettersContainer)
        {
            _lettersContainer = lettersContainer;
        }
        
        public void SetCellGeometry(float cellWidth, float cellSpacing)
        {
            _cellWidth = cellWidth;
            _cellSpacing = cellSpacing;
        }
        
        public int GetGrabbedLetterIndex(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (_lettersContainer == null)
            {
                return 0;
            }

            // экран → локальные координаты контейнера букв
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _lettersContainer, eventData.position, eventData.pressEventCamera, out var local))
            {
                return 0;
            }

            // учитываем pivot контейнера
            float leftEdge = -_lettersContainer.rect.width * _lettersContainer.pivot.x;

            // учитываем padding HorizontalLayoutGroup
            var hlg = _lettersContainer.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            int padLeft = hlg != null ? hlg.padding.left : 0;

            // x-позиция относительно левого края первой буквы
            float xFromFirst = (local.x - leftEdge) - padLeft;

            float step = _cellWidth + _cellSpacing;
            int index = Mathf.RoundToInt(xFromFirst / step);
            int lettersCount = _lettersContainer.childCount;

            return Mathf.Clamp(index, 0, Mathf.Max(0, lettersCount - 1));
        }
    }
}