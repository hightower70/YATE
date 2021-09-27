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
// Joystick driver native functions
///////////////////////////////////////////////////////////////////////////////
using System;
using System.Runtime.InteropServices;

namespace YATECommon.Drivers
{
  class JoystickNative
  {
    #region · Constants ·
    const string DLLName = "winmm.dll";

    /// <summary></summary>
    public const uint JOYERR_NOERROR = 0;

    /// <summary></summary>
    public const uint JOYERR_PARMS = 165;

    /// <summary></summary>
    public const uint JOYERR_NOCANDO = 166;

    /// <summary></summary>
    public const uint JOYERR_UNPLUGGED = 167;

    /// <summary>
    /// Joystick button 1
    /// </summary>
    public const uint JOY_BUTTON1 = 0x00000001;

    /// <summary>
    /// Joystick button 2
    /// </summary>
    public const uint JOY_BUTTON2 = 0x00000002;

    /// <summary>
    /// Joystick button 3
    /// </summary>
    public const uint JOY_BUTTON3 = 0x00000004;

    /// <summary>
    /// Joystick button 4
    /// </summary>
    public const uint JOY_BUTTON4 = 0x00000008;

    /// <summary>
    /// Joystick button 5
    /// </summary>
    public const uint JOY_BUTTON5 = 0x00000010;

    /// <summary>
    /// Joystick button 6
    /// </summary>
    public const uint JOY_BUTTON6 = 0x00000020;

    /// <summary>
    /// Joystick button 7
    /// </summary>
    public const uint JOY_BUTTON7 = 0x00000040;

    /// <summary>
    /// Joystick button 8
    /// </summary>
    public const uint JOY_BUTTON8 = 0x00000080;

    /// <summary>
    /// Joystick button 9
    /// </summary>
    public const uint JOY_BUTTON9 = 0x00000100;

    /// <summary>
    /// Joystick button 10
    /// </summary>
    public const uint JOY_BUTTON10 = 0x00000200;

    /// <summary>
    /// Joystick button 11
    /// </summary>
    public const uint JOY_BUTTON11 = 0x00000400;

    /// <summary>
    /// Joystick button 12
    /// </summary>
    public const uint JOY_BUTTON12 = 0x00000800;

    /// <summary>
    /// Joystick button 13
    /// </summary>
    public const uint JOY_BUTTON13 = 0x00001000;

    /// <summary>
    /// Joystick button 14
    /// </summary>
    public const uint JOY_BUTTON14 = 0x00002000;

    /// <summary>
    /// Joystick button 15
    /// </summary>
    public const uint JOY_BUTTON15 = 0x00004000;

    /// <summary>
    /// Joystick button 16
    /// </summary>
    public const uint JOY_BUTTON16 = 0x00008000;

    /// <summary>
    /// Joystick has z-coordinate information.
    /// </summary>
    public const uint JOYCAPS_HASZ = 1;

    /// <summary>
    /// Joystick has rudder (fourth axis) information.
    /// </summary>
    public const uint JOYCAPS_HASR = 2;

    /// <summary>
    /// Joystick has u-coordinate (fifth axis) information.
    /// </summary>
    public const uint JOYCAPS_HASU = 4;

    /// <summary>
    /// Joystick has v-coordinate (sixth axis) information.
    /// </summary>
    public const uint JOYCAPS_HASV = 8;

    /// <summary>
    /// Joystick has point-of-view information.
    /// </summary>
    public const uint JOYCAPS_HASPOV = 16;

    /// <summary>
    /// Joystick point-of-view supports discrete values (centered, forward, backward, left, and right).
    /// </summary>
    public const uint JOYCAPS_POV4DIR = 32;

    /// <summary>
    /// Joystick point-of-view supports continuous degree bearings.
    /// </summary>
    public const uint JOYCAPS_POVCTS = 64;

    /// <summary></summary>
    public const uint JOY_RETURNX = 0x00000001;

    /// <summary></summary>
    public const uint JOY_RETURNY = 0x00000002;

    /// <summary></summary>
    public const uint JOY_RETURNZ = 0x00000004;

    /// <summary></summary>
    public const uint JOY_RETURNR = 0x00000008;

