using System;
using App;
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
            var all = _levelRepository.LoadAll();
            var level = all.Length > 0 ? all[_currentLevelIndex] : null;

            string header = level != null
                ? $"Level {level.Id} — {level.Layout.Rows}×{level.Layout.WordLength}"
                : "No levels";

            _view.SetHeader(header);

            // TODO: greed , clusters
        }

        public void Close()
        {
            _view.DebugWinClicked -= OnDebugWinClicked;
        }

        private void OnDebugWinClicked()
        {
            _progressService.LastCompletedLevelIndex = Math.Max(_progressService.LastCompletedLevelIndex, _currentLevelIndex);
            _progressService.Save();

            _screenNavigator.Show(ScreenId.Win);
        }
    }
}