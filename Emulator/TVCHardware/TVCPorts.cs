using YATE.Emulator.Z80CPU;
using YATECommon;

namespace YATE.Emulator.TVCHardware
{
	public class TVCPorts : IZ80Port, ITVCPorts
	{
		public const int PortCount = 256;

		private IORead[] m_io_read;
		private IOWrite[] m_io_write;
		private IOReset[] m_io_reset;
		private Z80 m_cpu;

		public TVCPorts()
		{
			m_io_read = new IORead[PortCount];
			m_io_write = new IOWrite[PortCount];
			m_io_reset = new IOReset[PortCount];
		}

		public void SetCPU(Z80 in_cpu)
		{
			m_cpu = in_cpu;
		}

		public byte Read(ushort in_address)
		{
			ushort address = (ushort)(in_address & 0xff);
			byte data = 0xff;

			m_io_read[address]?.Invoke(in_address, ref data);

			return data;
		}

		public void Write(ushort in_address, byte in_data)
		{
			ushort address = (ushort)(in_address & 0xff);

			m_io_write[address]?.Invoke(in_address, in_data);
		}

		/// <summary>
		/// Resets all port 
		/// </summary>
		public void Reset()
		{
			for (int i = 0; i < PortCount; i++)
			{
				m_io_reset[i]?.Invoke();
			}
		}

		public void AddPortReader(ushort in_address, IORead in_reader)
		{
			int address = in_address & 0xff;

			m_io_read[address] += in_reader;
		}

		public void RemovePortReader(ushort in_address, IORead in_reader)
		{
			int address = in_address & 0xff;

			m_io_read[address] -= in_reader;
		}

		public void AddPortWriter(ushort in_address, IOWrite in_writer)
		{
			int address = in_address & 0xff;

			m_io_write[address] += in_writer;
		}

		public void RemovePortWriter(ushort in_address, IOWrite in_writer)
		{
			int address = in_address & 0xff;

			m_io_write[address] -= in_writer;
		}

		public void AddPortReset(ushort in_address, IOReset in_reset)
		{
			int address = in_address & 0xff;

			m_io_reset[address] += in_reset;
		}

		public void RemovePortReset(ushort in_address, IOReset in_reset)
		{
			int address = in_address & 0xff;

			m_io_reset[address] -= in_reset;
		}
	}
}
