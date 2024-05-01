using System;

namespace DunGen
{
	public interface IKeyLock
	{
		void OnKeyAssigned(Key key, KeyManager manager);
	}
}

