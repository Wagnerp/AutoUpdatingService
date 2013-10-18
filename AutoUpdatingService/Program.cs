using System;
using System.ServiceProcess;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace AutoUpdateService
{
	public class Program : ServiceBase
	{
		static readonly TimeSpan CheckTime = TimeSpan.FromMinutes(15);
		public static void Main (string[] args)
		{
#if COMMAND_LINE
			ServiceBase.Run(new Program());
#else
			new Program().OnStart(args);
			Thread.Sleep(Timeout.Infinite);		
#endif

		}

		protected override void OnStart (string[] args)
		{
			Thread t = new Thread(()=>ThreadRun(args));
			t.Start();
		}

		static string[] Directories(string baseDir){
			Version i;
			if(!Directory.Exists(baseDir))
				return new string[0];

			return Directory.GetDirectories(baseDir)
				.Where(n=> Version.TryParse(Path.GetFileName(n), out i))
				.OrderByDescending(n=> Version.Parse(Path.GetFileName(n)))
				.ToArray();
		}

		static dynamic LoadPlugin (string directory)
		{
			var pathToDll = Assembly.GetExecutingAssembly ().CodeBase;
			var domainSetup = new AppDomainSetup {
				PrivateBinPath = pathToDll
			};
			var domain = AppDomain.CreateDomain (Guid.NewGuid ().ToString (), null, domainSetup);
			dynamic loader = domain.CreateInstanceAndUnwrap (Assembly.GetExecutingAssembly ().FullName, "AutoUpdateService.PluginLoader");
			loader.LoadAssembly (Path.Combine (directory, "AutoUpdate.dll"));
			return loader.New ("AutoUpdate.Plugin");
		}

		Tuple<string, Version> GetNewestDirectory(){
			var dirs = Directories(Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "Updates"));
			if(dirs.Any())			
				return Tuple.Create(dirs[0], Version.Parse(Path.GetFileName(dirs[0])));
			return null;
		}

		static bool HasNewVersion (Version lastVersion, Version lastTried, Tuple<string, Version> dir)
		{
			return dir != null && (lastVersion == null || dir.Item2 != lastVersion) && (lastTried == null || lastTried != dir.Item2);
		}

		void ThreadRun(string[] args){
			try{
				dynamic plugin = null;
				Version lastVersion  = null;
				Version lastTried = null;
				while(true){
					try{
						var dir = GetNewestDirectory();
						bool stopped = false;
						if (HasNewVersion (lastVersion, lastTried, dir)) {
							try {
								plugin = LoadPlugin (dir.Item1);
								if (plugin != null) {
									stopped = true;
									plugin.Stop ();
								}
								lastTried = dir.Item2;
								plugin.Run ();
								lastVersion = dir.Item2;
							} catch (Exception ex) {
								Trace.WriteLine ("Error loading " + dir.Item2 + " " + ex);
								if (stopped)
									plugin.Run ();
							}
						}
					}catch(Exception ex){
						Trace.WriteLine("Error in main loop " +ex);
					}

					Thread.Sleep(CheckTime);
				}
			}catch(Exception ex){
				Trace.WriteLine("Error in Run " + ex);
			}
		}

		protected override void OnStop ()
		{
			base.OnStop ();
		}
	}
}

