using System;

namespace YATECommon.Chips
{
  public class I8255
  {
    #region · Constants · 

    /* mode select or interrupt control */
    private const byte I8255_CTRL_CONTROL = (1 << 7);
    private const byte I8255_CTRL_CONTROL_MODE = (1 << 7);
    private const byte I8255_CTRL_CONTROL_BIT = (0);

    /* port A input/output select */
    private const byte I8255_CTRL_A = (1 << 4);
    private const byte I8255_CTRL_A_INPUT = (1 << 4);
    private const byte I8255_CTRL_A_OUTPUT = (0);

    /* port B input/output select */
    private const byte I8255_CTRL_B = (1 << 1);
    private const byte I8255_CTRL_B_INPUT = (1 << 1);
    private const byte I8255_CTRL_B_OUTPUT = (0);

    /* port C (lower) input/output select */
    private const byte I8255_CTRL_CLO = (1 << 0);
    private const byte I8255_CTRL_CLO_INPUT = (1 << 0);
    private const int I8255_CTRL_CLO_OUTPUT = (0);

    /* port C (upper) input/output select */
    private const byte I8255_CTRL_CHI = (1 << 3);
    private const byte I8255_CTRL_CHI_INPUT = (1 << 3);
    private const byte I8255_CTRL_CHI_OUTPUT = (0);

    /* set/reset bit (for I8255_CTRL_CONTROL_BIT) */
    private const int I8255_CTRL_BIT = (1 << 0);
    private const int I8255_CTRL_BIT_SET = (1 << 0);
    private const int I8255_CTRL_BIT_RESET = (0);

    public enum PinNames : int
    {
      PA0 = 0,
      PA1 = 1,
      PA2 = 2,
      PA3 = 3,
      PA4 = 4,
      PA5 = 5,
      PA6 = 6,
      PA7 = 7,

      PB0 = 8,
      PB1 = 9,
      PB2 = 10,
      PB3 = 11,
      PB4 = 12,
      PB5 = 13,
      PB6 = 14,
      PB7 = 15,

      PC0 = 16,
      PC1 = 17,
      PC2 = 18,
      PC3 = 19,
      PC4 = 20,
      PC5 = 21,
      PC6 = 22,
      PC7 = 23
    };

    #endregion

    #region · Types ·

    public struct PortState
    {
      public byte Out;
      public byte In;
    }

    public class PortPins
    {
      private I8255 m_ppi;

      public PortPins(I8255 in_ppi)
      {
        m_ppi = in_ppi;
      }

      public bool this[PinNames in_pin]
      {
        get
        {
          int port = (int)in_pin / 8;
          int pin = (int)in_pin % 8;

          switch(port)
          {
            case 0:
              return (m_ppi.PortA & (1 << pin)) != 0;

            case 1:
              return (m_ppi.PortB & (1 << pin)) != 0;

            case 2:
              return (m_ppi.PortC & (1 << pin)) != 0;
          }

          return false;
        }
        set
        {
          int port = (int)in_pin / 8;
          int pin = (int)in_pin % 8;
          switch (port)
          {
            case 0:
              m_ppi.m_port_A.In = (byte)(m_ppi.m_port_A.In & ~(1 << pin) | (value ? (1 << pin) : 0));
              break;

            case 1:
              m_ppi.m_port_B.In = (byte)(m_ppi.m_port_B.In & ~(1 << pin) | (value ? (1 << pin) : 0));
              break;

            case 2:
              m_ppi.m_port_C.In = (byte)(m_ppi.m_port_C.In & ~(1 << pin) | (value ? (1 << pin) : 0));
              break;
          }
        }
      }
    }

    public byte PortA
    {
      get
      {
        if ((m_port_control & I8255_CTRL_A) == I8255_CTRL_A_OUTPUT)
          return m_port_A.Out;
        else
          return m_port_A.In;
      }
      set
      {
        m_port_A.In = value;
      }
    }