    /// <summary></summary>
    public const uint JOY_RETURNU = 0x00000010;

    /// <summary></summary>
    public const uint JOY_RETURNV = 0x00000020;

    /// <summary></summary>
    public const uint JOY_RETURNPOV = 0x00000040;

    /// <summary></summary>
    public const uint JOY_RETURNBUTTONS = 0x00000080;

    /// <summary></summary>
    public const uint JOY_RETURNRAWDATA = 0x00000100;

    /// <summary></summary>
    public const uint JOY_RETURNPOVCTS = 0x00000200;

    /// <summary></summary>
    public const uint JOY_RETURNCENTERED = 0x00000400;

    /// <summary></summary>
    public const uint JOY_USEDEADZONE = 0x00000800;

    /// <summary></summary>
    public const uint JOY_RETURNALL = (JOY_RETURNX | JOY_RETURNY | JOY_RETURNZ | JOY_RETURNR | JOY_RETURNU | JOY_RETURNV | JOY_RETURNPOV | JOY_RETURNBUTTONS);

    /// <summary></summary>
    public const uint JOY_CAL_READALWAYS = 0x00010000;

    /// <summary></summary>
    public const uint JOY_CAL_READXYONLY = 0x00020000;

    /// <summary></summary>
    public const uint JOY_CAL_READ3 = 0x00040000;

    /// <summary></summary>
    public const uint JOY_CAL_READ4 = 0x00080000;

    /// <summary></summary>
    public const uint JOY_CAL_READXONLY = 0x00100000;

    /// <summary></summary>
    public const uint JOY_CAL_READYONLY = 0x00200000;

    /// <summary></summary>
    public const uint JOY_CAL_READ5 = 0x00400000;

    /// <summary></summary>
    public const uint JOY_CAL_READ6 = 0x00800000;

    /// <summary></summary>
    public const uint JOY_CAL_READZONLY = 0x00800000;

    /// <summary></summary>
    public const uint JOY_CAL_READRONLY = 0x02000000;

    /// <summary></summary>
    public const uint JOY_CAL_READUONLY = 0x04000000;

    /// <summary></summary>
    public const uint JOY_CAL_READVONLY = 0x08000000;

    /// <summary></summary>
    public const uint JOY_POVCENTERED = 65535;

    /// <summary></summary>
    public const uint JOY_POVFORWARD = 0;

    /// <summary></summary>
    public const uint JOY_POVRIGHT = 9000;

    /// <summary></summary>
    public const uint JOY_POVBACKWARD = 18000;

    /// <summary></summary>
    public const uint JOY_POVLEFT = 27000;

    public const uint JOY_CENTER = 0x7fff;

    #endregion

    #region · Types ·

    /// <summary>
    /// The JOYCAPS structure contains information about the joystick capabilities.
    /// </summary>
    /// <seealso cref="WinMM.JOYINFO"/>
    /// <seealso cref="WinMM.JOYINFOEX"/>
    /// <seealso cref="WinMM.joySetCapture"/>
    [StructLayout(LayoutKind.Sequential)]
    public struct JOYCAPS
    {
      /// <summary>
      /// Manufacturer identifier. Manufacturer identifiers are defined in Manufacturer and Product Identifiers.
      /// </summary>
      public ushort wMid;

      /// <summary>
      /// Product identifier. Product identifiers are defined in Manufacturer and Product Identifiers.
      /// </summary>
      public ushort wPid;

      /// <summary>
      /// Null-terminated string containing the joystick product name.
      /// </summary>
      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
      public string szPname;

      /// <summary>
      /// Minimum X-coordinate.
      /// </summary>
      public uint wXmin;

      /// <summary>
      /// Maximum X-coordinate.
      /// </summary>
      public uint wXmax;

      /// <summary>
      /// Minimum Y-coordinate.
      /// </summary>
      public uint wYmin;

      /// <summary>
      /// Maximum Y-coordinate.
      /// </summary>
      public uint wYmax;

      /// <summary>
      /// Minimum Z-coordinate.
      /// </summary>
      public uint wZmin;

      /// <summary>
      /// Maximum Z-coordinate.
      /// </summary>
      public uint wZmax;

