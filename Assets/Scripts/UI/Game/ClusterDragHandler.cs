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

            if (_originalParent != null)
            {
                transform.SetParent(_originalParent);
            }

            if (_rectTransform != null)
            {
                _rectTransform.anchoredPosition = Vector2.zero;
            }

            if (OnDragEnded != null)
            {
                OnDragEnded.Invoke(_clusterId, eventData.position);
            }
        }

        public void SetClusterId(int clusterId)
        {
            _clusterId = clusterId;
        }
    }
}