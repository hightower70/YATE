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
// Videoton TV Computer Tape Interface Emulation
///////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YATE.Emulator.TVCHardware
{
  /// <summary>
  /// Tape hardware emulation
  /// </summary>
  public class TVCTape
  {

    public TVCTape(TVComputer in_tvc)
    {
    }


    // PORT 05H
    // ========
    //
    // +-------+-------+-------+-------+-------+-------+-------+-------+
    // |                   M O T O R  C O N T R O L                    |
    // +---7---+---6---+---5---+---4---+---3---+---2---+---1---+---0---+
    // |   M1  |   M0  |   +   |   +   |   +   |   +   |   +   |   +   |
    // +-------+-------+-------+-------+-------+-------+-------+-------+
    private void PortWrite05H(ushort in_address, byte in_data)
    {

    }

    // PORT 50H
    // ========
    //
    // +-------+-------+-------+-------+-------+-------+-------+-------+
    // |                   T A P E  D A T A  O U T                     |
    // +---7---+---6---+---5---+---4---+---3---+---2---+---1---+---0---+
    // |   -   |   -   |   -   |   -   |   -   |   -   |   -   |   -   |
    // +-------+-------+-------+-------+-------+-------+-------+-------+
    private void PortWrite50H(ushort in_address, byte in_data)
    {

    }
  }
}
