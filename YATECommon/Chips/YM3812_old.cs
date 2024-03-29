﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YATECommon.Chips
{
    public class YM3812_old
    {
        //public static Timer.emu_timer[] timer;
        //public delegate void ym3812_delegate(LineState linestate);
        //public static ym3812_delegate ym3812handler;
        public static void IRQHandler_3812(int irq)
        {
            //ym3812handler(irq != 0 ? LineState.ASSERT_LINE : LineState.CLEAR_LINE);
        }
        public static void timer_callback_3812_0()
        {
            //TODO:timer FMOpl.ym3812_timer_over(0);
        }
        public static void timer_callback_3812_1()
        {
            //TODO:timer  FMOpl.ym3812_timer_over(1);
        }   /*
        private static void TimerHandler_3812(int c, Atime period)
        {
            if (Attotime.attotime_compare(period, Attotime.ATTOTIME_ZERO) == 0)
            {
                Timer.timer_enable(timer[c], false);
            }
            else
            {
                Timer.timer_adjust_periodic(timer[c], period, Attotime.ATTOTIME_NEVER);
            }
        }     */
        public static void _stream_update_3812(int interval)
        {
           // Sound.ym3812stream.stream_update();
        }
        public static void ym3812_start(int clock)
        {         
            YM3812.tl_tab = new int[0x1800];
            YM3812.sin_tab = new uint[0x1000];
            //TODO: timer timer = new Timer.emu_timer[2];
            int rate = clock / 72;
            YM3812.ym3812_init(0, clock, rate);
            //TODO: timer FMOpl.ym3812_set_timer_handler(TimerHandler_3812);
            YM3812.ym3812_set_irq_handler(IRQHandler_3812);
            YM3812.ym3812_set_update_handler(_stream_update_3812);
            //TODO: timer timer[0] = Timer.timer_alloc_common(timer_callback_3812_0, "timer_callback_3812_0", false);
            //TODO: timer timer[1] = Timer.timer_alloc_common(timer_callback_3812_1, "timer_callback_3812_1", false);
        }
        public static void ym3812_control_port_0_w(byte data)
        {
            YM3812.ym3812_write(0, data);
        }
        public static void ym3812_write_port_0_w(byte data)
        {
            YM3812.ym3812_write(1, data);
        }
    }
}
