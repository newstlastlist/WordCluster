using System;
using System.Collections.Generic;
using Domain;
using UnityEngine;

namespace Infrastructure
{
    public sealed class JsonLevelRepository : ILevelRepository
    {
        private readonly Dictionary<int, LevelData> _byId = new();
        private LevelData[] _all = Array.Empty<LevelData>();

        public JsonLevelRepository(string bundleResourcePath = "Levels/LevelsBundle", string folderPath = "Levels")
        {
            LoadFromResources(bundleResourcePath, folderPath);
        }

        public int Count => _all.Length;

        public LevelData[] LoadAll()
        {
            return _all;
        }

        public LevelData LoadById(int id)
        {
            return _byId.TryGetValue(id, out var lvl) ? lvl : null;
        }

        private void LoadFromResources(string bundlePath, string folderPath)
        {
            var loaded = new List<LevelData>();

            var bundle = Resources.Load<TextAsset>(bundlePath);
            if (bundle != null)
            {
                TryAddFromBundleText(bundle.text, loaded);
            }

            var singles = Resources.LoadAll<TextAsset>(folderPath);
            foreach (var ta in singles)
            {
                if (bundle != null && ta.name.Equals(System.IO.Path.GetFileNameWithoutExtension(bundlePath), StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!TryAddSingleLevelText(ta.text, loaded))
                {
                    TryAddFromBundleText(ta.text, loaded);
                }
            }

            if (loaded.Count == 0)
            {
                Debug.LogError("[JsonLevelRepository] Уровни не найдены. Проверь, что JSON лежит в Resources/Levels.");
                _all = Array.Empty<LevelData>();
                _byId.Clear();
                return;
            }

            _byId.Clear();
            foreach (var lvl in loaded)
            {
                if (lvl == null) continue;

                if (_byId.ContainsKey(lvl.Id))
                {
                    Debug.LogWarning($"[JsonLevelRepository] Дублирующийся Id уровня: {lvl.Id}. Перезаписываю предыдущий.");
                }
                _byId[lvl.Id] = lvl;
            }

            _all = new LevelData[_byId.Count];
            _byId.Values.CopyTo(_all, 0);

            Array.Sort(_all, (a, b) => a.Id.CompareTo(b.Id));
        }

        private static void TryAddFromBundleText(string json, List<LevelData> target)
        {
            try
            {
                var bundle = JsonUtility.FromJson<LevelDataBundle>(json);
                if (bundle?.Levels != null && bundle.Levels.Length > 0)
                {
                    target.AddRange(bundle.Levels);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[JsonLevelRepository] Не удалось распарсить bundle JSON: {e.Message}");
            }
        }

        private static bool TryAddSingleLevelText(string json, List<LevelData> target)
        {
            try
            {
                var lvl = JsonUtility.FromJson<LevelData>(json);
                if (lvl != null && lvl.Layout.WordLength > 0 && lvl.Layout.Rows > 0)
                {
                    target.Add(lvl);
                    return true;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[JsonLevelRepository] Не удалось распарсить single JSON: {e.Message}");
            }
            return false;
        }
    }
}