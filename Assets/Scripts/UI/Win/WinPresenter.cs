using System;
using App;
using Infrastructure;
using Shared;

namespace UI.Win
{
    public sealed class WinPresenter
    {
        private readonly WinView _view;
        private readonly ILevelRepository _levelRepository;
        private readonly IProgressService _progressService;
        private readonly IScreenNavigator _screenNavigator;

        public WinPresenter(WinView view)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _levelRepository = Services.Get<ILevelRepository>();
            _progressService = Services.Get<IProgressService>();
            _screenNavigator = Services.Get<IScreenNavigator>();
        }

        public void Open()
        {
            _view.MainMenuClicked += OnMainMenuClicked;
            _view.NextLevelClicked += OnNextLevelClicked;

            int completed = Math.Max(0, _progressService.LastCompletedLevelIndex + 1);
            int total = _levelRepository.Count;

            string title = $"Victory! Completed {completed}/{total}";
            _view.SetResultsTitle(title);

            // TODO: fill words list sorted
        }

        public void Close()
        {
            _view.MainMenuClicked -= OnMainMenuClicked;
            _view.NextLevelClicked -= OnNextLevelClicked;
        }

        private void OnMainMenuClicked()
        {
            _screenNavigator.Show(ScreenId.Main);
        }

        private void OnNextLevelClicked()
        {
            _screenNavigator.Show(ScreenId.Game);
        }
    }
}