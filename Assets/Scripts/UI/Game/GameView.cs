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
        [SerializeField] private Vector2 _cellSize;
        [SerializeField] private Vector2 _cellSpacing;
        [SerializeField] private Vector2 _gridPadding;

        [Header("Clusters")]
        [SerializeField] private RectTransform _clustersContent;
        [SerializeField] private List<ClusterPrefabRule> _clusterPrefabs;
        [SerializeField] private GameObject _letterItemPrefab;
        [SerializeField] private RectTransform _boardOverlay;

        [Header("DEBUG")]
        [SerializeField] private Button _debugButton;

        // Grid: (row,col) -> (button,label,dropHandler)
        private readonly Dictionary<(int row, int col), (Button button, TMP_Text label, CellDropHandler drop)> _cellMap =
            new Dictionary<(int, int), (Button, TMP_Text, CellDropHandler)>();

        // Clusters (in list): clusterId -> (button,label(not used),dragHandler)
        private readonly Dictionary<int, (Button button, TMP_Text label, ClusterDragHandler drag)> _clusterMap =
            new Dictionary<int, (Button, TMP_Text, ClusterDragHandler)>();

        // DnD delegates cache for proper unsubscribe
        private readonly Dictionary<int, Action<int, Vector2>> _draggedHandlers =
            new Dictionary<int, Action<int, Vector2>>();
        private readonly Dictionary<int, Action<int, Vector2>> _dragEndedHandlers =
            new Dictionary<int, Action<int, Vector2>>();

        // Frames on screen: clusterId -> (frameRect, lettersContainerRect)
        private readonly Dictionary<int, (RectTransform frame, RectTransform lettersContainer)> _clusterFrames =
            new Dictionary<int, (RectTransform, RectTransform)>();

        // Cluster location state: true = on board, false = in list
        private readonly Dictionary<int, bool> _clusterOnBoard =
            new Dictionary<int, bool>();

        public int RowsCount { get; private set; }
        public int WordLength { get; private set; }

        public event Action<int, int> OnCellClicked;                  // row, col
        public event Action<int> OnClusterClicked;                    // clusterId
        public event Action<int, int, int> OnClusterDropped;          // clusterId, row, col
        public event Action<int> OnClusterDragStarted;                // clusterId
        public event Action<int> OnClusterDragEnded;                  // clusterId
        public event Action OnDebugWinClicked;

        private void Awake()
        {
            if (_debugButton != null)
            {
                _debugButton.onClick.AddListener(OnDebugWinClickedHandler);
            }
        }

        private void OnDestroy()
        {
            if (_debugButton != null)
            {
                _debugButton.onClick.RemoveListener(OnDebugWinClickedHandler);
            }
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
                    CellDropHandler dropHandler = go.GetComponent<CellDropHandler>();

                    if (label != null)
                    {
                        label.text = string.Empty;
                    }

                    if (btn != null)
                    {
                        int capturedRow = row;
                        int capturedCol = col;
                        btn.onClick.AddListener(() => OnCellButtonClickedHandler(capturedRow, capturedCol));
                    }

                    if (dropHandler != null)
                    {
                        dropHandler.SetCoordinates(row, col);
                        dropHandler.SetWordLength(WordLength);
                        dropHandler.OnClusterDropped += OnClusterDroppedInternalHandler;
                    }

                    _cellMap[(row, col)] = (btn, label, dropHandler);
                }
            }
        }

        public void RenderClusters(IReadOnlyDictionary<int, string> clustersById)
        {
            ClearClustersInternal();

            if (_clustersContent == null || _letterItemPrefab == null)
            {
                Debug.LogWarning("[GameView] Cluster content/letter prefab are not assigned.");
                return;
            }

            foreach (var kvp in clustersById)
            {
                int clusterId = kvp.Key;
                string clusterText = kvp.Value ?? string.Empty;

                GameObject framePrefab = GetClusterPrefabForLengthHandler(clusterText.Length);
                if (framePrefab == null)
                {
                    Debug.LogError($"[GameView] Cluster frame prefab not found for len={clusterText.Length}. " +
                                   $"ClusterId={clusterId}, text='{clusterText}'");
                    continue;
                }

                GameObject frameGO = Instantiate(framePrefab, _clustersContent);
                if (frameGO == null)
                {
                    continue;
                }

                RectTransform frameRect = frameGO.GetComponent<RectTransform>();
                if (frameRect == null)
                {
                    Debug.LogError("[GameView] Cluster frame prefab has no RectTransform.");
                    Destroy(frameGO);
                    continue;
                }

                RectTransform lettersContainer = frameGO.transform.Find("LettersContainer") as RectTransform;
                if (lettersContainer == null)
                {
                    Debug.LogError("[GameView] Cluster frame is missing 'LettersContainer' child.");
                    Destroy(frameGO);
                    continue;
                }

                BuildLettersForClusterHandler(lettersContainer, clusterText);

                // Клики и DnD
                Button btn = frameGO.GetComponent<Button>();
                if (btn != null)
                {
                    int capturedId = clusterId;
                    btn.onClick.AddListener(() => OnClusterButtonClickedHandler(capturedId));
                }

                ClusterDragHandler drag = frameGO.GetComponent<ClusterDragHandler>();
                if (drag != null)
                {
                    drag.SetClusterId(clusterId);
                    drag.SetLettersContainer(lettersContainer);
                    drag.SetCellGeometry(_cellSize.x, _cellSpacing.x);

                    Action<int, Vector2> onDragged = (id, _) =>
                    {
                        OnClusterDragStarted?.Invoke(id);
                    };

                    Action<int, Vector2> onDragEnded = (id, _) =>
                    {
                        OnClusterDragEnded?.Invoke(id);
                    };

                    _draggedHandlers[clusterId] = onDragged;
                    _dragEndedHandlers[clusterId] = onDragEnded;

                    drag.OnDragged += onDragged;
                    drag.OnDragEnded += onDragEnded;
                }

                // Кэшируем
                _clusterMap[clusterId] = (btn, null, drag);
                _clusterFrames[clusterId] = (frameRect, lettersContainer);
                _clusterOnBoard[clusterId] = false;
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

        public void AttachClusterToBoard(int clusterId, int rowIndex, int startColumn)
        {
            if (!_clusterFrames.TryGetValue(clusterId, out var tuple) || _boardOverlay == null)
            {
                return;
            }

            tuple.frame.SetParent(_boardOverlay, worldPositionStays: false);
            
            var cg = tuple.frame.GetComponent<CanvasGroup>();
            if (cg == null)
            {
                cg = tuple.frame.gameObject.GetComponent<CanvasGroup>();
            }
            if (cg != null)
            {
                cg.alpha = 1f;
            }

            // гарантируем опору Top-Left у рамки (если префабы не правили)
            tuple.frame.anchorMin = new Vector2(0f, 1f);
            tuple.frame.anchorMax = new Vector2(0f, 1f);
            tuple.frame.pivot     = new Vector2(0f, 1f);

            Vector2 pos = CalculateFramePosFromCellHandler(rowIndex, startColumn, tuple.lettersContainer);
            tuple.frame.anchorMin = new Vector2(0f, 1f);
            tuple.frame.anchorMax = new Vector2(0f, 1f);
            tuple.frame.pivot     = new Vector2(0f, 1f);
            tuple.frame.anchoredPosition = pos;

            _clusterOnBoard[clusterId] = true;
        }

        public void ReturnClusterToPool(int clusterId)
        {
            if (!_clusterFrames.TryGetValue(clusterId, out var tuple))
            {
                return;
            }

            tuple.frame.SetParent(_clustersContent, worldPositionStays: false);
            tuple.frame.anchoredPosition = Vector2.zero;

            _clusterOnBoard[clusterId] = false;
        }

        public void SetClusterText(int clusterId, string text)
        {
            if (!_clusterFrames.TryGetValue(clusterId, out var tuple))
            {
                return;
            }

            BuildLettersForClusterHandler(tuple.lettersContainer, text ?? string.Empty);
        }
        
        public void SetClusterLocked(int clusterId, bool locked)
        {
            if (_clusterMap.TryGetValue(clusterId, out var tuple))
            {
                if (tuple.drag != null)
                {
                    // выключаем сам drag
                    tuple.drag.enabled = !locked;

                    var cg = tuple.drag.GetComponent<CanvasGroup>();
                    if (cg != null)
                    {
                        cg.alpha = 1f;

                        cg.interactable = !locked;
                        cg.blocksRaycasts = true;
                    }
                }
            }
        }
        
        public void SetClusterFrameState(int clusterId, ClusterView.FrameState state)
        {
            if (_clusterMap.TryGetValue(clusterId, out var tuple))
            {
                var cv = tuple.button != null
                    ? tuple.button.GetComponent<ClusterView>()
                    : null;

                if (cv == null && tuple.drag != null)
                {
                    cv = tuple.drag.GetComponent<ClusterView>();
                }

                if (cv != null)
                {
                    cv.ChangeFrameColor(state);
                }
            }
        }
        
        public void ResetForNewLevel()
        {
            ClearGridInternal();
            ClearClustersInternal();
            ClearBoardOverlayInternal();
        }

        public void ClearAllVisuals()
        {
            ClearGridInternal();
            ClearClustersInternal();
            ClearBoardOverlayInternal();
        }
        
        public bool ConsumeDropAcceptedFlag(int clusterId)
        {
            if (_clusterMap.TryGetValue(clusterId, out var tuple) && tuple.drag != null)
            {
                return tuple.drag.ConsumeDropAcceptedFlag();
            }

            return false;
        }
        
        private void ClearBoardOverlayInternal()
        {
            // Удаляем все инстансы рамок, прикреплённых к полю
            foreach (var kvp in _clusterFrames)
            {
                var frame = kvp.Value.frame;
                if (frame != null)
                {
                    Destroy(frame.gameObject);
                }
            }
            _clusterFrames.Clear();
            _clusterOnBoard.Clear();
        }

        private void OnDebugWinClickedHandler()
        {
            OnDebugWinClicked?.Invoke();
        }

        private void OnCellButtonClickedHandler(int row, int col)
        {
            OnCellClicked?.Invoke(row, col);
        }

        private void OnClusterButtonClickedHandler(int clusterId)
        {
            OnClusterClicked?.Invoke(clusterId);
        }

        private void OnClusterDroppedInternalHandler(int clusterId, int row, int col)
        {
            OnClusterDropped?.Invoke(clusterId, row, col);
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

                CellDropHandler drop = pair.Value.drop;
                if (drop != null)
                {
                    drop.OnClusterDropped -= OnClusterDroppedInternalHandler;
                }
            }

            _cellMap.Clear();

            if (_gridContainer != null)
            {
                for (int i = _gridContainer.childCount - 1; i >= 0; i--)
                {
                    var child = _gridContainer.GetChild(i);
                    if (child != null)
                    {
                        Destroy(child.gameObject);
                    }
                }
            }
        }

        private void ClearClustersInternal()
        {
            // 1) безопасная отписка
            foreach (var pair in _clusterMap)
            {
                var btn = pair.Value.button;
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                }

                var drag = pair.Value.drag;
                int id = pair.Key;
                if (drag != null)
                {
                    if (_draggedHandlers.TryGetValue(id, out var onDragged))
                    {
                        drag.OnDragged -= onDragged;
                    }
                    if (_dragEndedHandlers.TryGetValue(id, out var onDragEnded))
                    {
                        drag.OnDragEnded -= onDragEnded;
                    }
                }
            }

            _clusterMap.Clear();
            _draggedHandlers.Clear();
            _dragEndedHandlers.Clear();

            // 2) ЖЁСТКО удалить всех детей контента
            if (_clustersContent != null)
            {
                for (int i = _clustersContent.childCount - 1; i >= 0; i--)
                {
                    var child = _clustersContent.GetChild(i);
                    if (child != null)
                    {
                        Destroy(child.gameObject);
                    }
                }
            }
        }

        private void BuildLettersForClusterHandler(RectTransform lettersContainer, string clusterText)
        {
            if (lettersContainer == null || _letterItemPrefab == null)
            {
                return;
            }

            // Очистить старые буквы
            for (int i = lettersContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(lettersContainer.GetChild(i).gameObject);
            }

            // Создать новые
            for (int idx = 0; idx < clusterText.Length; idx++)
            {
                GameObject item = Instantiate(_letterItemPrefab, lettersContainer);
                if (item == null)
                {
                    continue;
                }

                // Буква
                TMP_Text txt = item.GetComponentInChildren<TMP_Text>(true);
                if (txt != null)
                {
                    txt.text = clusterText[idx].ToString();
                }

                // Подогнать размер под сетку (желательно, чтобы LetterItem имел LayoutElement)
                var layout = item.GetComponent<LayoutElement>();
                if (layout != null)
                {
                    layout.preferredWidth = _cellSize.x;
                    layout.preferredHeight = _cellSize.y;
                }
            }

            // Убедимся, что spacing совпадает с гридом
            var hlg = lettersContainer.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null)
            {
                hlg.spacing = _cellSpacing.x;
            }
        }

        private Vector2 CalculateFramePosFromCellHandler(int rowIndex, int startColumn, RectTransform lettersContainer)
        {
            // 1) берём RectTransform нужной ячейки
            if (!_cellMap.TryGetValue((rowIndex, startColumn), out var cellTuple) || cellTuple.button == null)
            {
                return Vector2.zero;
            }

            var cellRect = cellTuple.button.GetComponent<RectTransform>();
            if (cellRect == null || _boardOverlay == null)
            {
                return Vector2.zero;
            }

            // 2) получаем ВЕРХ-ЛЕВЫЙ угол ячейки в локальных координатах BoardOverlay
            Vector3[] corners = new Vector3[4];
            cellRect.GetWorldCorners(corners);              // 0:BL 1:TL 2:TR 3:BR
            Vector3 worldTL = corners[1];

            Vector2 localTL;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _boardOverlay,
                RectTransformUtility.WorldToScreenPoint(null, worldTL),
                null,
                out localTL
            );

            // 3) учтём padding у LettersContainer, чтобы первая буква попала точно в ячейку
            var hlg = lettersContainer != null ? lettersContainer.GetComponent<HorizontalLayoutGroup>() : null;
            int padLeft = hlg != null ? hlg.padding.left : 0;
            int padTop  = hlg != null ? hlg.padding.top  : 0;

            // pivot рамки = Top-Left, поэтому смещаем на -padLeft по X и +padTop по Y
            return new Vector2(localTL.x - padLeft, localTL.y + padTop);
        }

        private GameObject GetClusterPrefabForLengthHandler(int length)
        {
            if (_clusterPrefabs == null || _clusterPrefabs.Count == 0)
            {
                Debug.LogWarning("[GameView] _clusterPrefabs is null or empty");
                return null;
            }

            // Точное совпадение
            for (int i = 0; i < _clusterPrefabs.Count; i++)
            {
                var rule = _clusterPrefabs[i];
                if (rule != null && rule.ClusterPrefab != null && rule.ClusterTextLength == length)
                {
                    return rule.ClusterPrefab;
                }
            }

            // Лучший ≤ length
            GameObject bestLE = null;
            int bestLELen = int.MinValue;

            // Запомним максимальную доступную длину на случай, если ≤ length нет
            GameObject maxAny = null;
            int maxAnyLen = int.MinValue;

            for (int i = 0; i < _clusterPrefabs.Count; i++)
            {
                var rule = _clusterPrefabs[i];
                if (rule == null || rule.ClusterPrefab == null)
                {
                    continue;
                }

                int L = rule.ClusterTextLength;

                if (L <= length && L > bestLELen)
                {
                    bestLELen = L;
                    bestLE = rule.ClusterPrefab;
                }

                if (L > maxAnyLen)
                {
                    maxAnyLen = L;
                    maxAny = rule.ClusterPrefab;
                }
            }

            if (bestLE != null)
            {
                return bestLE;
            }

            if (maxAny != null)
            {
                return maxAny;
            }

            Debug.LogWarning($"[GameView] No cluster prefab found for length={length}");
            return null;
        }
    }
}