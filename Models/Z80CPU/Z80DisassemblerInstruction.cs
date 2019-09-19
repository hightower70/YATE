namespace Models.Z80Emulator
{
	public class Z80DisassemblerInstruction
	{
		public string Asm;
		public string Comment;
		public uint TStates;
		public uint TStates2;
		public ushort Length;
		public ushort Address;
		public ushort? NextAddress1;
		public ushort? NextAaddress2;
		public ushort? WordVal;
		public byte? ByteVal;
		public byte NumericOperand;
		public Z80DisassemblerTable.OpCode OpCode;
	}
}
