using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDCart
{
  interface IFunctionGroup
  {
    byte CoProcessorRead();
    void CoProcessorWrite(byte in_data);
  }
}
