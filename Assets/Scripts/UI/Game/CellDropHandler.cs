using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI.Game
{
    public sealed class CellDropHandler : MonoBehaviour, IDropHandler
    {
        private int _rowIndex;
        private int _colIndex;

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

            int clusterId = clusterHandler.ClusterId;

            if (OnClusterDropped != null)
            {
                OnClusterDropped.Invoke(clusterId, _rowIndex, _colIndex);
            }
        }

        public void SetCoordinates(int rowIndex, int colIndex)
        {
            _rowIndex = rowIndex;
            _colIndex = colIndex;
        }
    }
}