      /// <summary>
      /// Number of joystick buttons.
      /// </summary>
      public uint wNumButtons;

      /// <summary>
      /// Smallest polling frequency supported when captured by the <see cref="joySetCapture"/> function.
      /// </summary>
      public uint wPeriodMin;

      /// <summary>
      /// Largest polling frequency supported when captured by <see cref="joySetCapture"/>.
      /// </summary>
      public uint wPeriodMax;

      /// <summary>
      /// Minimum rudder value. The rudder is a fourth axis of movement.
      /// </summary>
      public uint wRmin;

      /// <summary>
      /// Maximum rudder value. The rudder is a fourth axis of movement.
      /// </summary>
      public uint wRmax;

      /// <summary>
      /// Minimum u-coordinate (fifth axis) values.
      /// </summary>
      public uint wUmin;

      /// <summary>
      /// Maximum u-coordinate (fifth axis) values.
      /// </summary>
      public uint wUmax;
      /// <summary>
      /// Minimum v-coordinate (sixth axis) values.
      /// </summary>
      public uint wVmin;

      /// <summary>
      /// Maximum v-coordinate (sixth axis) values.
      /// </summary>
      public uint wVmax;

      /// <summary>
      /// Joystick capabilities The following flags define individual capabilities that a joystick might have:
      /// </summary>
      /// <remarks>
      /// <see cref="JOYCAPS_HASZ"/> - Joystick has z-coordinate information.
      /// <see cref="JOYCAPS_HASR"/> - Joystick has rudder (fourth axis) information.
      /// <see cref="JOYCAPS_HASU"/> - Joystick has u-coordinate (fifth axis) information.
      /// <see cref="JOYCAPS_HASV"/> - Joystick has v-coordinate (sixth axis) information.
      /// <see cref="JOYCAPS_HASPOV"/> - Joystick has point-of-view information.
      /// <see cref="JOYCAPS_POV4DIR"/> - Joystick point-of-view supports discrete values (centered, forward, backward, left, and right).
      /// <see cref="JOYCAPS_POVCTS"/> - Joystick point-of-view supports continuous degree bearings.
      /// </remarks>
      public uint wCaps;

      /// <summary>
      /// Maximum number of axes supported by the joystick.
      /// </summary>
      public uint wMaxAxes;

      /// <summary>
      /// Number of axes currently in use by the joystick.
      /// </summary>
      public uint wNumAxes;

      /// <summary>
      /// Maximum number of buttons supported by the joystick.
      /// </summary>
      public uint wMaxButtons;

      /// <summary>
      /// Null-terminated string containing the registry key for the joystick.
      /// </summary>
      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
      public string szRegKey;

      /// <summary>
      /// Null-terminated string identifying the joystick driver OEM.
      /// </summary>
      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
      public string szOEMVxD;
    }

    /// <summary>
    /// The JOYINFO structure contains information about the joystick position and button state.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct JOYINFO
    {
      /// <summary>
      /// Current X-coordinate.
      /// </summary>
      public uint wXpos;

      /// <summary>
      /// Current Y-coordinate.
      /// </summary>
      public uint wYpos;

      /// <summary>
      /// Current Z-coordinate.
      /// </summary>
      public uint wZpos;

      /// <summary>
      /// Current state of joystick buttons.
      /// </summary>
      /// <remarks>
      /// <para>According to one or more of the following values:</para>
      /// <para>
      /// <see cref="JOY_BUTTON1"/> - First joystick button is pressed.
      /// <see cref="JOY_BUTTON2"/> - Second joystick button is pressed.
      /// <see cref="JOY_BUTTON3"/> - Third joystick button is pressed.
      /// <see cref="JOY_BUTTON4"/> - Fourth joystick button is pressed.
      /// </para>
      /// </remarks>
      public uint wButtons;
    }

