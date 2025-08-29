using System;
using System.Collections.Generic;
using App;
using Domain;
using Infrastructure;
using Shared;

namespace UI.Game
{
    public sealed class GamePresenter
    {
        private readonly GameView _view;
        private readonly ILevelRepository _levelRepository;
        private readonly IProgressService _progressService;
        private readonly IScreenNavigator _screenNavigator;
        private readonly ISolvedWordsOrderService _solvedWordsOrderService;

        private BoardState _boardState;
        private readonly Dictionary<int, string> _clusterTextById = new Dictionary<int, string>();
        private readonly HashSet<int> _placedClusterIds = new HashSet<int>();

        private int _currentLevelIndex;
        private string[] _levelWords = Array.Empty<string>();

        public GamePresenter(GameView view)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _levelRepository = Services.Get<ILevelRepository>();
            _progressService = Services.Get<IProgressService>();
            _screenNavigator = Services.Get<IScreenNavigator>();
            _solvedWordsOrderService = Services.Get<ISolvedWordsOrderService>();
        }

        public void Open()
        {
            _view.OnDebugWinClicked += OnDebugWinClickedHandler;
            _view.OnClusterDropped += OnClusterDroppedHandler;
            _view.OnClusterDragEnded += OnClusterDragEndedHandler;
            
            _view.ResetForNewLevel();

            var levels = _levelRepository.LoadAll();
            int levelsCount = levels.Length;

            _currentLevelIndex = _progressService.ResolveCurrentLevelIndex(levelsCount);

            var levelData = levelsCount > 0 ? levels[_currentLevelIndex] : null;
            if (levelData == null)
            {
                _view.SetHeader("No levels");
                _boardState = null;
                _clusterTextById.Clear();
                _placedClusterIds.Clear();
                _levelWords = Array.Empty<string>();
                return;
            }

            _levelWords = levelData.Words ?? Array.Empty<string>();

            _view.SetHeader($"Level {levelData.Id}");

            BuildDomainForLevelHandler(levelData);

            _view.BuildGrid(_boardState.RowsCount, _boardState.WordLength);
            _view.RenderClusters(_clusterTextById);
            _view.ClearAllCells(); // буквы не рисуем в ячейки — визуал только рамками
            
        }

        public void Close()
        {
            _view.OnDebugWinClicked -= OnDebugWinClickedHandler;
            _view.OnClusterDropped -= OnClusterDroppedHandler;
            _view.OnClusterDragEnded -= OnClusterDragEndedHandler;
            
            _view.ClearAllVisuals();
        }

        private void BuildDomainForLevelHandler(LevelData level)
        {
            _clusterTextById.Clear();
            _placedClusterIds.Clear();
            _solvedWordsOrderService.Clear();

            // присвоим кластерам id 1..N
            int nextId = 1;
            foreach (string text in level.Clusters)
            {
                _clusterTextById[nextId++] = text;
            }

            // создаём BoardState с master-данными
            IEnumerable<(int clusterId, string clusterText)> clustersEnum = EnumerateClustersHandler();
            _boardState = new BoardState(
                level.Layout.Rows,
                level.Layout.WordLength,
                clustersEnum,
                level.Words
            );
        }

        private IEnumerable<(int clusterId, string clusterText)> EnumerateClustersHandler()
        {
            foreach (var kvp in _clusterTextById)
            {
                yield return (kvp.Key, kvp.Value);
            }
        }

        private void OnDebugWinClickedHandler()
        {
            ForceWinForDebugHandler();
        }
        
        private void OnClusterDragEndedHandler(int clusterId)
        {
            // Был ли дроп принят ячейкой? Если да — всё уже обработано в OnClusterDroppedHandler.
            bool accepted = _view.ConsumeDropAcceptedFlag(clusterId);
            if (accepted)
            {
                return;
            }

            // Драг завершился НЕ на ячейке: если кластер числится на поле — снимаем размещение.
            if (_boardState != null)
            {
                _boardState.TryRemoveCluster(clusterId);
            }
            _placedClusterIds.Remove(clusterId);

            // Визуал на всякий случай (если уже в ленте — просто нормализуем состояние)
            _view.ReturnClusterToPool(clusterId);
            _view.SetClusterLocked(clusterId, false);
            _view.SetClusterFrameState(clusterId, ClusterView.FrameState.Default);
        }

