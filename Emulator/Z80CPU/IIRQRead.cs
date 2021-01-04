namespace YATE.Emulator.Z80CPU
{
    public interface IIRQRead
		{
        byte Read(Z80 cpu);
    }
}
