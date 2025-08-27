using System;
using App;
using Infrastructure;
using Shared;

namespace UI.MainMenu
{
    public sealed class MainMenuPresenter
    {
        private readonly MainMenuView _view;
        private readonly ILevelRepository _levels;
        private readonly IProgressService _progress;
        private readonly IScreenNavigator _screenNavigator;

        public MainMenuPresenter(MainMenuView view)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _levels = Services.Get<ILevelRepository>();
            _progress = Services.Get<IProgressService>();
            _screenNavigator = Services.Get<IScreenNavigator>();
        }

        public void OnOpen()
        {
            _view.SetTitle("WORD PUZZLE");
            var completed = _progress.LastCompletedLevelIndex;
            var total = _levels.Count;

            string progressText = $"TOTAL PROGRESS: {Math.Max(0, completed + 1)} / {total}";
            _view.SetProgressText(progressText);
        }

        public void OnClose()
        {
            
        }

        public void StartGame()
        {
            _screenNavigator.Show(ScreenId.Game);
        }
    }
}