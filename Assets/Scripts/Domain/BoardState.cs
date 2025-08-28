using System;
using System.Collections.Generic;
using System.Text;

namespace Domain
{
    public sealed class BoardState
    {
        private readonly int _rowsCount;
        private readonly int _wordLength;

        // master-data по кластерам: ClusterId -> ClusterText
        private readonly Dictionary<int, string> _clusterTextById = new Dictionary<int, string>();

        // активные размещения: каждый кластер не более одного Placement
        private readonly List<Placement> _placements = new List<Placement>();

        // целевые слова уровня как мультимножество (слово -> сколько раз требуется)
        private readonly Dictionary<string, int> _targetWordMultiset = new Dictionary<string, int>();

        // порядок, в котором различные слова ВПЕРВЫЕ стали валидными на строках (для экрана Win)
        private readonly List<string> _wordCompletionOrder = new List<string>();
        private readonly HashSet<string> _wordsAlreadyCountedInOrder = new HashSet<string>();

        public int RowsCount => _rowsCount;
        public int WordLength => _wordLength;
        public int TotalClusters => _clusterTextById.Count;
        public int PlacedClustersCount => _placements.Count;
        public IReadOnlyList<string> WordCompletionOrder => _wordCompletionOrder;

        public enum PlacementError
        {
            None = 0,
            UnknownCluster = 1,
            AlreadyPlaced = 2,
            OutOfBounds = 3,
            Overlap = 4
        }

        public sealed class PlacementResult
        {
            public bool Success;
            public PlacementError Error;
        }

        // сущность, характеризующая размещение кластера внутри загаданного слова. 
        private readonly struct Placement
        {
            public readonly int ClusterId;
            public readonly int RowIndex;
            public readonly int StartColumn;    // нулевой индекс колонки в выбранной строке, куда встанет первая буква кластера.
            public readonly int Length;

            public Placement(int clusterId, int rowIndex, int startColumn, int length)
            {
                ClusterId = clusterId;
                RowIndex = rowIndex;
                StartColumn = startColumn;
                Length = length;
            }
        }

        public BoardState(
            int rowsCount,
            int wordLength,
            IEnumerable<(int clusterId, string clusterText)> clusters,
            IEnumerable<string> targetWords)
        {
            if (rowsCount <= 0)
            {
                throw new ArgumentException("rowsCount must be > 0");
            }

            if (wordLength <= 0)
            {
                throw new ArgumentException("wordLength must be > 0");
            }

            _rowsCount = rowsCount;
            _wordLength = wordLength;

            if (clusters == null)
            {
                throw new ArgumentNullException(nameof(clusters));
            }

            if (targetWords == null)
            {
                throw new ArgumentNullException(nameof(targetWords));
            }

            foreach (var (clusterId, clusterText) in clusters)
            {
                if (string.IsNullOrEmpty(clusterText))
                {
                    throw new ArgumentException($"Cluster {clusterId} has empty text.");
                }

                _clusterTextById[clusterId] = clusterText;
            }

            foreach (var word in targetWords)
            {
                if (_targetWordMultiset.TryGetValue(word, out int count))
                {
                    _targetWordMultiset[word] = count + 1;
                }
                else
                {
                    _targetWordMultiset[word] = 1;
                }
            }
        }

        public PlacementResult TryPlaceCluster(int clusterId, int rowIndex, int startColumn)
        {
            if (!_clusterTextById.ContainsKey(clusterId))
            {
                return new PlacementResult { Success = false, Error = PlacementError.UnknownCluster };
            }

            if (IsClusterAlreadyPlaced(clusterId))
            {
                return new PlacementResult { Success = false, Error = PlacementError.AlreadyPlaced };
            }

            string clusterText = _clusterTextById[clusterId];
            int clusterLength = clusterText.Length;

            if (!IsInsideRow(rowIndex, startColumn, clusterLength))
            {
                return new PlacementResult { Success = false, Error = PlacementError.OutOfBounds };
            }

            if (HasOverlapInRow(rowIndex, startColumn, clusterLength))
            {
                return new PlacementResult { Success = false, Error = PlacementError.Overlap };
            }

            _placements.Add(new Placement(clusterId, rowIndex, startColumn, clusterLength));
            RecalculateWordCompletions();

            return new PlacementResult { Success = true, Error = PlacementError.None };
        }

