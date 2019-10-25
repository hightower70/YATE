using Models.Z80Emulator;
using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Input;
using TVCHardware;

namespace TVCEmu.Controls
{
	public class ExecutionControl : IDisposable
	{
		#region · Types · 

		public enum ExecutionState
		{
			Paused,
			Running,
			ResetAndRun,
			ResetAndPause
		}


		public delegate void DebuggerBreakEventDelegate(TVComputer in_sender);

		#endregion

		#region · Data members · 

		// thread variables
		private Thread m_stream_thread;
		private AutoResetEvent m_thread_event;
		private SynchronizationContext m_context;
		private bool m_thread_running;
		private uint m_cpu_cycle;
		private ExecutionState m_execution_state;
		#endregion

		#region · Constructor ·

		/// <summary>
		/// Default constructor
		/// </summary>
		public ExecutionControl()
		{
			TVC = new TVComputer();
			ExecutionHistory = new ExecutionHistoryCollection(2);

			m_stream_thread = null;
			m_thread_event = new AutoResetEvent(false);
			m_context = SynchronizationContext.Current;
			m_thread_running = false;
			m_context = SynchronizationContext.Current;

			m_execution_state = ExecutionState.Running;

			DebugStepIntoCommand = new ExecutionControlCommand(DebugStepExecute);
			DebugPauseCommand = new ExecutionControlCommand(DebugPauseExecute);
			DebugRunCommand = new ExecutionControlCommand(DebugRunExecute);
			DebugResetCommand = new ExecutionControlCommand(DebugResetExecute);
		}
		#endregion

		#region · Commands ·

		// Reset command
		public ExecutionControlCommand DebugResetCommand { get; private set; }

		public void DebugResetExecute(object parameter)
		{
			if (m_execution_state == ExecutionState.Running)
				m_execution_state = ExecutionState.ResetAndRun;
			else
				m_execution_state = ExecutionState.ResetAndPause;

			m_thread_event.Set();
		}



		// Step into
		public ExecutionControlCommand DebugStepIntoCommand { get; private set; }

		public void DebugStepExecute(object parameter)
		{
			StepInto();
		}

		// Pause
		public ExecutionControlCommand DebugPauseCommand { get; private set; }

		public void DebugPauseExecute(object parameter)
		{
			if (m_execution_state != ExecutionState.Paused)
			{
				m_execution_state = ExecutionState.Paused;
				m_thread_event.Set();
			}
		}

		// Run
		public ExecutionControlCommand DebugRunCommand { get; private set; }

		public void DebugRunExecute(object parameter)
		{
			if (m_execution_state != ExecutionState.Running)
			{
				m_execution_state = ExecutionState.Running;
				m_thread_event.Set();
			}
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
			int delay_time = 0;
			int frame_count = 0;
			ushort instruction_start_pc;
			uint instruction_t_cycle;
			uint current_instruction_t_cycle;
			uint target_cycle = 0;
			uint target_time;
			uint current_time;

			m_disassembler = new Z80Disassembler();
			m_disassembler.ReadByte = ReadMemory;

			m_stopwatch = new Stopwatch();

			instruction_start_pc = TVC.CPU.Registers.PC;
			instruction_t_cycle = 0;

			DateTime speed_timer = DateTime.Now;

			m_stopwatch.Reset();
			m_stopwatch.Start();
			target_time = 0;
			current_time = 0;

			while (m_thread_running)
			{
				if (m_execution_state == ExecutionState.Running)
				{
					while (!TVC.Video.RenderScanline() && m_thread_running)
					{
						target_cycle += 178;
						TVC.Memory.VideoMemAccessCount = 0;

						do
						{
							current_instruction_t_cycle = TVC.CPU.Step();
							m_cpu_cycle += current_instruction_t_cycle;
							instruction_t_cycle += current_instruction_t_cycle;

							if (TVC.CPU.InstructionDone)
							{
								AddInstructionToExecutionHistory(instruction_start_pc, instruction_t_cycle);

								instruction_start_pc = TVC.CPU.Registers.PC;
								instruction_t_cycle = 0;
							}

							if (TVC.Interrupt.IsIntActive())
								m_cpu_cycle += (uint)TVC.CPU.Int();

						} while (((long)target_cycle - m_cpu_cycle) > 0 && m_thread_running);

						if (TVC.Memory.VideoMemAccessCount > 0)
						{
							m_cpu_cycle += (uint)(TVC.Memory.VideoMemAccessCount + 1);
						}

						TVC.PeriodicCallback();
					}

					// generate debug event
					if (m_thread_running)
					{
						frame_count++;

						if (frame_count > 5)
						{
							frame_count = 0;

							ExecutionHistory.UpdateHistory();

							m_context.Post(delegate
							{
								DebuggerBreakEvent?.Invoke(TVC);
							}, null);
						}
					}

					// restart stopwatch
					m_stopwatch.Stop();
					current_time += (uint)m_stopwatch.ElapsedMilliseconds;
					m_stopwatch.Restart();

					if (m_thread_running)
					{
						target_time += 19;

						delay_time = (int)((long)target_time - current_time);

						if (delay_time < 0)
							delay_time = 0;

						m_thread_event.WaitOne(delay_time);

					}
				}
				else
				{
					switch (m_execution_state)
					{
						case ExecutionState.ResetAndPause:
							TVC.Reset();
							m_execution_state = ExecutionState.Paused;
							break;

						case ExecutionState.ResetAndRun:
							TVC.Reset();
							m_execution_state = ExecutionState.Running;
							break;

						case ExecutionState.Paused:
							m_thread_event.WaitOne(1000);
							break;
					}
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
