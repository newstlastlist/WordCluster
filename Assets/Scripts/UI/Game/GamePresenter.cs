using System;
using App;
using Infrastructure;
using Shared;

namespace UI.Game
{
    public sealed class GamePresenter
    {
        private readonly GameView _view;
        private readonly ILevelRepository _levelsRepo;
        private readonly IProgressService _progress;
        private readonly IScreenNavigator _screenNavigator;

        private int _currentLevelIndex;

        public GamePresenter(GameView view)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _levelsRepo = Services.Get<ILevelRepository>();
            _progress = Services.Get<IProgressService>();
            _screenNavigator = Services.Get<IScreenNavigator>();
        }

        public void OnOpen()
        {
            _currentLevelIndex = Math.Clamp(_progress.LastCompletedLevelIndex + 1, 0, Math.Max(0, _levelsRepo.Count - 1));
            var all = _levelsRepo.LoadAll();
            var level = all.Length > 0 ? all[_currentLevelIndex] : null;

            string header = level != null
                ? $"LEVEL {level.Id} — {level.Layout.Rows}×{level.Layout.WordLength}"
                : "THERE IS NO LEVELS";

            _view.SetHeader(header);

            // Здесь позже: инициализация доменной логики, построение грида и полосы кластеров
        }

        public void OnClose()
        {
            // позже: освобождение подписок/ресурсов
        }

        public void DebugWin()
        {
            _progress.LastCompletedLevelIndex = Math.Max(_progress.LastCompletedLevelIndex, _currentLevelIndex);
            _progress.Save();

            _screenNavigator.Show(ScreenId.Win);
        }
    }
}