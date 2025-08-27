using System;
using App;
using Infrastructure;
using Shared;

namespace UI.Win
{
    public sealed class WinPresenter
    {
        private readonly WinView _view;
        private readonly ILevelRepository _levels;
        private readonly IProgressService _progress;
        private readonly IScreenNavigator _screenNavigator;

        public WinPresenter(WinView view)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _levels = Services.Get<ILevelRepository>();
            _progress = Services.Get<IProgressService>();
            _screenNavigator = Services.Get<IScreenNavigator>();
        }

        public void OnOpen()
        {
            int completed = Math.Max(0, _progress.LastCompletedLevelIndex + 1);
            int total = _levels.Count;

            string title = $"LEVEL COMPLETED! TOTAL PROGRESS: {completed}/{total}";
            _view.SetResultsTitle(title);

            // Позже: показать слова в порядке разгадки.
            // Пока заглушка: список будем наполнять после реализации домена.
        }

        public void OnClose()
        {
            // здесь пока ничего не нужно
        }

        public void GoToMainMenu()
        {
            _screenNavigator.Show(ScreenId.Main);
        }

        public void GoToNextLevel()
        {
            int nextIndex = (_progress.LastCompletedLevelIndex + 1) % Math.Max(1, _levels.Count);
            _screenNavigator.Show(ScreenId.Game);
        }
    }
}