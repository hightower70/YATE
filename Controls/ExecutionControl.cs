using Models.Z80Emulator;
using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using TVCHardware;

namespace TVCEmu.Controls
{
	public class ExecutionControl : IDisposable
	{
		#region · Constants · 
		private int DebugEventPeriod = 200; // Debug event preiod in ms
		#endregion

		#region · Types · 

		public enum ExecutionStateRequest
		{
			NoChange,

			Run,
			RunFullSpeed,
			Pause,
			Restore,
			Reset
		}

		public enum ExecutionState
		{
			Paused,
			Running,
			RunningFullSpeed
		}

		public delegate void DebuggerBreakEventDelegate(TVComputer in_sender);

		#endregion

		#region · Data members · 

		// thread variables
		private Thread m_stream_thread;
		private AutoResetEvent m_thread_event;
		private SynchronizationContext m_context;
		private bool m_thread_running;
		private uint m_cpu_t_cycle;						// current CPU cycle
		private uint m_target_cycle;					// desired CPU cycle
		private uint m_instruction_t_cycle;
		private ushort m_instruction_start_pc; // address of the current instruction's first byte

		private ExecutionStateRequest m_execution_state_request;
		private AutoResetEvent m_execution_state_changed_event;

		private ExecutionState m_execution_state;
		private ExecutionState m_last_execution_state;

		private DateTime m_debug_event_timestamp;

		public readonly Window ParentWindow;
		#endregion

		#region · Constructor ·

		/// <summary>
		/// Default constructor
		/// </summary>
		public ExecutionControl(Window in_parent_window)
		{
			ParentWindow = in_parent_window;

			TVC = new TVComputer();
			ExecutionHistory = new ExecutionHistoryCollection(2);

			m_stream_thread = null;
			m_thread_event = new AutoResetEvent(false);
			m_context = SynchronizationContext.Current;
			m_thread_running = false;

			m_execution_state = ExecutionState.Running;
			m_last_execution_state = ExecutionState.Running;

			m_execution_state_request = ExecutionStateRequest.NoChange;
			m_execution_state_changed_event = new AutoResetEvent(false);

			DebugStepIntoCommand = new ExecutionControlCommand(DebugStepExecute);
			DebugPauseCommand = new ExecutionControlCommand(DebugPauseExecute);
			DebugRunCommand = new ExecutionControlCommand(DebugRunExecute);
			DebugResetCommand = new ExecutionControlCommand(DebugResetExecute);
			DebugRunFullSpeedCommand = new ExecutionControlCommand(DebugRunFullSpeedExecute);
		}
		#endregion

		#region · Commands ·

		// Run
		public ExecutionControlCommand DebugRunCommand { get; private set; }

		public void DebugRunExecute(object parameter)
		{
			ChangeExecutionState(ExecutionStateRequest.Run);
		}

		// Run full speed
		public ExecutionControlCommand DebugRunFullSpeedCommand { get; private set; }

		public void DebugRunFullSpeedExecute(object parameter)
		{
			ChangeExecutionState(ExecutionStateRequest.RunFullSpeed);
		}

		// Reset command
		public ExecutionControlCommand DebugResetCommand { get; private set; }

		public void DebugResetExecute(object parameter)
		{
			ChangeExecutionState(ExecutionStateRequest.Reset);
		}

		// Pause
		public ExecutionControlCommand DebugPauseCommand { get; private set; }

		public void DebugPauseExecute(object parameter)
		{
			ChangeExecutionState(ExecutionStateRequest.Pause);
		}

		// Step into
		public ExecutionControlCommand DebugStepIntoCommand { get; private set; }

		public void DebugStepExecute(object parameter)
		{
			StepInto();
		}

		#endregion

		#region · Properties · 

		public TVComputer TVC { get; private set; }
		public ExecutionHistoryCollection ExecutionHistory { get; private set; }

		public CommandBinding StepCommand
		{
			get; private set;
		}

		#endregion

		public DebuggerBreakEventDelegate DebuggerBreakEvent;

