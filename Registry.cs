using System.Collections.Generic;

namespace Bastard
{
    public class Registry<T>
    {
        private List<T> m_Data = new();
        private Stack<int> m_free = new();

        public int Register(T item)
        {
            var ID = m_Data.Count;
            m_Data.Add(item);
            return ID;
        }

        public T Get(int ID)
        {
            return m_Data[ID];
        }
    }
}