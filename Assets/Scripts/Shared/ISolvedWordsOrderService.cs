using System.Collections.Generic;

namespace Shared
{
    public interface ISolvedWordsOrderService
    {
        IReadOnlyList<string> GetSnapshot();
        void AddIfNew(string word);
        void Clear();
    }
}