		public void Initialize()
		{
			TVC.Initialize();

			DebuggerBreakEvent?.Invoke(TVC);
		}

		public void StepInto()
		{
			ushort instruction_start_pc = TVC.CPU.Registers.PC;
			uint TCycle = 0;
			do
			{
				TCycle += TVC.CPU.Step();
			} while (!TVC.CPU.InstructionDone || instruction_start_pc == TVC.CPU.Registers.PC);

			AddInstructionToExecutionHistory(instruction_start_pc, TCycle);

			DebuggerBreakEvent?.Invoke(TVC);
		}

		public void StepOver()
		{

		}

		#region · Thread control functions ·

		/// <summary>
		/// Starts simulation thread
		/// </summary>
		public void Start()
		{
			m_thread_running = true;
			m_stream_thread = new Thread(new ThreadStart(SimulationThread));
			m_stream_thread.Name = "TVCSimulationThread";
			m_stream_thread.Start();
		}

		/// <summary>
		/// Stops simulation thread
		/// </summary>
		public void Stop()
		{
			if (m_stream_thread == null)
				return;

			m_thread_running = false;
			m_thread_event.Set();

			m_stream_thread.Join();

			m_stream_thread = null;
		}

		public void ChangeExecutionState(ExecutionStateRequest in_request)
		{
			if (in_request == ExecutionStateRequest.NoChange)
				return;

			m_execution_state_changed_event.Reset();

			m_execution_state_request = in_request;
			m_thread_event.Set();

			m_execution_state_changed_event.WaitOne();
		}

		#endregion

		#region · Simulation thread ·

		private Stopwatch m_stopwatch;

		Z80Disassembler m_disassembler;
	
		private byte ReadMemory(ushort in_address)
		{
			return TVC.Memory.Read(in_address);
		}

		private void SimulationThread()
		{
			double delay_time;

			m_disassembler = new Z80Disassembler();
			m_disassembler.ReadByte = ReadMemory;

			m_stopwatch = new Stopwatch();

			m_instruction_start_pc = TVC.CPU.Registers.PC;
			m_instruction_t_cycle = 0;

			m_debug_event_timestamp = DateTime.Now;

			delay_time = 0;

			while (m_thread_running)
			{
				// handle execution state change
				switch(m_execution_state_request)
				{
					// change to pause state
					case ExecutionStateRequest.Pause:
						m_execution_state_request = ExecutionStateRequest.NoChange;
						m_last_execution_state = m_execution_state;
						m_execution_state = ExecutionState.Paused;
						m_execution_state_changed_event.Set();
						break;

					case ExecutionStateRequest.Restore:
						m_execution_state_request = ExecutionStateRequest.NoChange;
						m_execution_state = m_last_execution_state;
						m_execution_state_changed_event.Set();
						break;

					// change to running
					case ExecutionStateRequest.Run:
						m_execution_state_request = ExecutionStateRequest.NoChange;
						m_execution_state = ExecutionState.Running;
						m_execution_state_changed_event.Set();
						break;

					// full speed run
					case ExecutionStateRequest.RunFullSpeed:
						m_execution_state_request = ExecutionStateRequest.NoChange;
						m_execution_state = ExecutionState.RunningFullSpeed;
						m_execution_state_changed_event.Set();
						break;

					// resets computer
					case ExecutionStateRequest.Reset:
						m_execution_state_request = ExecutionStateRequest.NoChange;
						TVC.Reset();
						m_execution_state_changed_event.Set();
						break;

					// no change
					case ExecutionStateRequest.NoChange:
						// do nothing
						break;
				}

				// execute according the execution state
				switch (m_execution_state)
				{
					case ExecutionState.Running:
						{
							uint ellapsed_tick;

							m_stopwatch.Restart();
							ellapsed_tick = RunOneFrame();
							GenerateDebugEvent();
							m_stopwatch.Stop();

							delay_time += TVC.CPUTickToMillisec(ellapsed_tick) - m_stopwatch.Elapsed.TotalMilliseconds;

							if (delay_time > 0)
							{
								int delay = (int)Math.Truncate(delay_time);

								m_stopwatch.Restart();
								m_thread_event.WaitOne(delay);
								m_stopwatch.Stop();
								delay_time -= m_stopwatch.Elapsed.TotalMilliseconds;
							}
							else
							{
								// execution is behind the schedule -> no delay
								m_thread_event.WaitOne(0);

								delay_time = 0;
							}
						}
						break;

					case ExecutionState.RunningFullSpeed:
						RunOneFrame();
						GenerateDebugEvent();
						m_thread_event.WaitOne(0);
						break;

					case ExecutionState.Paused:
						m_thread_event.WaitOne(1000);
						break;
				}
			}
		}

