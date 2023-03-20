using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using YATE.Controls;
using YATE.Emulator.TVCHardware;
using YATE.Emulator.Z80CPU;
using YATECommon;

namespace YATE.Forms
{
  /// <summary>
  /// Interaction logic for DisassemblyListView.xaml
  /// </summary>
  public partial class DisassemblyListView : Window
  {

    List<DisassemblyLine> m_disassembly_collection;

    public DisassemblyListView()
    {
      m_disassembly_collection = new List<DisassemblyLine>();

      InitializeComponent();

      StartAddress = 6659;

      EndAddress = 0x3000;

      Disassemble();

      dlbMain.ItemsSource = m_disassembly_collection;
      
    }

    public int StartAddress { get; set; }
    public int EndAddress { get; set; }


    private byte[] m_memory;

    private void Disassemble()
    {
      Z80Disassembler disassembler = new Z80Disassembler();
      disassembler.ReadByte = ReadMemoryByte;
      disassembler.HexConstDisplayMode = Z80Disassembler.HexConstDisplay.Postfix;

      m_memory = ((TVComputer)TVCManagers.Default.ExecutionManager.ITVC).Memory.DebugUserMemory;

      ushort address = (ushort)StartAddress;
      while (address < (ushort)EndAddress)
      {
        Z80DisassemblerInstruction current_instruction = disassembler.Disassemble(address);
        address += current_instruction.Length;

        m_disassembly_collection.Add(new DisassemblyLine(current_instruction));
      }
    }

    private byte ReadMemoryByte(ushort in_address)
    {
      return m_memory[in_address];
    }
  }


}