    /// <summary>
    /// The JOYINFOEX structure contains extended information about the joystick position, point-of-view position, and button state.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The value of the dwSize member is also used to identify the version number for the structure when it's passed to the <see cref="joyGetPosEx"/> function.
    /// </para>
    /// <para>
    /// Most devices with a point-of-view control have only five positions. When the JOY_RETURNPOV flag is set, these positions are reported by using the following constants:
    /// </para>
    /// <para>
    /// <see cref="JOY_POVBACKWARD"/> - Point-of-view hat is pressed backward. The value 18,000 represents an orientation of 180.00 degrees (to the rear).
    /// <see cref="JOY_POVCENTERED"/> - Point-of-view hat is in the neutral position. The value -1 means the point-of-view hat has no angle to report.
    /// <see cref="JOY_POVFORWARD"/> - Point-of-view hat is pressed forward. The value 0 represents an orientation of 0.00 degrees (straight ahead).
    /// <see cref="JOY_POVLEFT"/> - Point-of-view hat is being pressed to the left. The value 27,000 represents an orientation of 270.00 degrees (90.00 degrees to the left).
    /// <see cref="JOY_POVRIGHT"/> - Point-of-view hat is pressed to the right. The value 9,000 represents an orientation of 90.00 degrees (to the right).
    /// </para>
    /// <para>
    /// The default joystick driver currently supports these five discrete directions. If an application can accept only the defined point-of-view values, it must use the JOY_RETURNPOV flag. If an application can accept other degree readings, it should use the JOY_RETURNPOVCTS flag to obtain continuous data if it is available. The JOY_RETURNPOVCTS flag also supports the JOY_POV constants used with the JOY_RETURNPOV flag.
    /// </para>
    /// </remarks>
    /// <seealso cref="joyGetPosEx"/>
    [StructLayout(LayoutKind.Sequential)]
    public struct JOYINFOEX
    {
      /// <summary>
      /// Size, in bytes, of this structure.
      /// </summary>
      public uint dwSize;

      /// <summary>
      /// Flags indicating the valid information returned in this structure. Members that do not contain valid information are set to zero.
      /// </summary>
      /// <remarks>
      /// <para>
      /// <see cref="JOY_RETURNALL"/> - Equivalent to setting all of the JOY_RETURN bits except JOY_RETURNRAWDATA.
      /// <see cref="JOY_RETURNBUTTONS"/> - The dwButtons member contains valid information about the state of each joystick button.
      /// <see cref="JOY_RETURNCENTERED"/> - Centers the joystick neutral position to the middle value of each axis of movement.
      /// <see cref="JOY_RETURNPOV"/> - The dwPOV member contains valid information about the point-of-view control, expressed in discrete units.
      /// <see cref="JOY_RETURNPOVCTS"/> - The dwPOV member contains valid information about the point-of-view control expressed in continuous, one-hundredth degree units.
      /// <see cref="JOY_RETURNR"/> - The dwRpos member contains valid rudder pedal data. This information represents another (fourth) axis.
      /// <see cref="JOY_RETURNRAWDATA"/>	- Data stored in this structure is uncalibrated joystick readings.
      /// <see cref="JOY_RETURNU"/> - The dwUpos member contains valid data for a fifth axis of the joystick, if such an axis is available, or returns zero otherwise.
      /// <see cref="JOY_RETURNV"/> - The dwVpos member contains valid data for a sixth axis of the joystick, if such an axis is available, or returns zero otherwise.
      /// <see cref="JOY_RETURNX"/> - The dwXpos member contains valid data for the x-coordinate of the joystick.
      /// <see cref="JOY_RETURNY"/> - The dwYpos member contains valid data for the y-coordinate of the joystick.
      /// <see cref="JOY_RETURNZ"/> - The dwZpos member contains valid data for the z-coordinate of the joystick.
      /// <see cref="JOY_USEDEADZONE"/> - Expands the range for the neutral position of the joystick and calls this range the dead zone. The joystick driver returns a constant value for all positions in the dead zone.
      /// </para>
      /// <para>
      /// The following flags provide data to calibrate a joystick and are intended for custom calibration applications.
      /// </para>
      /// <para>
      /// <see cref="JOY_CAL_READ3"/> - Read the x-, y-, and z-coordinates and store the raw values in dwXpos, dwYpos, and dwZpos.
      /// <see cref="JOY_CAL_READ4"/> - Read the rudder information and the x-, y-, and z-coordinates and store the raw values in dwXpos, dwYpos, dwZpos, and dwRpos.
      /// <see cref="JOY_CAL_READ5"/> - Read the rudder information and the x-, y-, z-, and u-coordinates and store the raw values in dwXpos, dwYpos, dwZpos, dwRpos, and dwUpos.
      /// <see cref="JOY_CAL_READ6"/> - Read the raw v-axis data if a joystick mini driver is present that will provide the data. Returns zero otherwise.
      /// <see cref="JOY_CAL_READALWAYS"/> - Read the joystick port even if the driver does not detect a device.
      /// <see cref="JOY_CAL_READRONLY"/> - Read the rudder information if a joystick mini-driver is present that will provide the data and store the raw value in dwRpos. Return zero otherwise.
      /// <see cref="JOY_CAL_READXONLY"/> - Read the x-coordinate and store the raw (uncalibrated) value in dwXpos.
      /// <see cref="JOY_CAL_READXYONLY"/> - Reads the x- and y-coordinates and place the raw values in dwXpos and dwYpos.
      /// <see cref="JOY_CAL_READYONLY"/> - Reads the y-coordinate and store the raw value in dwYpos.
      /// <see cref="JOY_CAL_READZONLY"/> - Read the z-coordinate and store the raw value in dwZpos.
      /// <see cref="JOY_CAL_READUONLY"/> - Read the u-coordinate if a joystick mini-driver is present that will provide the data and store the raw value in dwUpos. Return zero otherwise.
      /// <see cref="JOY_CAL_READVONLY"/> - Read the v-coordinate if a joystick mini-driver is present that will provide the data and store the raw value in dwVpos. Return zero otherwise.
      /// </para>
      /// </remarks>
      public uint dwFlags;

