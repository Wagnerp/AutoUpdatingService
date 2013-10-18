using System;

namespace AutoUpdateService
{
	/// <summary>
	/// Plugin interface. Either inherit from this class or make your class implement it
	/// </summary>
	public abstract class Plugin : MarshalByRefObject
	{
		/// <summary>
		/// Run async
		/// </summary>
		public abstract void Run();
		/// <summary>
		/// Stop
		/// </summary>
		public abstract void Stop();
	}
}

