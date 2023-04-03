using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using YATE.Emulator.TVCHardware;
using YATE.Emulator.Z80CPU;
using YATECommon;

namespace YATE.Managers
{
	public class ExecutionManager : IExecutionManager, IDisposable, INotifyPropertyChanged
	{
		#region · Constants · 
		private int DebugEventPeriod = 200; // Debug event period in ms
		#endregion

		#region · Types · 

		public enum ExecutionStateRequests
		{
			NoChange,

			Run,
			RunFullSpeed,
			Pause,
			Restore,
			Reset,
			StepInto,
			StepOver,
			StepOut
		}

		public enum ExecutionStates
		{
			Paused,
			Running,
			RunningFullSpeed,
			StepInto,
			StepOver,
			StepOut
		}

		public delegate void DebuggerEventDelegate(TVComputer in_sender, DebugEventType in_event_type);
		public delegate void DebuggerAnimationEventDelegate(TVComputer in_sender);

		private struct StepInstructionInfo
		{
			public ushort Address;
			public ushort NextAddress;
			public TVCMemoryType MemoryType;
			public ushort Page;
			public ushort SP;
			public Z80DisassemblerInstruction Instruction;
    }

		#endregion

		#region · Data members · 

		// thread variables
		private Thread m_stream_thread;
		private AutoResetEvent m_thread_event;
		private SynchronizationContext m_context;
		private bool m_thread_running;
		private uint m_cpu_t_cycle;           // current CPU cycle
		private uint m_target_cycle;          // desired CPU cycle
		private uint m_instruction_t_cycle;
		private ushort m_instruction_start_pc; // address of the current instruction's first byte

		private ExecutionStateRequests m_execution_state_request;
		private AutoResetEvent m_execution_state_changed_event;

		private ExecutionStates m_execution_state;
		private ExecutionStates m_last_execution_state;

		private DateTime m_debug_event_timestamp;

		public readonly Window ParentWindow;

		private StepInstructionInfo m_step_instruction_info;

		private SemaphoreSlim m_audio_sync = new SemaphoreSlim(0, AudioManager.AudioBufferCount);

		#endregion

		#region · Constructor ·

		/// <summary>
		/// Default constructor
		/// </summary>
		public ExecutionManager(Window in_parent_window)
		{
			ParentWindow = in_parent_window;

			TVC = new TVComputer();
			ExecutionHistory = new ExecutionHistoryCollection(2);

			m_stream_thread = null;
			m_thread_event = new AutoResetEvent(false);
			m_context = SynchronizationContext.Current;
			m_thread_running = false;

			SetExecutionState(ExecutionStates.Running);
			m_last_execution_state = ExecutionStates.Running;

			m_execution_state_request = ExecutionStateRequests.NoChange;
			m_execution_state_changed_event = new AutoResetEvent(false);

			DebugStepIntoCommand = new ManagerCommand(DebugStepIntoExecute);
			DebugStepOverCommand = new ManagerCommand(DebugStepOverExecute);
			DebugStepOutCommand = new ManagerCommand(DebugStepOutExecute);
			DebugPauseCommand = new ManagerCommand(DebugPauseExecute);
			DebugRunCommand = new ManagerCommand(DebugRunExecute);
			DebugResetCommand = new ManagerCommand(DebugResetExecute);
			DebugRunFullSpeedCommand = new ManagerCommand(DebugRunFullSpeedExecute);

			BreakpointAddress = -1;
		}
		#endregion

		#region · Commands ·

		// Run
		public ManagerCommand DebugRunCommand { get; private set; }

		public void DebugRunExecute(object parameter)
		{
			ChangeExecutionState(ExecutionStateRequests.Run);
		}

		// Run full speed
		public ManagerCommand DebugRunFullSpeedCommand { get; private set; }

		public void DebugRunFullSpeedExecute(object parameter)
		{
			ChangeExecutionState(ExecutionStateRequests.RunFullSpeed);
		}

		// Reset command
		public ManagerCommand DebugResetCommand { get; private set; }

		public void DebugResetExecute(object parameter)
		{
			ChangeExecutionState(ExecutionStateRequests.Reset);
		}

