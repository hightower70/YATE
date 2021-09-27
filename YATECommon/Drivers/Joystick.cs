using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace YATECommon.Drivers
{
  public class Joystick
  {

    private JoystickNative.JOYINFOEX m_joy_info_ex = new JoystickNative.JOYINFOEX();

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public static List<Joystick> GetJoysticks()
    {
      List<Joystick> ret = new List<Joystick>();

      JoystickNative.JOYCAPS tmpJoyCaps = new JoystickNative.JOYCAPS();

      uint joystick_device_count = JoystickNative.joyGetNumDevs();

      for (int i = 0; i < joystick_device_count; i++)
      {
        if (JoystickNative.joyGetDevCaps((UIntPtr)i, ref tmpJoyCaps, (uint)Marshal.SizeOf(tmpJoyCaps)) == JoystickNative.JOYERR_NOERROR) // Get joystick info
        {
          JoystickNative.JOYINFO joyinfo = new JoystickNative.JOYINFO();
          if (JoystickNative.joyGetPos((uint)i, ref joyinfo) == JoystickNative.JOYERR_NOERROR)
          {
            ret.Add(new Joystick(i));
          }
        }
      }

      Joystick no_joy = new Joystick(-1);
      ret.Add(no_joy);

      return ret;
    }

    #region · Properties ·

    public string Description { get; private set; }

    /// <summary>
    /// 
    /// </summary>
    public uint ID { get; private set; }

    /// <summary>
    /// 
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// 
    /// </summary>
    public bool HasZ { get; private set; }

    /// <summary>
    /// 
    /// </summary>
    public bool HasR { get; private set; }

    /// <summary>
    /// 
    /// </summary>
    public bool HasU { get; private set; }

    /// <summary>
    /// 
    /// </summary>
    public bool HasV { get; private set; }

    /// <summary>
    /// 
    /// </summary>
    public bool HasPOV { get; private set; }

    /// <summary>
    /// 
    /// </summary>
    public bool IsPOV4DIR { get; private set; }

    /// <summary>
    /// 
    /// </summary>
    public bool IsPOVCTS { get; private set; }


    /// <summary>
    /// 
    /// </summary>
    public uint NumberOfAxes { get; private set; }

    /// <summary>
    /// 
    /// </summary>
    public uint NumberOfButtons { get; private set; }

    #endregion

    #region · Constructor ·

    /// <summary>
    /// 
    /// </summary>
    public Joystick(int in_joy_index)
    {
      if (in_joy_index < 0)
      {
        ID = JoystickNative.joyGetNumDevs();
        Name = "";

        HasZ = false;
        HasR = false;
        HasU = false;
        HasV = false;
        HasPOV = false;
        IsPOV4DIR = false;
        IsPOVCTS = false;

        NumberOfAxes = 0;
        NumberOfButtons = 0;

        Description = "[none]";
      }
      else
      {
        JoystickNative.JOYCAPS joyCaps = new JoystickNative.JOYCAPS();

        if (JoystickNative.joyGetDevCaps((UIntPtr)in_joy_index, ref joyCaps, (uint)Marshal.SizeOf(joyCaps)) != 0) //Get joystick info
        {
          throw new Exception("Joystick is not ready.");
        }

        ID = (uint)in_joy_index;
        Name = joyCaps.szPname;

        HasZ = (joyCaps.wCaps & JoystickNative.JOYCAPS_HASZ) != 0;
        HasR = (joyCaps.wCaps & JoystickNative.JOYCAPS_HASR) != 0;
        HasU = (joyCaps.wCaps & JoystickNative.JOYCAPS_HASU) != 0;
        HasV = (joyCaps.wCaps & JoystickNative.JOYCAPS_HASV) != 0;
        HasPOV = (joyCaps.wCaps & JoystickNative.JOYCAPS_HASPOV) != 0;
        IsPOV4DIR = (joyCaps.wCaps & JoystickNative.JOYCAPS_POV4DIR) != 0;
        IsPOVCTS = (joyCaps.wCaps & JoystickNative.JOYCAPS_POVCTS) != 0;

        NumberOfAxes = joyCaps.wNumAxes;
        NumberOfButtons = joyCaps.wNumButtons;

        // try to get human readable name
        try
        {
          const string userRoot = "HKEY_CURRENT_USER";
          string key = string.Format("{0}\\System\\CurrentControlSet\\Control\\MediaResources\\Joystick\\{1}\\CurrentJoystickSettings", userRoot, joyCaps.szRegKey);
          string value_name = string.Format("Joystick{0}OEMName", in_joy_index + 1);

          string oem_name = (string)Registry.GetValue(key, value_name, "");

          key = string.Format("{0}\\System\\CurrentControlSet\\Control\\MediaProperties\\PrivateProperties\\Joystick\\OEM\\{1}", userRoot, oem_name);
          Description = (string)Registry.GetValue(key, "OEMName", "");
        }
        catch
        {
          Description = "Game Controller #" + (in_joy_index + 1).ToString();
        }
      }
    }

    #endregion

    public void Update()
    {
      m_joy_info_ex.dwSize = (uint)Marshal.SizeOf(m_joy_info_ex);
      m_joy_info_ex.dwFlags = 0x2ff;


      uint joy_device_count = JoystickNative.joyGetNumDevs();
      if (ID == joy_device_count)
      {
        m_joy_info_ex.dwPOV = JoystickNative.JOY_POVCENTERED;
        m_joy_info_ex.dwRpos = JoystickNative.JOY_CENTER;
        m_joy_info_ex.dwUpos = JoystickNative.JOY_CENTER;
        m_joy_info_ex.dwVpos = JoystickNative.JOY_CENTER;
        m_joy_info_ex.dwXpos = JoystickNative.JOY_CENTER;
        m_joy_info_ex.dwYpos = JoystickNative.JOY_CENTER;
        m_joy_info_ex.dwZpos = JoystickNative.JOY_CENTER;

        m_joy_info_ex.dwButtons = 0;
      }
      else
      {
        JoystickNative.joyGetPosEx(ID, ref m_joy_info_ex);
      }
    }

    /// <summary>
    /// 
    /// </summary>
    public void GetState(JoystickState inout_joystick_data)
    {
      inout_joystick_data.R = m_joy_info_ex.dwRpos;
      inout_joystick_data.U = m_joy_info_ex.dwUpos;
      inout_joystick_data.V = m_joy_info_ex.dwVpos;
      inout_joystick_data.X = m_joy_info_ex.dwXpos;
      inout_joystick_data.Y = m_joy_info_ex.dwYpos;
      inout_joystick_data.Z = m_joy_info_ex.dwZpos;

      inout_joystick_data.Button01 = GetButtonState(JoystickNative.JOY_BUTTON1);
      inout_joystick_data.Button02 = GetButtonState(JoystickNative.JOY_BUTTON2);
      inout_joystick_data.Button03 = GetButtonState(JoystickNative.JOY_BUTTON3);
      inout_joystick_data.Button04 = GetButtonState(JoystickNative.JOY_BUTTON4);
      inout_joystick_data.Button05 = GetButtonState(JoystickNative.JOY_BUTTON5);
      inout_joystick_data.Button06 = GetButtonState(JoystickNative.JOY_BUTTON6);
      inout_joystick_data.Button07 = GetButtonState(JoystickNative.JOY_BUTTON7);
      inout_joystick_data.Button08 = GetButtonState(JoystickNative.JOY_BUTTON8);
      inout_joystick_data.Button09 = GetButtonState(JoystickNative.JOY_BUTTON9);
      inout_joystick_data.Button10 = GetButtonState(JoystickNative.JOY_BUTTON10);
      inout_joystick_data.Button11 = GetButtonState(JoystickNative.JOY_BUTTON11);
      inout_joystick_data.Button12 = GetButtonState(JoystickNative.JOY_BUTTON12);
      inout_joystick_data.Button13 = GetButtonState(JoystickNative.JOY_BUTTON13);
      inout_joystick_data.Button14 = GetButtonState(JoystickNative.JOY_BUTTON14);
      inout_joystick_data.Button15 = GetButtonState(JoystickNative.JOY_BUTTON15);
      inout_joystick_data.Button16 = GetButtonState(JoystickNative.JOY_BUTTON16);

      inout_joystick_data.POV = m_joy_info_ex.dwPOV;
      inout_joystick_data.POVAngle = m_joy_info_ex.dwPOV / 100.0;
    }


    private bool GetButtonState(UInt32 button)
    {
      uint tmp = m_joy_info_ex.dwButtons & button;
      if (tmp == 0) return false;
      else return true;
    }
  }
}