    public byte PortB
    {
      get
      {
        if ((m_port_control & I8255_CTRL_B) == I8255_CTRL_B_OUTPUT)
          return m_port_B.Out;
        else
          return m_port_B.In;
      }
      set
      {
        m_port_B.In = value;
      }
    }

    public byte PortC
    {
      get
      {
        byte data = m_port_C.In;
        if ((m_port_control & I8255_CTRL_CHI) == I8255_CTRL_CHI_OUTPUT)
        {
          data = (byte)((data & 0x0F) | (m_port_C.Out & 0xF0));
        }
        if ((m_port_control & I8255_CTRL_CLO) == I8255_CTRL_CLO_OUTPUT)
        {
          data = (byte)((data & 0xF0) | (m_port_C.Out & 0x0F));
        }
        return data;
      }
      set
      {
        m_port_C.In = value;
      }
    }

    public class PortChangedEventArgs : EventArgs
    {
      public byte NewValue { get; set; }
      public byte OldValue { get; set; }
    }

    public delegate void PortChangedEventHandler(Object sender, PortChangedEventArgs e);

    #endregion

    #region · Data members ·

    private byte m_port_control;
    private PortState m_port_A;
    private PortState m_port_B;
    private PortState m_port_C;

    private PortChangedEventArgs m_port_changed_event_arg;
    #endregion

    #region · Properties ·

    public PortPins Pins { get; private set; }

    public event PortChangedEventHandler PortAChanged;
    public event PortChangedEventHandler PortBChanged;
    public event PortChangedEventHandler PortCChanged;

    public bool IsPortAOutput => (m_port_control & I8255_CTRL_A) == I8255_CTRL_A_OUTPUT;
    public bool IsPortBOutput => (m_port_control & I8255_CTRL_B) == I8255_CTRL_B_OUTPUT;
    public bool IsPortCLoOutput => (m_port_control & I8255_CTRL_CLO) == I8255_CTRL_CLO_OUTPUT;
    public bool IsPortCHiOutput => (m_port_control & I8255_CTRL_CHI) == I8255_CTRL_CHI_OUTPUT;

    #endregion


    public I8255()
    {
      Pins = new PortPins(this);
      m_port_changed_event_arg = new PortChangedEventArgs();

      Reset();
    }

    public void Reset()
    {
      m_port_control = I8255_CTRL_CONTROL_MODE |
                   I8255_CTRL_CLO_INPUT |
                   I8255_CTRL_CHI_INPUT |
                   I8255_CTRL_B_INPUT |
                   I8255_CTRL_A_INPUT;

      byte old_value = 0;

      old_value = m_port_A.Out;
      m_port_A.Out = 0;
      OnPortAChanged(old_value);

      old_value = m_port_B.Out;
      m_port_B.Out = 0;
      OnPortBChanged(old_value);

      old_value = m_port_C.Out;
      m_port_C.Out = 0;
      OnPortCChanged(old_value);
    }

