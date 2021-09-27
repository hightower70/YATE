///////////////////////////////////////////////////////////////////////////////
// Copyright (c) 2019-2021 Laszlo Arvai. All rights reserved.
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
// NanoSD card emulation class
///////////////////////////////////////////////////////////////////////////////
using System;
using System.IO;
using System.Reflection;
using YATECommon;
using YATECommon.Helpers;

namespace GameCard
{
  public class GameCard : ITVCCard
  {
    const byte PAGE_REGISTER = 0x0f;
    const byte JOY3_REGISTER = 0x02;
    const byte JOY4_REGISTER = 0x03;

    const byte JOY_UP = 0x01;
    const byte JOY_DOWN = 0x02;
    const byte JOY_LEFT = 0x04;
    const byte JOY_RIGHT = 0x08;
    const byte JOY_FIRE = 0x10;

    const int SN_CLOCK = 3125000;

    const ulong JOY_REFRESH_RATE = 20; // Joystick refresh rate in ms

    public const int MaxRomSize = 64 * 1024;

    public byte[] Rom { get; private set; }

    private ExpansionMain m_expansion_main;
    private ITVComputer m_tvcomputer;

    public GameCardSettings Settings { get; private set; }

    private UInt16 m_current_page_address = 0xe000;

    private ulong m_joystick_refresh_timestamp;

    private TVCJoystick m_joystick3;
    private TVCJoystick m_joystick4;

    public GameCard(ExpansionMain in_expansion_main)
    {
      m_expansion_main = in_expansion_main;

      m_joystick3 = new TVCJoystick();
      m_joystick4 = new TVCJoystick();
    }

    public bool SetSettings(GameCardSettings in_settings)
    {
      Settings = in_settings;

      m_joystick_refresh_timestamp = 0;

      // load ROM content
      bool settings_changed = LoadROM();

      // setup joysticks
      m_joystick3.SetSettings(Settings.Joystick3);
      m_joystick4.SetSettings(Settings.Joystick4);

      return settings_changed;
    }

    public void StoreSettings()
    {
      m_expansion_main.ParentManager.Settings.SetSettings(Settings);
    }

    private bool LoadROM()
    {
      bool changed = false;
      byte[] old_rom = null;

      // save old rom
      old_rom = Rom;

      // load ROM
      Rom = new byte[MaxRomSize];
      for (int i = 0; i < Rom.Length; i++)
        Rom[i] = 0xff;

      if (string.IsNullOrEmpty(Settings.ROMFileName))
      {
        ROMFile.LoadMemoryFromResource("GameCard.Resources.GameCard.bin", Rom);
      }
      else
      {
        ROMFile.LoadMemoryFromFile(Settings.ROMFileName, Rom);
      }

      changed = !ROMFile.IsMemoryEqual(old_rom, Rom);

      return changed;
    }

    public byte MemoryRead(ushort in_address)
    {
      return Rom[in_address | m_current_page_address];
    }

    public void MemoryWrite(ushort in_address, byte in_byte)
    {
      // no ROM write
    }

    public void Reset()
    {
    }

    /*public void Initialize(ITVComputer in_parent)
    {
     
    }
      */
    public void PortRead(ushort in_address, ref byte inout_data)
    {
      switch (in_address & 0x0f)
      {
        case JOY3_REGISTER:
          inout_data = GetJoystickStateByte(m_joystick3);
          break;

        case JOY4_REGISTER:
          inout_data = GetJoystickStateByte(m_joystick4);
          break;

      }
    }

    public void PortWrite(ushort in_address, byte in_byte)
    {
      switch (in_address & 0x0f)
      {
        case PAGE_REGISTER:
          m_current_page_address = (UInt16)((in_byte & 7) << 13);
          break;
      }
    }

    public void PeriodicCallback(ulong in_cpu_tick)
    {
      // refresh joystick state
      if (m_tvcomputer.GetTicksSince(m_joystick_refresh_timestamp) > JOY_REFRESH_RATE)
      {
        m_joystick_refresh_timestamp = m_tvcomputer.GetCPUTicks();

        m_joystick3.Update();
        m_joystick4.Update();
      }
    }

    public byte GetID()
    {
      return 0x03;
    }

    public void Install(ITVComputer in_parent)
    {
      m_tvcomputer = in_parent;
    }

    public void Remove(ITVComputer in_parent)
    {
      // no action needed
    }

    private byte GetJoystickStateByte(TVCJoystick in_joystick)
    {
      byte retval = 0Xff;

      if (in_joystick.Up)
        retval = (byte)(retval & ~JOY_UP);

      if (in_joystick.Down)
        retval = (byte)(retval & ~JOY_DOWN);

      if (in_joystick.Left)
        retval = (byte)(retval & ~JOY_LEFT);

      if (in_joystick.Right)
        retval = (byte)(retval & ~JOY_RIGHT);

      if (in_joystick.Fire)
        retval = (byte)(retval & ~JOY_FIRE);

      return retval;
    }
  }
}
