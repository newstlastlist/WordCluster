using Infrastructure;
using Shared;
using UnityEngine;

namespace App
{
    public sealed class GameApp : MonoBehaviour
    {
        [SerializeField] private ScreenController _screenController;

        private ServiceRegistry _registry;

        private void Awake()
        {
            Application.targetFrameRate = 60;

            _registry = new ServiceRegistry();
            Services.SetProvider(_registry);

            var levelRepo = new JsonLevelRepository(
                bundleResourcePath: "Levels/LevelsBundle",
                folderPath: "Levels"
            );

            var progress = new PlayerPrefsProgressService();
            progress.Load();

            _registry.Register<ILevelRepository>(levelRepo);
            _registry.Register<IProgressService>(progress);
            _registry.Register<IScreenNavigator>(new ScreenNavigator(_screenController));

            _screenController.Show(ScreenId.Main);
        }

        private void OnApplicationQuit()
        {
            if (Services.TryGet<IProgressService>(out var progress))
                progress.Save();
        }
    }
}