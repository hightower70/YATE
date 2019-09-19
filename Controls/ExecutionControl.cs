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

		private const int HistoryEntryByteBufferLength = 6;

		public class ExecutionHistoryEntry
		{
			public ushort PC { get; set; }
			public uint TCycle { get; set; }
			public byte[] Bytes { get; private set; }

			public ExecutionHistoryEntry()
			{
				Bytes = new byte[HistoryEntryByteBufferLength];
			}

			public byte ReadMemory(ushort in_address)
			{
				int address = in_address - PC;

				if (address < 0 || address >= Bytes.Length)
					return 0;
				else
					return Bytes[address];
			}

			public void CopyTo(ExecutionHistoryEntry in_entry)
			{
				in_entry.PC = PC;
				in_entry.TCycle = TCycle;
				Bytes.CopyTo(in_entry.Bytes,0);
			}
		}

		public class ExecutionHistoryColection
		{
			private ExecutionHistoryEntry[] m_write_history;
			private ExecutionHistoryEntry[] m_read_history;
			private int m_write_index;
			private int m_read_index;

			public ExecutionHistoryColection(int in_element_count)
			{
				m_read_history = new ExecutionHistoryEntry[in_element_count];
				m_write_history = new ExecutionHistoryEntry[in_element_count];

				for (int i =0;i<m_write_history.Length;i++)
				{
					m_read_history[i] = new ExecutionHistoryEntry();
					m_write_history[i] = new ExecutionHistoryEntry();
				}
			}

			public ExecutionHistoryEntry GetNextEmptySlot()
			{
				m_write_index--;

				if (m_write_index < 0)
					m_write_index = m_write_history.Length - 1;

				return m_write_history[m_write_index];
			}

			public void UpdateHistory()
			{
				m_read_index = m_write_index;
				for (int i = 0; i < m_write_history.Length; i++)
					m_write_history[i].CopyTo(m_read_history[i]);
			}


			public ExecutionHistoryEntry this[int in_index]
			{
				get
				{
					int index = (m_read_index + in_index) % m_write_history.Length;

					return m_write_history[index];
				}
			}

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
		#endregion

		#region · Constructor ·

		/// <summary>
		/// Default constructor
		/// </summary>
		public ExecutionControl()
		{
			TVC = new TVComputer();
			ExecutionHistory = new ExecutionHistoryColection(2);

			m_stream_thread = null;
			m_thread_event = new AutoResetEvent(false);
			m_context = SynchronizationContext.Current;
			m_thread_running = false;
			m_context = SynchronizationContext.Current;

			DebugStepCommand = new ExecutionControlCommand(DebugStepExecute);
		}
		#endregion

		#region · Commands ·
		public ExecutionControlCommand DebugStepCommand { get; private set; }

		public void DebugStepExecute(object parameter)
		{
			StepInto();
		}


		#endregion

		#region · Properties · 

		public TVComputer TVC { get; private set; }
		public ExecutionHistoryColection ExecutionHistory { get; private set; }

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

			//ExecutionHistory.Add(instruction_start_pc, TCycle);

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
							ExecutionHistoryEntry history_entry = ExecutionHistory.GetNextEmptySlot();
							history_entry.PC = instruction_start_pc;
							history_entry.TCycle = instruction_t_cycle;

							for (int i = 0; i < HistoryEntryByteBufferLength; i++)
							{
								history_entry.Bytes[i] = TVC.Memory.Read((ushort)(instruction_start_pc + i));
							}

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
