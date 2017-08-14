using System;
using System.IO;
using System.Reflection;

using MemeMachine4.MasterPlugin;
using Mister4Eyes.GeneralUtilities;
namespace MemeMachine4
{
	class PluginHandler
	{
		public Plugin LoadPlugin(string file, PluginArgs pag)
		{
			if(File.Exists(file))
			{
				//Just in case the loading of the assembly fails.
				try
				{
					Assembly assembly = Assembly.LoadFrom(file);
					Type pluginType = typeof(Plugin);
					
					foreach(Type type in assembly.GetExportedTypes())
					{
						if(type.BaseType == pluginType)
						{
							return (Plugin)Activator.CreateInstance(type, pag);
						}
					}
				}
				catch(Exception e)
				{
					Utilities.SetColor(ConsoleColor.Red);
					Console.WriteLine(e.Message);
					Utilities.SetColor();
				}
			}

			return null;
		}

		public void UnloadPlugin(Plugin plug)
		{
			//TODO: Make this do a thing.
		}
	}
}
