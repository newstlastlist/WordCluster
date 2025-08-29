using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI.Game
{
    public sealed class CellDropHandler : MonoBehaviour, IDropHandler
    {
        private int _rowIndex;
        private int _colIndex;
        private int _wordLength;

        public int RowIndex => _rowIndex;
        public int ColIndex => _colIndex;

        public event Action<int, int, int> OnClusterDropped;

        public void OnDrop(PointerEventData eventData)
        {
            var clusterHandler = eventData.pointerDrag != null
                ? eventData.pointerDrag.GetComponent<ClusterDragHandler>()
                : null;

            if (clusterHandler == null)
            {
                return;
            }

            clusterHandler.MarkDropAcceptedOnce();

            // индекс буквы, за которую тянут
            int grabbedIndex = clusterHandler.GetGrabbedLetterIndex(eventData);

            // длина кластера
            int clusterLen = clusterHandler.transform
                .GetComponentInChildren<RectTransform>(true)
                ? clusterHandler.transform.GetComponentInChildren<RectTransform>(true).childCount
                : 1;

            // стартовая колонка с учётом смещения
            int startCol = _colIndex - grabbedIndex;

            // клэмп по границам слова
            int maxStart = Mathf.Max(0, _wordLength - clusterLen);
            startCol = Mathf.Clamp(startCol, 0, maxStart);

            OnClusterDropped?.Invoke(clusterHandler.ClusterId, _rowIndex, startCol);
        }

        public void SetCoordinates(int rowIndex, int colIndex)
        {
            _rowIndex = rowIndex;
            _colIndex = colIndex;
        }
        
        public void SetWordLength(int wordLength)
        {
            _wordLength = Mathf.Max(1, wordLength);
        }
    }
}