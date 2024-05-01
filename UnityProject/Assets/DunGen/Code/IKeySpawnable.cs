using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DunGen
{
	public interface IKeySpawnable
	{
        void SpawnKey(Key key, KeyManager manager);
	}
}
