using System.Collections.Generic;
using System.Linq;

namespace YATE.Managers
{
  public class IndexedWindowManager
  {
    private List<int> m_window_indices = new List<int>();

    /// <summary>
    /// Gets a window index
    /// </summary>
    /// <returns></returns>
    public int AcquireWindowIndex()
    {
      // find unused index
      for (int i = 0; i < m_window_indices.Count; i++)
      {
        if (m_window_indices[i] < 0)
          return i;
      }

      // add new index
      m_window_indices.Add(m_window_indices.Count);

      return m_window_indices.Last();
    }

    /// <summary>
    /// Releases window index
    /// </summary>
    /// <param name="in_index"></param>
    public void ReleaseWindowIndex(int in_index)
    {
      if (in_index < m_window_indices.Count)
      {
        m_window_indices[in_index] = -1;
      }
    }
  }
}