		/// <summary>
		/// Runs simulation for one video frame
		/// </summary>
		private uint RunOneFrame()
		{
			uint current_instruction_t_cycle;
			uint frame_start_cycle = m_cpu_t_cycle;
			bool breakpoint_exit = false;

			while (!TVC.Video.RenderScanline() && m_thread_running && !breakpoint_exit)
			{
				m_target_cycle += 200;	 //TODO: calculate from video timing
				TVC.Memory.VideoMemAccessCount = 0;

				do
				{
					current_instruction_t_cycle = TVC.CPU.Step();
					m_cpu_t_cycle += current_instruction_t_cycle;
					m_instruction_t_cycle += current_instruction_t_cycle;

					if (TVC.CPU.InstructionDone)
					{
						AddInstructionToExecutionHistory(m_instruction_start_pc, m_instruction_t_cycle);

						m_instruction_start_pc = TVC.CPU.Registers.PC;
						m_instruction_t_cycle = 0;
								 
						if (TVC.CPU.Registers.PC == 0x0f1d)
						{
							breakpoint_exit = true;
							m_execution_state = ExecutionState.Paused;
						}	 
					}

					if (TVC.Interrupt.IsIntActive())
						m_cpu_t_cycle += (uint)TVC.CPU.Int();

				} while (((long)m_target_cycle - m_cpu_t_cycle) > 0 && m_thread_running && !breakpoint_exit);

				if (TVC.Memory.VideoMemAccessCount > 0)
				{
					m_cpu_t_cycle += (uint)(TVC.Memory.VideoMemAccessCount + 1);
				}

				TVC.PeriodicCallback();
			}

			// return frame total cycle
			return m_cpu_t_cycle - frame_start_cycle;
		}

		/// <summary>
		/// Generates debug event (refreshes debug information display)
		/// </summary>
		private void GenerateDebugEvent()
		{
			// generate debug event
			if (m_thread_running)
			{
				DateTime current_time = DateTime.Now;

				if ((current_time - m_debug_event_timestamp).TotalMilliseconds > DebugEventPeriod)
				{
					m_debug_event_timestamp = current_time;

					ExecutionHistory.UpdateHistory();

					m_context.Post(delegate
					{
						DebuggerBreakEvent?.Invoke(TVC);
					}, null);
				}
			}
		}


		/// <summary>
		/// Adds last instruction to the execution history 
		/// </summary>
		/// <param name="in_instruction_start_pc">PC where the instruction begins</param>
		/// <param name="in_instruction_t_cycle">T cycle used for execution</param>
		private void AddInstructionToExecutionHistory(ushort in_instruction_start_pc, uint in_instruction_t_cycle)
		{
			ExecutionHistoryEntry history_entry = ExecutionHistory.GetNextEmptySlot();
			history_entry.PC = in_instruction_start_pc;
			history_entry.TCycle = in_instruction_t_cycle;

			for (int i = 0; i < ExecutionHistoryEntry.HistoryEntryByteBufferLength; i++)
			{
				history_entry.Bytes[i] = TVC.Memory.Read((ushort)(in_instruction_start_pc + i));
			}
		}

		#endregion


		#region · Disposable interface ·

		bool disposed = false;

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		// Protected implementation of Dispose pattern.
		protected virtual void Dispose(bool disposing)
		{
			if (disposed)
				return;

			if (disposing)
			{
				Stop();
			}

			disposed = true;
		}

		#endregion
	}
}
