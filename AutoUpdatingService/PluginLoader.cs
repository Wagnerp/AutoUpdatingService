using System;
using System.Reflection;
using System.Linq;

namespace AutoUpdateService
{
	public class PluginLoader
	{
		public void LoadAssembly(string filename){
			Assembly.UnsafeLoadFrom(filename);
		}

		public object New(string type){
			var assembly = AppDomain.CurrentDomain.GetAssemblies()
				.FirstOrDefault(n=>n.GetTypes().Any(m=>m.Name == type));
			return assembly.GetType(type);
		}
	}
}

