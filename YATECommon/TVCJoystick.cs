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
// Videoton TV Computer Joystick Emulation
///////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using YATECommon.Drivers;
using YATECommon.Settings;

namespace YATECommon
{
  public class TVCJoystick
  {
    private JoystickSettings m_settings;
    private JoystickState m_joystick_state;

    #region · Properties ·

    public Joystick JoystickDevice { get; private set; }

    public bool Left
    {
      get
      {
        if (m_settings == null || JoystickDevice == null)
          return false;

        return GetChannelState((JoystickChannel)m_settings.LeftChannel, m_settings.LeftThreshold);
      }
    }

    public bool Right
    {
      get
      {
        if (m_settings == null || JoystickDevice == null)
          return false;

        return GetChannelState((JoystickChannel)m_settings.RightChannel, m_settings.RightThreshold);
      }
    }

    public bool Up
    {
      get
      {
        if (m_settings == null || JoystickDevice == null)
          return false;

        return GetChannelState((JoystickChannel)m_settings.UpChannel, m_settings.UpThreshold);
      }
    }

    public bool Down
    {
      get
      {
        if (m_settings == null || JoystickDevice == null)
          return false;

        return GetChannelState((JoystickChannel)m_settings.DownChannel, m_settings.DownThreshold);
      }
    }

    public bool Fire
    {
      get
      {
        if (m_settings == null || JoystickDevice == null)
          return false;

        return GetChannelState((JoystickChannel)m_settings.FireChannel, m_settings.FireThreshold);
      }
    }

    public bool Acceleration
    {
      get
      {
        if (m_settings == null || JoystickDevice == null)
          return false;

        return GetChannelState((JoystickChannel)m_settings.AccelerationChannel, m_settings.AccelerationThreshold);
      }
    }

    public byte StateByte
    {
      get
      {
        byte retval = 0;

        if (Up)
          retval |= 2;

        if (Down)
          retval |= 4;

        if (Fire)
          retval |= 8;

        if (Acceleration)
          retval |= 16;

        if (Right)
          retval |= 32;

        if (Left)
          retval |= 64;

        return (byte)(~retval);
      }
    }

    #endregion

    public TVCJoystick()
    {
      JoystickDevice = null;
      m_joystick_state = new JoystickState();
    }

    public void SetSettings(JoystickSettings in_settings)
    {
      m_settings = in_settings;

      // get driver class
      List<Joystick> available_joysticks = Joystick.GetJoysticks();

      JoystickDevice = available_joysticks.Find(
              delegate (Joystick joystick)
              {
                return joystick.Description == m_settings.ControllerName;
              });
    }

    public void Update()
    {
      if (m_settings == null || JoystickDevice == null)
        return;

      JoystickDevice.Update();
      JoystickDevice.GetState(m_joystick_state);
    }


    private bool GetChannelState(JoystickChannel in_channel, int in_threshold)
    {
      int threshold = 10 + in_threshold * 150;

      switch (in_channel)
      {
        case JoystickChannel.Undefined:
          return false;

        case JoystickChannel.XP:
          return (int)m_joystick_state.X - JoystickNative.JOY_CENTER > threshold;

        case JoystickChannel.XM:
          return (int)m_joystick_state.X - JoystickNative.JOY_CENTER < -threshold;

        case JoystickChannel.YP:
          return (int)m_joystick_state.Y - JoystickNative.JOY_CENTER > threshold;

        case JoystickChannel.YM:
          return (int)m_joystick_state.Y - JoystickNative.JOY_CENTER < -threshold;

        case JoystickChannel.ZP:
          return (int)m_joystick_state.Z - JoystickNative.JOY_CENTER > threshold;

        case JoystickChannel.ZM:
          return (int)m_joystick_state.Z - JoystickNative.JOY_CENTER < -threshold;

        case JoystickChannel.RP:
          return (int)m_joystick_state.R - JoystickNative.JOY_CENTER > threshold;

        case JoystickChannel.RM:
          return (int)m_joystick_state.R - JoystickNative.JOY_CENTER < -threshold;

        case JoystickChannel.UP:
          return (int)m_joystick_state.U - JoystickNative.JOY_CENTER > threshold;

        case JoystickChannel.UM:
          return (int)m_joystick_state.U - JoystickNative.JOY_CENTER < -threshold;

        case JoystickChannel.VP:
          return (int)m_joystick_state.V - JoystickNative.JOY_CENTER > threshold;

        case JoystickChannel.VM:
          return (int)m_joystick_state.V - JoystickNative.JOY_CENTER < -threshold;

        case JoystickChannel.Button1:
          return m_joystick_state.Button01;

        case JoystickChannel.Button2:
          return m_joystick_state.Button02;

        case JoystickChannel.Button3:
          return m_joystick_state.Button03;

        case JoystickChannel.Button4:
          return m_joystick_state.Button04;

        case JoystickChannel.Button5:
          return m_joystick_state.Button05;

        case JoystickChannel.Button6:
          return m_joystick_state.Button06;

        case JoystickChannel.Button7:
          return m_joystick_state.Button07;

        case JoystickChannel.Button8:
          return m_joystick_state.Button08;

        case JoystickChannel.Button9:
          return m_joystick_state.Button09;

        case JoystickChannel.Button10:
          return m_joystick_state.Button10;

        case JoystickChannel.Button11:
          return m_joystick_state.Button11;

        case JoystickChannel.Button12:
          return m_joystick_state.Button12;

        case JoystickChannel.Button13:
          return m_joystick_state.Button13;

        case JoystickChannel.Button14:
          return m_joystick_state.Button14;

        case JoystickChannel.Button15:
          return m_joystick_state.Button15;

        case JoystickChannel.Button16:
          return m_joystick_state.Button16;

        default:
          return false;
      }

    }
  }
}