        public PlacementResult TryMoveCluster(int clusterId, int newRowIndex, int newStartColumn)
        {
            int placementIndex = FindPlacementIndexByCluster(clusterId);

            if (placementIndex < 0)
            {
                return TryPlaceCluster(clusterId, newRowIndex, newStartColumn);
            }

            string clusterText = _clusterTextById[clusterId];
            int clusterLength = clusterText.Length;

            if (!IsInsideRow(newRowIndex, newStartColumn, clusterLength))
            {
                return new PlacementResult { Success = false, Error = PlacementError.OutOfBounds };
            }

            // временно убираем для проверки оверлапов (чтобы не пересечься с самим собой)
            var oldPlacement = _placements[placementIndex];
            _placements.RemoveAt(placementIndex);

            bool hasOverlap = HasOverlapInRow(newRowIndex, newStartColumn, clusterLength);

            if (hasOverlap)
            {
                // откатываем
                _placements.Insert(placementIndex, oldPlacement);
                return new PlacementResult { Success = false, Error = PlacementError.Overlap };
            }

            _placements.Insert(placementIndex, new Placement(clusterId, newRowIndex, newStartColumn, clusterLength));
            RecalculateWordCompletions();

            return new PlacementResult { Success = true, Error = PlacementError.None };
        }

        // вовзращаем true если кластер уже лежал на поле
        public bool TryRemoveCluster(int clusterId)
        {
            int placementIndex = FindPlacementIndexByCluster(clusterId);

            if (placementIndex < 0)
            {
                return false;
            }

            _placements.RemoveAt(placementIndex);
            RecalculateWordCompletions();

            return true;
        }

        public bool IsEveryClusterPlaced()
        {
            return _placements.Count == _clusterTextById.Count;
        }