		// Pause
		public ManagerCommand DebugPauseCommand { get; private set; }

		public void DebugPauseExecute(object parameter)
		{
			ChangeExecutionState(ExecutionStateRequests.Pause);
		}

		// Step into
		public ManagerCommand DebugStepIntoCommand { get; private set; }

		public void DebugStepIntoExecute(object parameter)
		{
			ChangeExecutionState(ExecutionStateRequests.StepInto);
		}

		// Step over
		public ManagerCommand DebugStepOverCommand { get; private set; }

		public void DebugStepOverExecute(object parameter)
		{
			ChangeExecutionState(ExecutionStateRequests.StepOver);
		}

		// Step out
		public ManagerCommand DebugStepOutCommand { get; private set; }

		public void DebugStepOutExecute(object parameter)
		{
			ChangeExecutionState(ExecutionStateRequests.StepOut);
		}

		#endregion

		#region · Properties · 

		public TVComputer TVC { get; private set; }

		public ITVComputer ITVC { get { return TVC; } }

		public int BreakpointAddress { get; set; }

		public ExecutionHistoryCollection ExecutionHistory { get; private set; }

		public CommandBinding StepCommand
		{
			get; private set;
		}

		public ExecutionStates ExecutionState
		{
			get { return m_execution_state; }
		}

		public string ExecutionStateString { get; private set; }

		#endregion

		public DebuggerAnimationEventDelegate DebuggerAnimationEvent;
		public DebuggerEventDelegate DebuggerEvent;

		public void Initialize()
		{
			TVC.Initialize();

			DebuggerAnimationEvent?.Invoke(TVC);
		}

		public void SetSoundEvent()
		{
			if (m_audio_sync.CurrentCount < AudioManager.AudioBufferCount)
				m_audio_sync.Release();
		}

		private void SetExecutionState(ExecutionStates in_state)
		{
			m_execution_state = in_state;

			switch (m_execution_state)
			{
				case ExecutionStates.Paused:
					ExecutionStateString = "Paused";
					break;

				case ExecutionStates.Running:
					ExecutionStateString = "Running";
					break;

				case ExecutionStates.RunningFullSpeed:
					ExecutionStateString = "Full Speed";
					break;

				case ExecutionStates.StepInto:
					ExecutionStateString = "Step Into";
					break;

				case ExecutionStates.StepOver:
					ExecutionStateString = "Step Over";
					break;

				case ExecutionStates.StepOut:
					ExecutionStateString = "Step Over";
					break;

				default:
					break;
			}
			OnPropertyChanged("ExecutionStateString");
		}

