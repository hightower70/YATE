///////////////////////////////////////////////////////////////////////////////
// Copyright (c) 2019-2020 Laszlo Arvai. All rights reserved.
//
// This library is free software; you can redistribute it and/or modify it 
// under the terms of the GNU Lesser General Public License as published
// by the Free Software Foundation; either version 2.1 of the License, 
// or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston,
// MA 02110-1301  USA
///////////////////////////////////////////////////////////////////////////////
// File description
// ----------------
// Manages multiple instance of the same window (indexed windows)
///////////////////////////////////////////////////////////////////////////////
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
