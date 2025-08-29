using System.Collections.Generic;

namespace Shared
{
    public sealed class SolvedWordsOrderService : ISolvedWordsOrderService
    {
        private readonly List<string> _ordered = new List<string>();
        private readonly HashSet<string> _seen = new HashSet<string>();

        public IReadOnlyList<string> GetSnapshot()
        {
            return _ordered;
        }

        public void AddIfNew(string word)
        {
            if (string.IsNullOrEmpty(word))
            {
                return;
            }

            if (_seen.Add(word))
            {
                _ordered.Add(word);
            }
        }

        public void Clear()
        {
            _ordered.Clear();
            _seen.Clear();
        }
    }
}