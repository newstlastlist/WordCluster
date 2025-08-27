using System;

namespace Domain
{
    [Serializable]
    public class LevelData
    {
        public int Id;
        public LevelLayout Layout;
        public string[] Words;
        public string[] Clusters;
        public LevelUiOptions UiOptions;
    }

    [Serializable]
    public struct LevelLayout
    {
        public int Rows;
        public int WordLength;
    }

    [Serializable]
    public struct LevelUiOptions
    {
        public bool ShuffleClusters;
    }
    
    [Serializable]
    public class LevelDataBundle
    {
        public LevelData[] Levels;
    }
}