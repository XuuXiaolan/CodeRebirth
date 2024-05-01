using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DunGen
{
    [Flags]
	public enum NodeLockPlacement
	{
        Entrance    = 0x01,
        Exit        = 0x02,
	}
}
