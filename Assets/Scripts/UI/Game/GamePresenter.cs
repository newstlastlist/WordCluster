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

        private int _currentLevelIndex;
        private BoardState _boardState;

        private readonly Dictionary<int, string> _clusterTextById = new Dictionary<int, string>();

        private int? _selectedClusterId;
        public int CurrentLevelIndex => _currentLevelIndex;

        public GamePresenter(GameView view)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _levelRepository = Services.Get<ILevelRepository>();
            _progressService = Services.Get<IProgressService>();
            _screenNavigator = Services.Get<IScreenNavigator>();
        }

        public void Open()
        {
            _view.DebugWinClicked += OnDebugWinClicked;

            _currentLevelIndex = Math.Clamp(_progressService.LastCompletedLevelIndex + 1, 0, Math.Max(0, _levelRepository.Count - 1));

            var levels = _levelRepository.LoadAll();
            var level = levels.Length > 0 ? levels[_currentLevelIndex] : null;

            if (level == null)
            {
                _view.SetHeader("No levels");
                _boardState = null;
                _clusterTextById.Clear();
                _selectedClusterId = null;
                return;
            }

            _view.SetHeader($"Level {level.Id} — {level.Layout.Rows}×{level.Layout.WordLength}");

            BuildDomainForLevel(level);

            // Здесь позже: отдать данные во View для построения грида и списка кластеров
            // (размеры берём из _boardState.RowsCount и _boardState.WordLength; кластеры — из _clusterTextById)
        }

        public void Close()
        {
            _view.DebugWinClicked -= OnDebugWinClicked;

            _selectedClusterId = null;
            _clusterTextById.Clear();
            _boardState = null;
        }

        public void SelectCluster(int clusterId)
        {
            if (_clusterTextById.ContainsKey(clusterId))
            {
                _selectedClusterId = clusterId;
                return;
            }

            _selectedClusterId = null;
        }

        public void ClearSelectedCluster()
        {
            _selectedClusterId = null;
        }

        public BoardState.PlacementResult RequestPlaceSelectedCluster(int rowIndex, int startColumn)
        {
            if (_boardState == null)
            {
                return new BoardState.PlacementResult { Success = false, Error = BoardState.PlacementError.UnknownCluster };
            }

            if (_selectedClusterId.HasValue == false)
            {
                return new BoardState.PlacementResult { Success = false, Error = BoardState.PlacementError.UnknownCluster };
            }

            int clusterId = _selectedClusterId.Value;
            var result = _boardState.TryPlaceCluster(clusterId, rowIndex, startColumn);

            if (result.Success)
            {
                _selectedClusterId = null;
                TryTriggerVictoryIfSolved();
            }

            return result;
        }

        public BoardState.PlacementResult RequestMoveCluster(int clusterId, int newRowIndex, int newStartColumn)
        {
            if (_boardState == null)
            {
                return new BoardState.PlacementResult { Success = false, Error = BoardState.PlacementError.UnknownCluster };
            }

            var result = _boardState.TryMoveCluster(clusterId, newRowIndex, newStartColumn);

            if (result.Success)
            {
                TryTriggerVictoryIfSolved();
            }

            return result;
        }

        public bool RequestRemoveCluster(int clusterId)
        {
            if (_boardState == null)
            {
                return false;
            }

            bool removed = _boardState.TryRemoveCluster(clusterId);

            if (removed)
            {
                // Победа от удаления не наступит, но держим общую логику единообразной
                TryTriggerVictoryIfSolved();
            }

            return removed;
        }

        public BoardState GetBoardState()
        {
            return _boardState;
        }

        public IReadOnlyDictionary<int, string> GetClusters()
        {
            return _clusterTextById;
        }

        private void OnDebugWinClicked()
        {
            ForceWinForDebug();
        }

        private void BuildDomainForLevel(LevelData levelData)
        {
            _clusterTextById.Clear();

            var clusterPairs = new List<(int clusterId, string clusterText)>(levelData.Clusters.Length);

            for (int i = 0; i < levelData.Clusters.Length; i++)
            {
                string clusterText = levelData.Clusters[i];
                _clusterTextById[i] = clusterText;
                clusterPairs.Add((i, clusterText));
            }

            _boardState = new BoardState(
                rowsCount: levelData.Layout.Rows,
                wordLength: levelData.Layout.WordLength,
                clusters: clusterPairs,
                targetWords: levelData.Words
            );
        }

        private void TryTriggerVictoryIfSolved()
        {
            if (_boardState == null)
            {
                return;
            }

            if (_boardState.IsVictory())
            {
                _progressService.LastCompletedLevelIndex = Math.Max(_progressService.LastCompletedLevelIndex, _currentLevelIndex);
                _progressService.Save();

                _screenNavigator.Show(ScreenId.Win);
            }
        }

        private void ForceWinForDebug()
        {
            _progressService.LastCompletedLevelIndex = Math.Max(_progressService.LastCompletedLevelIndex, _currentLevelIndex);
            _progressService.Save();

            _screenNavigator.Show(ScreenId.Win);
        }
    }
}