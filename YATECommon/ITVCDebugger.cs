///////////////////////////////////////////////////////////////////////////////
// Copyright (c) 2019 Laszlo Arvai. All rights reserved.
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
// TVC Debugger Interface
///////////////////////////////////////////////////////////////////////////////
namespace YATECommon
{
  public interface IDebugManager
  {
    void RegisterDebuggableMemory(IDebuggableMemory in_memory);
    void UnregisterDebuggableMemory(IDebuggableMemory in_memory);

    IDebuggableMemory GetDebuggableMemory(TVCMemoryType in_memory_type);
  }
}
