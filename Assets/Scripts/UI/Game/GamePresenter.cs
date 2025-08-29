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
            _view.CellClicked += OnCellClicked;
            _view.ClusterClicked += OnClusterClicked;
            _view.DebugWinClicked += OnDebugWinClicked;

            _currentLevelIndex = Math.Clamp(_progressService.LastCompletedLevelIndex + 1, 0, Math.Max(0, _levelRepository.Count - 1));

            var levels = _levelRepository.LoadAll();
            var levelData = levels.Length > 0 ? levels[_currentLevelIndex] : null;

            if (levelData == null)
            {
                _view.SetHeader("No levels");
                _boardState = null;
                _clusterTextById.Clear();
                _selectedClusterId = null;
                return;
            }

            _view.SetHeader($"Level {levelData.Id} — {levelData.Layout.Rows}×{levelData.Layout.WordLength}");

            BuildDomainForLevel(levelData);

            _view.BuildGrid(_boardState.RowsCount, _boardState.WordLength);
            _view.RenderClusters(_clusterTextById);
        }

        public void Close()
        {
            _view.DebugWinClicked -= OnDebugWinClicked;
            _view.CellClicked -= OnCellClicked;
            _view.ClusterClicked -= OnClusterClicked;

            _selectedClusterId = null;
            _clusterTextById.Clear();
            _boardState = null;
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

        private void OnClusterClicked(int clusterId)
        {
            if (_clusterTextById.ContainsKey(clusterId))
            {
                _selectedClusterId = clusterId;
            }
        }

        private void OnCellClicked(int rowIndex, int colIndex)
        {
            if (_boardState == null || !_selectedClusterId.HasValue)
            {
                return;
            }

            int clusterId = _selectedClusterId.Value;

            var result = _boardState.TryPlaceCluster(clusterId, rowIndex, colIndex);
            if (result.Success)
            {
                _selectedClusterId = null;

                RefreshGridFromBoardState();
                _view.SetClusterInteractable(clusterId, false);

                TryTriggerVictoryIfSolved();
            }
            else
            {
                // TODO: можно подсветить ошибку в UI (например, красный флеш ячеек)
            }
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

        private void RefreshGridFromBoardState()
        {
            if (_boardState == null)
            {
                return;
            }

            _view.ClearAllCells();

            var placements = _boardState.GetPlacementsSnapshot();
            foreach (var placement in placements)
            {
                string clusterText = _clusterTextById[placement.clusterId];

                for (int k = 0; k < placement.length; k++)
                {
                    int rowIndex = placement.rowIndex;
                    int colIndex = placement.startColumn + k;
                    char ch = clusterText[k];

                    _view.SetCellChar(rowIndex, colIndex, ch);
                }
            }
        }

        private void TryTriggerVictoryIfSolved()
        {
            if (_boardState == null)
            {
                return;
            }

            if (_boardState.IsVictory())
            {
                _progressService.LastCompletedLevelIndex =
                    Math.Max(_progressService.LastCompletedLevelIndex, _currentLevelIndex);

                _progressService.Save();
                _screenNavigator.Show(ScreenId.Win);
            }
        }

        private void ForceWinForDebug()
        {
            _progressService.LastCompletedLevelIndex =
                Math.Max(_progressService.LastCompletedLevelIndex, _currentLevelIndex);

            _progressService.Save();
            _screenNavigator.Show(ScreenId.Win);
        }
    }
}