        public bool AreAllRowsValidWords()
        {
            var currentMultiset = BuildCurrentRowMultiset();
            if (currentMultiset.Count != _targetWordMultiset.Count)
            {
                return false;
            }

            foreach (var kvp in _targetWordMultiset)
            {
                if (!currentMultiset.TryGetValue(kvp.Key, out int count) || count != kvp.Value)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Полная победа по правилам ТЗ: все кластеры лежат И все строки валидны.
        /// </summary>
        public bool IsVictory()
        {
            if (!IsEveryClusterPlaced())
            {
                return false;
            }

            if (!AreAllRowsValidWords())
            {
                return false;
            }

            return true;
        }

        public List<(int clusterId, int rowIndex, int startColumn, int length)> GetPlacementsSnapshot()
        {
            var list = new List<(int, int, int, int)>(_placements.Count);

            for (int i = 0; i < _placements.Count; i++)
            {
                var p = _placements[i];
                list.Add((p.ClusterId, p.RowIndex, p.StartColumn, p.Length));
            }

            return list;
        }

        public PlacementResult CanPlaceClusterAt(int clusterId, int rowIndex, int startColumn)
        {
            if (!_clusterTextById.ContainsKey(clusterId))
            {
                return new PlacementResult { Success = false, Error = PlacementError.UnknownCluster };
            }

            string clusterText = _clusterTextById[clusterId];
            int clusterLength = clusterText.Length;

            if (!IsInsideRow(rowIndex, startColumn, clusterLength))
            {
                return new PlacementResult { Success = false, Error = PlacementError.OutOfBounds };
            }

            if (HasOverlapInRow(rowIndex, startColumn, clusterLength))
            {
                return new PlacementResult { Success = false, Error = PlacementError.Overlap };
            }

            return new PlacementResult { Success = true, Error = PlacementError.None };
        }

        private bool IsClusterAlreadyPlaced(int clusterId)
        {
            return FindPlacementIndexByCluster(clusterId) >= 0;
        }

        private int FindPlacementIndexByCluster(int clusterId)
        {
            for (int i = 0; i < _placements.Count; i++)
            {
                if (_placements[i].ClusterId == clusterId)
                {
                    return i;
                }
            }

            return -1;
        }

        private bool IsInsideRow(int rowIndex, int startColumn, int clusterLength)
        {
            if (rowIndex < 0 || rowIndex >= _rowsCount)
            {
                return false;
            }

            if (startColumn < 0)
            {
                return false;
            }

            if (startColumn + clusterLength > _wordLength)
            {
                return false;
            }

            return true;
        }

        private bool HasOverlapInRow(int rowIndex, int startColumn, int clusterLength)
        {
            // Пересечение по полуинтервалам [start; end)
            int newStart = startColumn;
            int newEnd = startColumn + clusterLength;

            for (int i = 0; i < _placements.Count; i++)
            {
                var p = _placements[i];
                if (p.RowIndex != rowIndex)
                {
                    continue;
                }

                int existStart = p.StartColumn;
                int existEnd = p.StartColumn + p.Length;

                bool overlap = newStart < existEnd && existStart < newEnd;
                if (overlap)
                {
                    return true;
                }
            }

            return false;
        }

        private void RecalculateWordCompletions()
        {
            // Строим текущие строки и фиксируем ВПЕРВЫЕ достигнутые целевые слова.
            for (int rowIndex = 0; rowIndex < _rowsCount; rowIndex++)
            {
                string rowWord = BuildRowString(rowIndex);

                if (rowWord.Length == _wordLength && _targetWordMultiset.ContainsKey(rowWord))
                {
                    if (_wordsAlreadyCountedInOrder.Add(rowWord))
                    {
                        _wordCompletionOrder.Add(rowWord);
                    }
                }
            }
        }

        private Dictionary<string, int> BuildCurrentRowMultiset()
        {
            var result = new Dictionary<string, int>();

            for (int rowIndex = 0; rowIndex < _rowsCount; rowIndex++)
            {
                string word = BuildRowString(rowIndex);

                // строка считается только если полностью заполнена
                if (word.Length == _wordLength)
                {
                    if (result.TryGetValue(word, out int count))
                    {
                        result[word] = count + 1;
                    }
                    else
                    {
                        result[word] = 1;
                    }
                }
                else
                {
                    // неполная строка => точно не равны мультимножества
                    // (но оставим логику сравнения наверху для прозрачности)
                }
            }

            return result;
        }

        private string BuildRowString(int rowIndex)
        {
            // Собираем строку "на лету": размещаем буквы из кластеров в буфер.
            var buffer = new char[_wordLength];
            bool hasAny = false;

            // заполним \0 для явности (массива default char и так '\0')
            for (int i = 0; i < _wordLength; i++)
            {
                buffer[i] = '\0';
            }

            for (int i = 0; i < _placements.Count; i++)
            {
                var p = _placements[i];
                if (p.RowIndex != rowIndex)
                {
                    continue;
                }

                string clusterText = _clusterTextById[p.ClusterId];

                for (int k = 0; k < p.Length; k++)
                {
                    buffer[p.StartColumn + k] = clusterText[k];
                    hasAny = true;
                }
            }

            if (!hasAny)
            {
                return string.Empty;
            }

            // если есть незаполненные ячейки, возвращаем укороченную строку до первого '\0'
            var sb = new StringBuilder(_wordLength);

            for (int i = 0; i < _wordLength; i++)
            {
                if (buffer[i] == '\0')
                {
                    break;
                }

                sb.Append(buffer[i]);
            }

            return sb.ToString();
        }

        private bool HasDuplicatesInTarget()
        {
            foreach (var kvp in _targetWordMultiset)
            {
                if (kvp.Value > 1)
                {
                    return true;
                }
            }

            return false;
        }
    }
}