      /// <summary>
      /// Current X-coordinate.
      /// </summary>
      public uint dwXpos;

      /// <summary>
      /// Current Y-coordinate.
      /// </summary>
      public uint dwYpos;

      /// <summary>
      /// Current Z-coordinate.
      /// </summary>
      public uint dwZpos;

      /// <summary>
      /// Current position of the rudder or fourth joystick axis.
      /// </summary>
      public uint dwRpos;

      /// <summary>
      /// Current fifth axis position.
      /// </summary>
      public uint dwUpos;

      /// <summary>
      /// Current sixth axis position.
      /// </summary>
      public uint dwVpos;

      /// <summary>
      /// Current state of the 32 joystick buttons. The value of this member can be set to any combination of JOY_BUTTONn flags, where n is a value in the range of 1 through 32 corresponding to the button that is pressed.
      /// </summary>
      public uint dwButtons;

      /// <summary>
      /// Current button number that is pressed.
      /// </summary>
      public uint dwButtonNumber;

      /// <summary>
      /// Current position of the point-of-view control. Values for this member are in the range 0 through 35,900. These values represent the angle, in degrees, of each view multiplied by 100.
      /// </summary>
      public uint dwPOV;

      /// <summary>
      /// Reserved; do not use.
      /// </summary>
      public uint dwReserved1;

      /// <summary>
      /// Reserved; do not use.
      /// </summary>
      public uint dwReserved2;
    }

    #endregion

    #region · Methods ·

    /// <summary>
    /// The joyGetDevCaps function queries a joystick to determine its capabilities.
    /// </summary>
    /// <param name="uJoyID">
    /// Identifier of the joystick to be queried. Valid values for uJoyID range from -1 to 15. A value of -1 enables retrieval of the szRegKey member of the JOYCAPS structure whether a device is present or not. For Windows NT 4.0, valid values are limited to zero (JOYSTICKID1) and JOYSTICKID2.
    /// </param>
    /// <param name="pjc">
    /// Pointer to a <see cref="JOYCAPS"/> structure to contain the capabilities of the joystick.
    /// </param>
    /// <param name="cbjc"> 
    /// Size, in bytes, of the JOYCAPS structure.
    /// </param>
    /// <returns>
    /// <para>
    /// Returns JOYERR_NOERROR if successful or one of the following error values:
    /// </para>
    /// <para>
    /// <see cref="MMSYSERR_NODRIVER"/> - The joystick driver is not present. Windows NT/2000/XP: The specified joystick identifier is invalid.
    /// <see cref="MMSYSERR_INVALPARAM"/> - An invalid parameter was passed. Windows 95/98/Me: The specified joystick identifier is invalid.
    /// </para>
    /// </returns>
    /// <remarks>
    /// <para>
    /// Use the <see cref="joyGetNumDevs"/> function to determine the number of joystick devices supported by the driver.
    /// </para>
    /// </remarks>
    /// <seealso cref="JOYCAPS"/>
    /// <seealso cref="joyGetNumDevs"/>
    [DllImport(DLLName)]
    public static extern Int32 joyGetDevCaps(UIntPtr uJoyID, ref JOYCAPS pjc, uint cbjc);