        private void OnClusterDroppedHandler(int clusterId, int rowIndex, int colIndex)
        {
            if (_boardState == null)
            {
                return;
            }

            var move = _boardState.TryMoveCluster(clusterId, rowIndex, colIndex);
            if (!move.Success)
            {
                var place = _boardState.TryPlaceCluster(clusterId, rowIndex, colIndex);
                if (!place.Success)
                {
                    // дроп БЫЛ на ячейку, но позиция невалидна → вернуть в ленту и гарантированно снять размещение
                    _view.ReturnClusterToPool(clusterId);

                    _boardState.TryRemoveCluster(clusterId);   // всегда пробуем удалить
                    _placedClusterIds.Remove(clusterId);

                    _view.SetClusterLocked(clusterId, false);
                    _view.SetClusterFrameState(clusterId, ClusterView.FrameState.Default);
                    return;
                }

                _placedClusterIds.Add(clusterId);
                _view.SetClusterInteractable(clusterId, false);
            }

            _view.AttachClusterToBoard(clusterId, rowIndex, colIndex);

            CheckCompletedRowsAndLockHandler();
            SyncSolvedWordsOrderHandler();
            TryTriggerVictoryIfSolvedHandler();
        }
        
        private void CheckCompletedRowsAndLockHandler()
        {
            if (_boardState == null || _levelWords == null || _levelWords.Length == 0)
            {
                return;
            }

            // снимок размещений
            var placements = _boardState.GetPlacementsSnapshot();

            for (int row = 0; row < _boardState.RowsCount; row++)
            {
                // собираем строку
                char[] buffer = new char[_boardState.WordLength];
                bool[] filled = new bool[_boardState.WordLength];

                // заполним буфер по кластерам этой строки
                List<int> rowClusterIds = new List<int>();

                for (int i = 0; i < placements.Count; i++)
                {
                    var p = placements[i];
                    if (p.rowIndex != row)
                    {
                        continue;
                    }

                    // нужен текст кластера
                    if (!_clusterTextById.TryGetValue(p.clusterId, out string clusterText) || string.IsNullOrEmpty(clusterText))
                    {
                        continue;
                    }

                    rowClusterIds.Add(p.clusterId);

                    for (int k = 0; k < clusterText.Length; k++)
                    {
                        int col = p.startColumn + k;
                        if (col >= 0 && col < buffer.Length)
                        {
                            buffer[col] = clusterText[k];
                            filled[col] = true;
                        }
                    }
                }

                // строка считается полной только если заполнены все колонки
                bool allFilled = true;
                for (int c = 0; c < filled.Length; c++)
                {
                    if (!filled[c])
                    {
                        allFilled = false;
                        break;
                    }
                }

                if (!allFilled)
                {
                    continue;
                }

                string word = new string(buffer);

                // если это одно из загаданных слов — фиксируем порядок и блокируем кластеры этой строки
                for (int i = 0; i < _levelWords.Length; i++)
                {
                    if (string.Equals(_levelWords[i], word, StringComparison.OrdinalIgnoreCase))
                    {
                        // добавим слово в сервис (если не добавляли ранее)
                        _solvedWordsOrderService.AddIfNew(word);

                        // заблокируем кластеры и покрасим их в Good
                        for (int j = 0; j < rowClusterIds.Count; j++)
                        {
                            int cid = rowClusterIds[j];
                            _view.SetClusterLocked(cid, true);
                            _view.SetClusterFrameState(cid, ClusterView.FrameState.Good);
                        }

                        // можно выходить из inner-loop — слово нашли
                        break;
                    }
                }
            }
        }

        private void SyncSolvedWordsOrderHandler()
        {
            if (_boardState == null)
            {
                return;
            }

            var order = _boardState.WordCompletionOrder;
            for (int i = 0; i < order.Count; i++)
            {
                _solvedWordsOrderService.AddIfNew(order[i]);
            }
        }

        private void TryTriggerVictoryIfSolvedHandler()
        {
            if (_boardState == null)
            {
                return;
            }

            if (_boardState.IsVictory())
            {
                SyncSolvedWordsOrderHandler();
                    
                int levelsCount = _levelRepository.Count;
                _progressService.OnLevelCompleted(levelsCount, _currentLevelIndex);
                _progressService.Save();

                _screenNavigator.Show(ScreenId.Win);
            }
        }

        private void ForceWinForDebugHandler()
        {
            SyncSolvedWordsOrderHandler();
            
            int levelsCount = _levelRepository.Count;
            _progressService.OnLevelCompleted(levelsCount, _currentLevelIndex);
            _progressService.Save();

            _screenNavigator.Show(ScreenId.Win);
        }
    }
}