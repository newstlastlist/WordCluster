namespace Infrastructure
{
    public interface IProgressService
    {
        int LastCompletedLevelIndex { get; set; }
        void Save();
        void Load();
        void Reset();
    }
}