using UnityEngine;

namespace Infrastructure
{
    public sealed class PlayerPrefsProgressService : IProgressService
    {
        private const string Key = "PG_LastCompletedLevelIndex";
        private int _lastCompleted = -1;

        public int LastCompletedLevelIndex
        {
            get => _lastCompleted;
            set => _lastCompleted = value;
        }

        public void Save()
        {
            PlayerPrefs.SetInt(Key, _lastCompleted);
        }

        public void Load()
        {
            _lastCompleted = PlayerPrefs.HasKey(Key) ? PlayerPrefs.GetInt(Key) : -1;
        }

        public void Reset()
        {
            _lastCompleted = -1;
            PlayerPrefs.DeleteKey(Key);
        }
    }
}