		private void StoreStepInstruction()
		{
			ushort pc = ((ExecutionManager)TVCManagers.Default.ExecutionManager).TVC.CPU.Registers.PC;
      Z80DisassemblerInstruction current_instruction = m_disassembler.Disassemble(pc);

			m_step_instruction_info.Instruction = current_instruction;

			m_step_instruction_info.Address = pc;
			m_step_instruction_info.NextAddress = (ushort)(pc + current_instruction.Length);
			m_step_instruction_info.MemoryType = ((TVCMemory)TVC.Memory).GetMemoryTypeAtAddress(pc);
			m_step_instruction_info.Page = (ushort)TVCManagers.Default.DebugManager.GetDebuggableMemory(m_step_instruction_info.MemoryType).PageIndex;
			m_step_instruction_info.SP = TVC.CPU.Registers.SP;
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

		public void ChangeExecutionState(ExecutionStateRequests in_request)
		{
			if (in_request == ExecutionStateRequests.NoChange)
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

		private void SimulationThread()
		{
			m_disassembler = new Z80Disassembler();
			m_disassembler.ReadByte = ((TVCMemory)TVC.Memory).Read;

			m_stopwatch = new Stopwatch();

			m_instruction_start_pc = TVC.CPU.Registers.PC;
			m_instruction_t_cycle = 0;

			m_debug_event_timestamp = DateTime.Now;


			while (m_thread_running)
			{
				// handle execution state change
				switch (m_execution_state_request)
				{
					// change to pause state
					case ExecutionStateRequests.Pause:
						m_execution_state_request = ExecutionStateRequests.NoChange;
						m_last_execution_state = m_execution_state;
						SetExecutionState(ExecutionStates.Paused);
						m_execution_state_changed_event.Set();
						GenerateDebuggerAnimationEvent(true);
						GenerateDebuggerEvent(DebugEventType.Paused);
						break;

					case ExecutionStateRequests.Restore:
						m_execution_state_request = ExecutionStateRequests.NoChange;
						SetExecutionState(m_last_execution_state);
						m_execution_state_changed_event.Set();
						break;

					// change to running
					case ExecutionStateRequests.Run:
						m_execution_state_request = ExecutionStateRequests.NoChange;
						GenerateDebuggerEvent(DebugEventType.Running);
						SetExecutionState(ExecutionStates.Running);
						m_execution_state_changed_event.Set(); break;

					// full speed run
					case ExecutionStateRequests.RunFullSpeed:
						m_execution_state_request = ExecutionStateRequests.NoChange;
						GenerateDebuggerEvent(DebugEventType.Running);
						SetExecutionState(ExecutionStates.RunningFullSpeed);
						m_execution_state_changed_event.Set(); break;

					// resets computer
					case ExecutionStateRequests.Reset:
						m_execution_state_request = ExecutionStateRequests.NoChange;
						TVC.Reset();
						m_execution_state_changed_event.Set();
						break;

					// step into
					case ExecutionStateRequests.StepInto:
						m_execution_state_request = ExecutionStateRequests.NoChange;
						if (m_execution_state != ExecutionStates.Paused)
							m_last_execution_state = m_execution_state;

            GenerateDebuggerEvent(DebugEventType.Running);
						StoreStepInstruction();

						SetExecutionState(ExecutionStates.StepInto);
						m_execution_state_changed_event.Set();
						break;

					// step over
					case ExecutionStateRequests.StepOver:
						{
							m_execution_state_request = ExecutionStateRequests.NoChange;
							if (m_execution_state != ExecutionStates.Paused)
								m_last_execution_state = m_execution_state;
							GenerateDebuggerEvent(DebugEventType.Running);

							StoreStepInstruction();

							SetExecutionState(ExecutionStates.StepOver);
							m_execution_state_changed_event.Set();
						}
						break;

					// step out
					case ExecutionStateRequests.StepOut:
						m_execution_state_request = ExecutionStateRequests.NoChange;
						if (m_execution_state != ExecutionStates.Paused)
							m_last_execution_state = m_execution_state;
						GenerateDebuggerEvent(DebugEventType.Running);

						SetExecutionState(ExecutionStates.StepOut);
						m_execution_state_changed_event.Set();
						break;

					// no change
					case ExecutionStateRequests.NoChange:
						// do nothing
						break;
				}

				// execute according the execution state
				switch (m_execution_state)
				{
					case ExecutionStates.Running:
						{
							uint ellapsed_tick;

							//m_stopwatch.Restart();
							ellapsed_tick = RunOneFrame();

							GenerateDebuggerAnimationEvent(false);
							//m_stopwatch.Stop();

							//delay_time += TVC.CPUTickToMillisec(ellapsed_tick) - m_stopwatch.Elapsed.TotalMilliseconds;

							//m_thread_event.WaitOne(1000);
							m_audio_sync.Wait(50);

							/*
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
              */
						}
						break;

					case ExecutionStates.RunningFullSpeed:
						RunOneFrame();
						m_thread_event.WaitOne(0);
						break;

					case ExecutionStates.Paused:
						m_thread_event.WaitOne(100);
						break;

					case ExecutionStates.StepInto:
						{

							RunOneFrame();
							/*
							ushort instruction_start_pc = TVC.CPU.Registers.PC;
							uint TCycle = 0;

							TCycle += TVC.CPU.Step();
							m_cpu_t_cycle += TCycle;

							// handle pending interrupts
							if (TVC.CPU.InstructionDone)
							{
								if (TVC.Interrupt.IsIntActive())
									m_cpu_t_cycle += (uint)TVC.CPU.Int();
							}

							TVC.PeriodicCallback();

							if (TVC.CPU.InstructionDone)
								AddInstructionToExecutionHistory(instruction_start_pc, TCycle);

							m_execution_state = ExecutionStates.Paused;
							GenerateDebuggerAnimationEvent();
							GenerateDebuggerEvent(DebugEventType.Paused);		 */
						}
						break;

					case ExecutionStates.StepOver:
						RunOneFrame();
						break;

					case ExecutionStates.StepOut:
						RunOneFrame();
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
			bool frame_finished = false;
			Z80DisassemblerTable.OpCode opcode = null;
			TVCMemory tvc_memory = (TVCMemory)TVC.Memory;
			Z80 cpu = TVC.CPU;
			BreakpointManager breakpoint_manager = (BreakpointManager)TVCManagers.Default.BreakpointManager;
			DebugManager debug_manager = (DebugManager)TVCManagers.Default.DebugManager;

			while (m_thread_running && !breakpoint_exit && !frame_finished)
			{
				m_target_cycle += 200;   //TODO: calculate from video timing
				tvc_memory.VideoMemAccessCount = 0;

				frame_finished = TVC.Video.RenderScanline();

				do
				{
					// handle step out
					if (m_execution_state == ExecutionStates.StepOut)
					{
						opcode = m_disassembler.GetCurrentOpcode(TVC.CPU.Registers.PC);
					}

					current_instruction_t_cycle = cpu.Step();
					m_cpu_t_cycle += current_instruction_t_cycle;
					m_instruction_t_cycle += current_instruction_t_cycle;

					if (cpu.InstructionDone)
					{
						AddInstructionToExecutionHistory(m_instruction_start_pc, m_instruction_t_cycle);

						m_instruction_start_pc = TVC.CPU.Registers.PC;
						m_instruction_t_cycle = 0;
					}

					// handle pending interrupts
					if (TVC.Interrupt.IsIntActive())
						m_cpu_t_cycle += (uint)cpu.Int();

					if (cpu.InstructionDone)
					{
						// handle breakpoints
						if (breakpoint_manager.IsBreakpointsExists && breakpoint_manager.IsBreakpointExistsAtAddress(cpu.Registers.PC))
						{
							TVCMemoryType memory_type = tvc_memory.GetMemoryTypeAtAddress(cpu.Registers.PC);
							ushort page = (ushort)(debug_manager.GetDebuggableMemory(memory_type).PageIndex);

							int breakpoint_index = breakpoint_manager.GetBreakpointIndex(memory_type, cpu.Registers.PC, page);

							if (breakpoint_index >= 0)
							{
								breakpoint_exit = true;
							}
						}

						// handle stepping conditions
						switch (m_execution_state)
						{
							case ExecutionStates.StepInto:
								breakpoint_exit = true;
								/*
								if ((m_step_instruction_info.Opcode.Flags & (Z80DisassemblerTable.OpCodeFlags.Returns | Z80DisassemblerTable.OpCodeFlags.Call | Z80DisassemblerTable.OpCodeFlags.Jumps)) != 0)
								{
								 break
								}
								else
								{

									if (cpu.Registers.PC == m_step_instruction_info.Address || cpu.Registers.PC == m_step_instruction_info.NextAddress)
									{
										breakpoint_exit = true;
									}
								}	*/
								break;

							// step out
							case ExecutionStates.StepOut:
								if (opcode != null && (opcode.Flags & Z80DisassemblerTable.OpCodeFlags.Returns) == Z80DisassemblerTable.OpCodeFlags.Returns)
								{
									if(cpu.Registers.SP == m_step_instruction_info.SP + 2)
                    breakpoint_exit = true;
								}
								break;

							case ExecutionStates.StepOver:
								if (cpu.InstructionDone)
								{
									// check for jump
									if ((m_step_instruction_info.Instruction.OpCode.Flags & (Z80DisassemblerTable.OpCodeFlags.Call | Z80DisassemblerTable.OpCodeFlags.Restarts)) != 0)
									{
										// returned from call
										if (cpu.Registers.PC == m_step_instruction_info.NextAddress)
										{
											TVCMemoryType memory_type = tvc_memory.GetMemoryTypeAtAddress(cpu.Registers.PC);
											ushort page = (ushort)(debug_manager.GetDebuggableMemory(memory_type).PageIndex);

											if (memory_type == m_step_instruction_info.MemoryType && page == m_step_instruction_info.Page)
												breakpoint_exit = true;
										}
									}
									else
									{
										// check for jump
										if ((m_step_instruction_info.Instruction.OpCode.Flags & Z80DisassemblerTable.OpCodeFlags.Jumps) != 0)
										{
											if (cpu.Registers.PC == m_step_instruction_info.Instruction.NextAddress1 || cpu.Registers.PC == m_step_instruction_info.Instruction.NextAddress2)
											{
												breakpoint_exit = true;
											}
										}
										else
										{
											// check for RET instruction
											if ((m_step_instruction_info.Instruction.OpCode.Flags & Z80DisassemblerTable.OpCodeFlags.Returns) != 0)
											{
												if (cpu.Registers.SP == m_step_instruction_info.SP + 2 || cpu.Registers.PC == m_step_instruction_info.NextAddress)
												{
													breakpoint_exit = true;
												}
											}
											else
											if (cpu.Registers.PC == m_step_instruction_info.NextAddress)
											{
												TVCMemoryType memory_type = tvc_memory.GetMemoryTypeAtAddress(cpu.Registers.PC);
												ushort page = (ushort)(debug_manager.GetDebuggableMemory(memory_type).PageIndex);

												if (memory_type == m_step_instruction_info.MemoryType && page == m_step_instruction_info.Page)
													breakpoint_exit = true;
											}
										}
									}
								}
								break;
						}
					}

					// handle clock stretching (video memory access)
					if (tvc_memory.VideoMemAccessCount > 0)
					{
						m_cpu_t_cycle += (uint)(tvc_memory.VideoMemAccessCount + 1);
						tvc_memory.VideoMemAccessCount = 0;
					}

				} while (((long)m_target_cycle - m_cpu_t_cycle) > 0 && m_thread_running && !breakpoint_exit);

				// Periodic callback for emulator classes
				TVC.PeriodicCallback();

				// do sound interrupt handling
				if (TVC.Sound != null)
					TVC.Sound.PeriodicCallback();
			}

			if (breakpoint_exit)
			{
				SetExecutionState(ExecutionStates.Paused);
				GenerateDebuggerAnimationEvent(true);
				GenerateDebuggerEvent(DebugEventType.Paused);
			}

			// return frame total cycle
			return m_cpu_t_cycle - frame_start_cycle;
		}

		/// <summary>
		/// Generates debug animation event (refreshes debug information display)
		/// </summary>
		private void GenerateDebuggerAnimationEvent(bool in_force_event)
		{
			// generate debug event
			if (m_thread_running)
			{
				DateTime current_time = DateTime.Now;

				if ((current_time - m_debug_event_timestamp).TotalMilliseconds > DebugEventPeriod || in_force_event)
				{
					m_debug_event_timestamp = current_time;

					ExecutionHistory.UpdateHistory();

					m_context.Post(delegate { DebuggerAnimationEvent?.Invoke(TVC); }, null);
				}
			}
		}

		/// <summary>
		/// Generates debug animation event (refreshes debug information display)
		/// </summary>
		private void GenerateDebuggerEvent(DebugEventType in_event_type)
		{
			// generate debug event
			if (m_thread_running)
			{
				m_context.Post(delegate { DebuggerEvent?.Invoke(TVC, in_event_type); }, null);
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
			TVCMemory tvc_memory = (TVCMemory)TVC.Memory;

			for (int i = 0; i < ExecutionHistoryEntry.HistoryEntryByteBufferLength; i++)
			{
				history_entry.Bytes[i] = tvc_memory.Read((ushort)(in_instruction_start_pc + i));
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

    #region · INotifyPropertyChanged Members ·

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
  }
}
