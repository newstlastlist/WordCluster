using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Game
{
    public sealed class GameView : MonoBehaviour
    {
        [Header("Header")]
        [SerializeField] private TMP_Text _headerText;

        [Header("Grid")]
        [SerializeField] private RectTransform _gridContainer;
        [SerializeField] private GridLayoutGroup _gridLayout;
        [SerializeField] private GameObject _cellButtonPrefab;

        [Header("Clusters")]
        [SerializeField] private RectTransform _clustersContent;
        [SerializeField] private GameObject _clusterItemPrefab;
        
        [Header("DEBUG")]
        [SerializeField] private Button _debugButton;

        private readonly Dictionary<(int row, int col), (Button button, TMP_Text label, CellDropHandler drop)> _cellMap =
            new Dictionary<(int, int), (Button, TMP_Text, CellDropHandler)>();

        private readonly Dictionary<int, (Button button, TMP_Text label, ClusterDragHandler drag)> _clusterMap =
            new Dictionary<int, (Button, TMP_Text, ClusterDragHandler)>();
        
        // для корректной отписки
        private readonly Dictionary<int, Action<int, Vector2>> _draggedHandlers =
            new Dictionary<int, Action<int, Vector2>>();
        private readonly Dictionary<int, Action<int, Vector2>> _dragEndedHandlers =
            new Dictionary<int, Action<int, Vector2>>();

        public int RowsCount { get; private set; }
        public int WordLength { get; private set; }

        public event Action<int, int> OnCellClicked; // row, col
        public event Action<int> OnClusterClicked; // clusterId
        public event Action<int, int, int> OnClusterDropped; // clusterId, row, col
        public event Action<int> OnClusterDragStarted; // clusterId
        public event Action<int> OnClusterDragEnded; // clusterId
        public event Action OnDebugWinClicked;

        private void Awake()
        {
            _debugButton.onClick.AddListener(() => OnDebugWinClicked?.Invoke());
        }

        private void OnDestroy()
        {
            _debugButton.onClick.RemoveAllListeners();
        }

        public void SetHeader(string text)
        {
            if (_headerText != null)
            {
                _headerText.text = text;
            }
        }

        public void BuildGrid(int rowsCount, int wordLength)
        {
            RowsCount = Mathf.Max(0, rowsCount);
            WordLength = Mathf.Max(0, wordLength);

            ClearGridInternal();

            if (_gridContainer == null || _cellButtonPrefab == null || _gridLayout == null)
            {
                Debug.LogWarning("[GameView] Grid references are not assigned.");
                return;
            }

            _gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _gridLayout.constraintCount = WordLength;

            for (int row = 0; row < RowsCount; row++)
            {
                for (int col = 0; col < WordLength; col++)
                {
                    GameObject go = Instantiate(_cellButtonPrefab, _gridContainer);
                    if (go == null) continue;

                    Button btn = go.GetComponent<Button>();
                    TMP_Text label = go.GetComponentInChildren<TMP_Text>(true);
                    CellDropHandler dropHandler = go.GetComponent<CellDropHandler>();

                    if (label != null) label.text = string.Empty;
                    if (btn != null)
                    {
                        int capturedRow = row;
                        int capturedCol = col;
                        btn.onClick.AddListener(() => OnCellButtonClickedHandler(capturedRow, capturedCol));
                    }

                    if (dropHandler != null)
                    {
                        dropHandler.SetCoordinates(row, col);
                        dropHandler.OnClusterDropped += OnOnClusterDroppedInternalHandler;
                    }

                    _cellMap[(row, col)] = (btn, label, dropHandler);
                }
            }
        }

        public void RenderClusters(IReadOnlyDictionary<int, string> clustersById)
        {
            ClearClustersInternal();

            if (_clustersContent == null || _clusterItemPrefab == null)
            {
                Debug.LogWarning("[GameView] Clusters references are not assigned.");
                return;
            }

            foreach (var kvp in clustersById)
            {
                int clusterId = kvp.Key;
                string clusterText = kvp.Value ?? string.Empty;

                GameObject go = Instantiate(_clusterItemPrefab, _clustersContent);
                if (go == null) continue;

                Button btn = go.GetComponent<Button>();
                TMP_Text label = go.GetComponentInChildren<TMP_Text>(true);
                ClusterDragHandler dragHandler = go.GetComponent<ClusterDragHandler>();

                if (label != null) label.text = clusterText;
                if (btn != null)
                {
                    int capturedId = clusterId;
                    btn.onClick.AddListener(() => OnClusterButtonClickedHandler(capturedId));
                }

                if (dragHandler != null)
                {
                    dragHandler.SetClusterId(clusterId);

                    // Создаём делегаты, сохраняем их, подписываемся:
                    Action<int, Vector2> onDragged = (id, _) =>
                    {
                        if (OnClusterDragStarted != null)
                        {
                            OnClusterDragStarted.Invoke(id);
                        }
                    };

                    Action<int, Vector2> onDragEnded = (id, _) =>
                    {
                        if (OnClusterDragEnded != null)
                        {
                            OnClusterDragEnded.Invoke(id);
                        }
                    };

                    _draggedHandlers[clusterId] = onDragged;
                    _dragEndedHandlers[clusterId] = onDragEnded;

                    dragHandler.OnDragged += onDragged;
                    dragHandler.OnDragEnded += onDragEnded;
                }

                _clusterMap[clusterId] = (btn, label, dragHandler);
            }
        }

        public void SetCellChar(int rowIndex, int colIndex, char? ch)
        {
            if (_cellMap.TryGetValue((rowIndex, colIndex), out var tuple))
            {
                if (tuple.label != null)
                {
                    tuple.label.text = ch.HasValue ? ch.Value.ToString() : string.Empty;
                }
            }
        }

        public void ClearAllCells()
        {
            foreach (var pair in _cellMap)
            {
                TMP_Text label = pair.Value.label;
                if (label != null) label.text = string.Empty;
            }
        }

        public void SetClusterInteractable(int clusterId, bool interactable)
        {
            if (_clusterMap.TryGetValue(clusterId, out var tuple) && tuple.button != null)
            {
                tuple.button.interactable = interactable;
            }
        }

        public void SetClusterText(int clusterId, string text)
        {
            if (_clusterMap.TryGetValue(clusterId, out var tuple) && tuple.label != null)
            {
                tuple.label.text = text ?? string.Empty;
            }
        }

        private void OnCellButtonClickedHandler(int row, int col)
        {
            OnCellClicked?.Invoke(row, col);
        }

        private void OnClusterButtonClickedHandler(int clusterId)
        {
            OnClusterClicked?.Invoke(clusterId);
        }

        private void OnOnClusterDroppedInternalHandler(int clusterId, int row, int col)
        {
            OnClusterDropped?.Invoke(clusterId, row, col);
        }

        private void ClearGridInternal()
        {
            foreach (var pair in _cellMap)
            {
                Button btn = pair.Value.button;
                if (btn != null) btn.onClick.RemoveAllListeners();

                if (pair.Value.drop != null)
                    pair.Value.drop.OnClusterDropped -= OnOnClusterDroppedInternalHandler;
            }

            _cellMap.Clear();

            if (_gridContainer != null)
            {
                for (int i = _gridContainer.childCount - 1; i >= 0; i--)
                {
                    Destroy(_gridContainer.GetChild(i).gameObject);
                }
            }
        }

        private void ClearClustersInternal()
        {
            foreach (var pair in _clusterMap)
            {
                Button btn = pair.Value.button;
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                }

                ClusterDragHandler drag = pair.Value.drag;
                int clusterId = pair.Key;
                if (drag != null)
                {
                    if (_draggedHandlers.TryGetValue(clusterId, out var onDragged))
                    {
                        drag.OnDragged -= onDragged;
                    }

                    if (_dragEndedHandlers.TryGetValue(clusterId, out var onDragEnded))
                    {
                        drag.OnDragEnded -= onDragEnded;
                    }
                }
            }

            _clusterMap.Clear();

            if (_clustersContent != null)
            {
                for (int i = _clustersContent.childCount - 1; i >= 0; i--)
                {
                    Destroy(_clustersContent.GetChild(i).gameObject);
                }
            }
        }
    }
}