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
        [SerializeField] private RectTransform _clustersContent; // ScrollView -> Viewport -> Content
        [SerializeField] private GameObject _clusterItemPrefab;
        
        [Header("Debug")]
        [SerializeField] private Button _debugButton;

        private readonly Dictionary<(int rowIndex, int colIndex), (Button button, TMP_Text label)> _cellMap =
            new Dictionary<(int, int), (Button, TMP_Text)>();

        private readonly Dictionary<int, (Button button, TMP_Text label)> _clusterMap =
            new Dictionary<int, (Button, TMP_Text)>();

        public int RowsCount { get; private set; }
        public int WordLength { get; private set; }
        
        public event Action<int, int> CellClicked;
        public event Action<int> ClusterClicked;
        public event Action DebugWinClicked;

        private void Awake()
        {
            _debugButton.onClick.AddListener(() => DebugWinClicked?.Invoke());
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
                    if (go == null)
                    {
                        continue;
                    }

                    Button btn = go.GetComponent<Button>();
                    TMP_Text label = go.GetComponentInChildren<TMP_Text>(true);

                    if (label != null)
                    {
                        label.text = string.Empty;
                    }

                    if (btn != null)
                    {
                        int capturedRow = row;
                        int capturedCol = col;
                        btn.onClick.AddListener(() => OnCellButtonClicked(capturedRow, capturedCol));
                    }

                    _cellMap[(row, col)] = (btn, label);
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

            if (clustersById == null || clustersById.Count == 0)
            {
                return;
            }

            foreach (var kvp in clustersById)
            {
                int clusterId = kvp.Key;
                string clusterText = kvp.Value ?? string.Empty;

                GameObject go = Instantiate(_clusterItemPrefab, _clustersContent);
                if (go == null)
                {
                    continue;
                }

                Button btn = go.GetComponent<Button>();
                TMP_Text label = go.GetComponentInChildren<TMP_Text>(true);

                if (label != null)
                {
                    label.text = clusterText;
                }

                if (btn != null)
                {
                    int capturedId = clusterId;
                    btn.onClick.AddListener(() => OnClusterButtonClicked(capturedId));
                }

                _clusterMap[clusterId] = (btn, label);
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
                if (label != null)
                {
                    label.text = string.Empty;
                }
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

        private void OnCellButtonClicked(int rowIndex, int colIndex)
        {
            CellClicked?.Invoke(rowIndex, colIndex);
        }

        private void OnClusterButtonClicked(int clusterId)
        {
            ClusterClicked?.Invoke(clusterId);
        }

        private void ClearGridInternal()
        {
            foreach (var pair in _cellMap)
            {
                Button btn = pair.Value.button;
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                }
            }

            _cellMap.Clear();

            if (_gridContainer != null)
            {
                for (int i = _gridContainer.childCount - 1; i >= 0; i--)
                {
                    Transform child = _gridContainer.GetChild(i);
                    Destroy(child.gameObject);
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
            }

            _clusterMap.Clear();

            if (_clustersContent != null)
            {
                for (int i = _clustersContent.childCount - 1; i >= 0; i--)
                {
                    Transform child = _clustersContent.GetChild(i);
                    Destroy(child.gameObject);
                }
            }
        }
    }
}