    public void PortWrite(ushort in_address, byte in_data)
    {
      byte old_value;

      switch (in_address & 0x03)
      {
        case 0: /* write to port A */
          old_value = m_port_A.Out;
          m_port_A.Out = in_data;
          OnPortAChanged(old_value);
          break;

        case 1: /* write to port B */
          old_value = m_port_A.Out;
          m_port_B.Out = in_data;
          OnPortBChanged(old_value);
          break;

        case 2: /* write to port C */
          old_value = m_port_C.Out;
          m_port_C.Out = in_data;
          OnPortCChanged(old_value);
          break;

        case 3: /* control operation*/
          if ((in_data & I8255_CTRL_CONTROL) == I8255_CTRL_CONTROL_MODE)
          {
            /* set port mode */
            byte old_mode = m_port_control;
            m_port_control = in_data;

            m_port_A.Out = 0;
            m_port_B.Out = 0;
            m_port_C.Out = 0;

            if (((old_mode & I8255_CTRL_A) != I8255_CTRL_A_OUTPUT) && ((m_port_control & I8255_CTRL_A) == I8255_CTRL_A_OUTPUT))
              OnPortAChanged(m_port_A.In);

            if (((old_mode & I8255_CTRL_B) != I8255_CTRL_B_OUTPUT) && ((m_port_control & I8255_CTRL_B) == I8255_CTRL_B_OUTPUT))
              OnPortBChanged(m_port_B.In);

            if (((old_mode & I8255_CTRL_CHI) != I8255_CTRL_CHI_OUTPUT) && ((m_port_control & I8255_CTRL_CHI) == I8255_CTRL_CHI_OUTPUT) ||
              ((old_mode & I8255_CTRL_CLO) != I8255_CTRL_CLO_OUTPUT) && ((m_port_control & I8255_CTRL_CLO) == I8255_CTRL_CLO_OUTPUT))
            {
              OnPortCChanged(m_port_C.In);
            }
          }
          else
          {
            old_value = m_port_C.Out;

            /* set/clear single bit in port C */
            byte mask = (byte)(1 << ((in_data >> 1) & 7));
            if ((in_data & I8255_CTRL_BIT) == I8255_CTRL_BIT_SET)
            {
              m_port_C.Out |= mask;
            }
            else
            {
              m_port_C.Out &= (byte)~mask;
            }

            // call port changed event handler
            OnPortCChanged(old_value);
          }
          break;
      }
    }

    /* read a value from the PPI */
    public void PortRead(ushort in_address, ref byte inout_data)
    {
      byte data = 0xff;

      switch (in_address & 0x03)
      {
        case 0: /* read from port A */
          if ((m_port_control & I8255_CTRL_A) == I8255_CTRL_A_OUTPUT)
          {
            data = m_port_A.Out;
          }
          else
          {
            data = m_port_A.In;
          }
          break;

        case 1: /* read from port B */
          if ((m_port_control & I8255_CTRL_B) == I8255_CTRL_B_OUTPUT)
          {
            data = m_port_B.Out;
          }
          else
          {
            data = m_port_B.In;
          }
          break;

        case 2: /* read from port C */
          data = m_port_C.In;
          if ((m_port_control & I8255_CTRL_CHI) == I8255_CTRL_CHI_OUTPUT)
          {
            data = (byte)((data & 0x0F) | (m_port_C.Out & 0xF0));
          }
          if ((m_port_control & I8255_CTRL_CLO) == I8255_CTRL_CLO_OUTPUT)
          {
            data = (byte)((data & 0xF0) | (m_port_C.Out & 0x0F));
          }
          break;

        case 3: /* read control word */
          data = m_port_control;
          break;
      }

      inout_data = data;
    }


    private void OnPortAChanged(byte in_old_value)
    {
      // generate event only when port is set to putput direction
      if ((m_port_control & I8255_CTRL_A) == I8255_CTRL_A_OUTPUT)
      {
        m_port_changed_event_arg.NewValue = m_port_A.Out;
        m_port_changed_event_arg.OldValue = in_old_value;
        PortAChanged?.Invoke(this, m_port_changed_event_arg);
      }
    }

    private void OnPortBChanged(byte in_old_value)
    {
      // generate event only when port is set to putput direction
      if ((m_port_control & I8255_CTRL_B) == I8255_CTRL_B_OUTPUT)
      {
        m_port_changed_event_arg.NewValue = m_port_B.Out;
        m_port_changed_event_arg.OldValue = in_old_value;
        PortBChanged?.Invoke(this, m_port_changed_event_arg);
      }
    }

    private void OnPortCChanged(byte in_old_value)
    {
      // generate event only when port is set to putput direction
      if ((m_port_control & I8255_CTRL_CHI) == I8255_CTRL_CHI_OUTPUT || (m_port_control & I8255_CTRL_CLO) == I8255_CTRL_CLO_OUTPUT)
      {
        m_port_changed_event_arg.NewValue = m_port_C.Out;
        m_port_changed_event_arg.OldValue = in_old_value;
        PortCChanged?.Invoke(this, m_port_changed_event_arg);
      }
    }
  }
}