    /// <summary>
    /// The joyGetNumDevs function queries the joystick driver for the number of joysticks it supports.
    /// </summary>
    /// <returns>
    /// The joyGetNumDevs function returns the number of joysticks supported by the current driver or zero if no driver is installed.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Use the <see cref="joyGetPos"/> function to determine whether a given joystick is physically attached to the system. If the specified joystick is not connected, joyGetPos returns a <see cref="JOYERR_UNPLUGGED"/> error value.
    /// </para>
    /// </remarks>
    [DllImport(DLLName)]
    public static extern uint joyGetNumDevs();

    /// <summary>
    /// The joyGetPos function queries a joystick for its position and button status.
    /// </summary>
    /// <param name="uJoyID">
    /// Identifier of the joystick to be queried. Valid values for uJoyID range from 0 to 15.
    /// </param>
    /// <param name="pji">
    /// Pointer to a <see cref="JOYINFO"/> structure that contains the position and button status of the joystick.
    /// </param>
    /// <returns>
    /// Returns <see cref="JOYERR_NOERROR"/> if successful or one of the following error values.
    /// <para>
    /// <see cref="MMSYSERR_NODRIVER"/> - The joystick driver is not present.
    /// <see cref="MMSYSERR_INVALPARAM"/> - An invalid parameter was passed.
    /// <see cref="JOYERR_UNPLUGGED"/> - The specified joystick is not connected to the system.
    /// </para>
    /// </returns>
    /// <remarks>
    /// For devices that have four to six axes of movement, a point-of-view control, or more than four buttons, use the <see cref="joyGetPosEx"/> function.
    /// </remarks>
    [DllImport(DLLName)]
    public static extern uint joyGetPos(uint uJoyID, ref JOYINFO pji);

    /// <summary>
    /// The joyGetPosEx function queries a joystick for its position and button status.
    /// </summary>
    /// <param name="uJoyID">
    /// Identifier of the joystick to be queried. Valid values for uJoyID range from 0 to 15.
    /// </param>
    /// <param name="pji">
    /// Pointer to a <see cref="JOYINFOEX"/> structure that contains extended position information and button status of the joystick. You must set the dwSize and dwFlags members or joyGetPosEx will fail. The information returned from joyGetPosEx depends on the flags you specify in dwFlags.
    /// </param>
    /// <returns>
    /// Returns JOYERR_NOERROR if successful or one of the following error values.
    /// <para>
    /// Returns JOYERR_NOERROR if successful or one of the following error values.
    /// </para>
    /// <para>
    /// <see cref="MMSYSERR_NODRIVER"/> - The joystick driver is not present.
    /// <see cref="MMSYSERR_INVALPARAM"/> - An invalid parameter was passed.
    /// <see cref="JOYERR_UNPLUGGED"/> - The specified joystick is not connected to the system.
    /// <see cref="JOYERR_PARMS"/> - The specified joystick identifier is invalid.
    /// </para>
    /// </returns>
    /// <remarks>
    /// This function provides access to extended devices such as rudder pedals, point-of-view hats, devices with a large number of buttons, and coordinate systems using up to six axes. For joystick devices that use three axes or fewer and have fewer than four buttons, use the joyGetPos function.
    /// </remarks>
    [DllImport(DLLName)]
    public static extern uint joyGetPosEx(uint uJoyID, ref JOYINFOEX pji);

    #endregion
  }
}
