using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.Game
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class ClusterDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private int _clusterId;

        private CanvasGroup _canvasGroup;
        private RectTransform _rectTransform;

        private Transform _originalParent;
        private int _originalSiblingIndex;

        private RectTransform _dragRootRect; // root canvas rect
        private Camera _uiCamera; // null для Overlay

        private Vector3 _worldGrabOffset;

        private bool _skipReturnToOriginalParentOnce;
        private bool _dropAcceptedOnce;

        private RectTransform _lettersContainer;
        private float _cellWidth;
        private float _cellSpacing;

        public int ClusterId => _clusterId;

        public event Action<int, Vector2> OnDragged;
        public event Action<int, Vector2> OnDragEnded;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _rectTransform = GetComponent<RectTransform>();
            _originalParent = transform.parent;
            _originalSiblingIndex = _rectTransform.GetSiblingIndex();

            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null && canvas.rootCanvas != null)
            {
                _dragRootRect = canvas.rootCanvas.GetComponent<RectTransform>();
                _uiCamera = canvas.rootCanvas.renderMode == RenderMode.ScreenSpaceCamera
                    ? canvas.rootCanvas.worldCamera
                    : null; // Overlay
            }
            else
            {
                _dragRootRect = _rectTransform.root as RectTransform;
                _uiCamera = null;
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.alpha = 0.6f;
            }

            // сохраняем текущий родитель и порядок
            _originalParent = transform.parent;
            _originalSiblingIndex = _rectTransform.GetSiblingIndex();

            // перевесить в drag-root, сохранив world-позицию (без скачка)
            _rectTransform.SetParent(_dragRootRect, worldPositionStays: true);

            // найдём точку под курсором в плоскости РАМКИ и посчитаем world-оффсет
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                    _rectTransform, eventData.position, _uiCamera, out var worldOnRect))
            {
                _worldGrabOffset = _rectTransform.position - worldOnRect;
            }
            else
            {
                _worldGrabOffset = Vector3.zero;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_rectTransform == null) return;

            // возьмём world-точку под курсором в плоскости drag-root
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                    _dragRootRect, eventData.position, _uiCamera, out var worldOnRoot))
            {
                // новая позиция = точка под курсором + оффсет хвата
                Vector3 newWorld = worldOnRoot + _worldGrabOffset;
                _rectTransform.position = newWorld;

                OnDragged?.Invoke(_clusterId, _rectTransform.anchoredPosition);
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

            // дропа не было — вернуть в список
            if (_originalParent != null)
            {
                _rectTransform.SetParent(_originalParent, worldPositionStays: false);
                _rectTransform.SetSiblingIndex(_originalSiblingIndex);
                _rectTransform.anchoredPosition = Vector2.zero;
            }

            OnDragEnded?.Invoke(_clusterId, eventData.position);
        }

        // ====== Публичный API (как было) ======

        public void SetClusterId(int clusterId) => _clusterId = clusterId;

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

        public void SetLettersContainer(RectTransform lettersContainer) => _lettersContainer = lettersContainer;

        public void SetCellGeometry(float cellWidth, float cellSpacing)
        {
            _cellWidth = cellWidth;
            _cellSpacing = cellSpacing;
        }

        public int GetGrabbedLetterIndex(PointerEventData eventData)
        {
            if (_lettersContainer == null) return 0;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _lettersContainer, eventData.position, _uiCamera, out var local))
                return 0;

            float leftEdge = -_lettersContainer.rect.width * _lettersContainer.pivot.x;

            var hlg = _lettersContainer.GetComponent<HorizontalLayoutGroup>();
            int padLeft = hlg != null ? hlg.padding.left : 0;

            float xFromFirst = (local.x - leftEdge) - padLeft;
            float step = _cellWidth + _cellSpacing;

            int index = Mathf.RoundToInt(xFromFirst / step);
            int lettersCount = _lettersContainer.childCount;
            return Mathf.Clamp(index, 0, Mathf.Max(0, lettersCount - 1));
        }
    }
}