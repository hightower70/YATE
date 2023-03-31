///////////////////////////////////////////////////////////////////////////////
// Copyright (c) 2019-2023 Laszlo Arvai. All rights reserved.
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
// TVC Debugger
///////////////////////////////////////////////////////////////////////////////
using System;
using YATECommon;

namespace YATE.Managers
{
  /// <summary>
  /// TVC debugger manager
  /// </summary>
  internal class DebugManager : IDebugManager
  {
    private IDebuggableMemory[]  m_memories;

    /// <summary>
    /// Default constructor
    /// </summary>
    public DebugManager()
    {
      m_memories = new IDebuggableMemory[Enum.GetNames(typeof(TVCMemoryType)).Length];

      for (int i = 0; i < m_memories.Length; i++)
      {
        m_memories[i] = null;
      }
    }

    /// <summary>
    /// Gets debuggable memory
    /// </summary>
    /// <param name="in_memory_type"></param>
    /// <returns></returns>
    public IDebuggableMemory GetDebuggableMemory(TVCMemoryType in_memory_type)
    {
      return m_memories[(int)in_memory_type];
    }

    /// <summary>
    /// Registers debuggable memory
    /// </summary>
    /// <param name="in_memory"></param>
    public void RegisterDebuggableMemory(IDebuggableMemory in_memory)
    {
      m_memories[(int)in_memory.MemoryType] = in_memory;
    }

    /// <summary>
    /// Unregisters debuggable memory
    /// </summary>
    /// <param name="in_memory"></param>
    public void UnregisterDebuggableMemory(IDebuggableMemory in_memory)
    {
      m_memories[(int)in_memory.MemoryType] = null;
    }
  }
}
