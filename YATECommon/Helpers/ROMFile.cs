using System;
using System.IO;
using System.Reflection;

namespace YATECommon.Helpers
{
  /// <summary>
  /// Binary ROM file handling helpers
  /// </summary>
  public class ROMFile
  {
    /// <summary>
    /// Loads binary content from the given resource file
    /// </summary>
    /// <param name="in_resource_name"></param>
    public static void LoadMemoryFromResource(string in_resource_name, byte[] in_memory, ushort in_address = 0)
    {
      Assembly assembly = Assembly.GetCallingAssembly();

      using (Stream stream = assembly.GetManifestResourceStream(in_resource_name))
      {
        using (BinaryReader binary_reader = new BinaryReader(stream))
        {
          byte[] data = binary_reader.ReadBytes((int)stream.Length);

          int byte_to_copy = data.Length;

          if (byte_to_copy > in_memory.Length)
            byte_to_copy = in_memory.Length;

          Array.Copy(data, 0, in_memory, in_address, byte_to_copy);

          // fill the remaining bytes with 0xFF
          while (byte_to_copy < in_memory.Length)
            in_memory[byte_to_copy++] = 0xff;
        }
      }
    }

    /// <summary>
    /// Loads binary content from the given file
    /// </summary>
    /// <param name="in_file_name"></param>
    /// <param name="in_memory"></param>
    /// <param name="in_address"></param>
    public static bool LoadMemoryFromFile(string in_file_name, byte[] in_memory, int in_address = 0)
    {
      bool success = false;

      if (!string.IsNullOrEmpty(in_file_name))
      {
        try
        {
          byte[] data = File.ReadAllBytes(in_file_name);

          int length = data.Length;

          if ((in_address + data.Length) > in_memory.Length)
            length = in_memory.Length - in_address;

          Array.Copy(data, 0, in_memory, in_address, length);

          // fill the remaining bytes with 0xFF
          while (length < in_memory.Length)
            in_memory[length++] = 0xff;
        }
        catch
        {
          success = false;
        }
      }

      return success;
    }

    /// <summary>
    /// Comapres two memory areas and returns true if the content is same
    /// </summary>
    /// <param name="in_memory1">Memory area 1</param>
    /// <param name="in_memory2">Memory area 2</param>
    /// <returns>True - if content is same</returns>
    public static bool IsMemoryEqual(byte[] in_memory1, byte[] in_memory2)
    {
      if (in_memory1 == null || in_memory2 == null)
        return false;

      if (in_memory1.Length != in_memory2.Length)
        return false;

      // check rom content changes
      for (int i = 0; i < in_memory1.Length; i++)
      {
        if (in_memory1[i] != in_memory2[i])
        {
          return false;
        }
      }

      return true;
    }
  }
}
