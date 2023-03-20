using System.Windows;
using System.Windows.Controls;
using YATE.Managers;
using YATE.Emulator.TVCHardware;
using YATE.Emulator.Z80CPU;
using System.Diagnostics;

namespace YATE.Forms
{
  /// <summary>
  /// Interaction logic for ExecutionHistoryPage.xaml
  /// </summary>
  public partial class ExecutionHistoryPage : UserControl
	{

		private void DebuggerBreakEventDelegate(TVComputer in_sender)
		{
			UpdateExecutionHistory();
		}

		private Z80Disassembler m_disassembler;
		private ExecutionManager m_execution_control;


		public ExecutionHistoryPage()
		{
			m_disassembler = new Z80Disassembler();

			InitializeComponent();
		}

		#region · ExecutionControl dependency property ·

		public static DependencyProperty ExecutionControlProperty = DependencyProperty.Register("ExecutionControl", typeof(ExecutionManager), typeof(ExecutionHistoryPage), new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnExecutionControlPropertyChanged)));

		private static void OnExecutionControlPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			((ExecutionHistoryPage)sender).RegisterDebugEventHandler((ExecutionManager)e.NewValue);
		}

		public ExecutionManager ExecutionControl
		{
			get { return (ExecutionManager)GetValue(ExecutionControlProperty); }
			set { SetValue(ExecutionControlProperty, value); }
		}

		private void UpdateExecutionHistory()
		{
			ushort address = m_execution_control.TVC.CPU.Registers.PC;
			Z80DisassemblerInstruction instruction;
			Z80Disassembler disassembler = new Z80Disassembler();

			for (int i = 0; i < 2; i++)
			{
				ExecutionHistoryEntry history = m_execution_control.ExecutionHistory[i];

				if (history.TCycle > 0)
				{
					disassembler.ReadByte = history.ReadMemory;
					instruction = disassembler.Disassemble(history.PC);
					instruction.TStates = history.TCycle;
					instruction.TStates2 = 0;
				}
				else
					instruction = null;

				UpdateAddress(instruction, 1 - i);
				UpdateBytes(instruction, 1 - i);
				UpdateMnemonics(instruction, 1 - i);
				UpdateOperand1(instruction, 1 - i);
				UpdateOperand2(instruction, 1 - i);

				UpdateTCycle(instruction, 1 - i);
			}

			for (int i = 2; i < 5; i++)
			{
				instruction = m_disassembler.Disassemble(address);

				UpdateAddress(instruction, i);
				UpdateBytes(instruction, i);
				UpdateMnemonics(instruction, i);
				UpdateOperand1(instruction, i);
				UpdateOperand2(instruction, i);

				UpdateTCycle(instruction, i);

				address += instruction.Length;
			}

		}

		private void UpdateAddress(Z80DisassemblerInstruction in_instruction, int in_index)
		{
			TextBlock text_block = FindName("Address" + in_index.ToString()) as TextBlock;

			if (in_instruction == null)
				text_block.Text = string.Empty;
			else
				text_block.Text = in_instruction.Address.ToString("X4");
		}

		private void UpdateBytes(Z80DisassemblerInstruction in_instruction, int in_index)
		{
			TextBlock text_block= FindName("Data" + in_index.ToString()) as TextBlock;

			string value = string.Empty;

			if (in_instruction != null)
			{

				for (int i = 0; i < in_instruction.Length; i++)
				{
					byte b = m_execution_control.TVC.Memory.DebuggerMemoryRead((ushort)(in_instruction.Address + i));

					if (!string.IsNullOrEmpty(value))
						value += ' ';

					value += b.ToString("X02");
				}
			}

			text_block.Text = value;
		}

		private void UpdateMnemonics(Z80DisassemblerInstruction in_instruction, int in_index)
		{
			TextBlock text_block = FindName("Mnemonic" + in_index.ToString()) as TextBlock;

			string mnemonic = string.Empty;

			if (in_instruction != null)
			{
				int space_pos = in_instruction.Asm.IndexOf(' ');

				if (space_pos > 0)
					mnemonic = in_instruction.Asm.Substring(0, space_pos);
				else
					mnemonic = in_instruction.Asm;
			}

			text_block.Text = mnemonic;
		}

		private void UpdateOperand1(Z80DisassemblerInstruction in_instruction, int in_index)
		{
			TextBlock text_block_reg = FindName("Operand1Reg_" + in_index.ToString()) as TextBlock;
			TextBlock text_block_num = FindName("Operand1Num_" + in_index.ToString()) as TextBlock;

			Visibility reg_visibility = Visibility.Collapsed;
			Visibility num_visibility = Visibility.Collapsed;

			string operand = string.Empty;

			if (in_instruction != null)
			{
				int space_pos = in_instruction.Asm.IndexOf(' ');

				if (space_pos >= 0)
				{
					operand = in_instruction.Asm.Substring(space_pos + 1);

					int comma_pos = operand.IndexOf(',');
					if (comma_pos >= 0)
						operand = operand.Substring(0, comma_pos);
				}

				if( in_instruction.NumericOperand==1)
				{
					text_block_num.Text = operand;
					num_visibility = Visibility.Visible;
				}
				else
				{
					text_block_reg.Text = operand;
					reg_visibility = Visibility.Visible;
				}
			}

			text_block_reg.Visibility = reg_visibility;
			text_block_num.Visibility = num_visibility;
		}

		private void UpdateOperand2(Z80DisassemblerInstruction in_instruction, int in_index)
		{
			TextBlock text_block_reg = FindName("Operand2Reg_" + in_index.ToString()) as TextBlock;
			TextBlock text_block_num = FindName("Operand2Num_" + in_index.ToString()) as TextBlock;
			TextBlock separator = FindName("OperandSeparator_" + in_index.ToString()) as TextBlock;

			Visibility reg_visibility = Visibility.Collapsed;
			Visibility num_visibility = Visibility.Collapsed;
			Visibility sep_visibility = Visibility.Hidden;

			string operand = string.Empty;

			if (in_instruction != null)
			{
				int comma_pos = in_instruction.Asm.IndexOf(',');
				if (comma_pos >= 0)
				{
					operand = in_instruction.Asm.Substring(comma_pos + 1).Trim();
					sep_visibility = Visibility.Visible;
				}

				if (in_instruction.NumericOperand == 2)
				{
					text_block_num.Text = operand;
					num_visibility = Visibility.Visible;
				}
				else
				{
					text_block_reg.Text = operand;
					reg_visibility = Visibility.Visible;
				}
			}

			text_block_reg.Visibility = reg_visibility;
			text_block_num.Visibility = num_visibility;
			separator.Visibility = sep_visibility;
		}

		private void UpdateTCycle(Z80DisassemblerInstruction in_instruction, int in_index)
		{
			TextBlock text_block = FindName("TCycle" + in_index.ToString()) as TextBlock;

			if (in_instruction == null)
			{
				text_block.Text = string.Empty;
			}
			else
			{
				if (in_instruction.TStates2 == 0)
				{
					text_block.Text = in_instruction.TStates.ToString();
				}
				else
				{
					text_block.Text = in_instruction.TStates.ToString() + '/' + in_instruction.TStates2.ToString();
				}
			}
		}

		private void RegisterDebugEventHandler(ExecutionManager in_execution_control)
		{
			in_execution_control.DebuggerPeriodicEvent += DebuggerBreakEventDelegate;
			m_execution_control = in_execution_control;
			m_disassembler.ReadByte = in_execution_control.TVC.Memory.DebuggerMemoryRead;

			UpdateExecutionHistory();
		}

		#endregion

	}
}
