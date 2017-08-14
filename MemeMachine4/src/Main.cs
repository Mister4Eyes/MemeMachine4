using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Discord;
using Discord.WebSocket;

using MemeMachine4.Audio;
using MemeMachine4.MasterPlugin;

using Mister4Eyes.IniParser;
using Mister4Eyes.GeneralUtilities;

namespace MemeMachine4.Master
{
	class MemeMachine4
	{
		string token;
		IniResults results;
		List<Plugin> plugins = new List<Plugin>();
		DiscordSocketClient client;
		PluginHandler Ph = new PluginHandler();
		AudioHandler ah;

		string game = string.Empty;
		string pluginLoc = ".\\Plugins";
		bool pollPlugins = true;
		bool startClient = true;

		public void VerifyLocationExistance()
		{
			if(!Directory.Exists(pluginLoc))
			{
				Utilities.SetColor(ConsoleColor.Red);
				Console.WriteLine(	"WARNING! The folder contianing the plugins DO NOT EXIST.\n" +
									"The program will continue executing but nothing will happen!\n" +
									$"Check for the existance of {pluginLoc} and try again.");
				Utilities.SetColor(ConsoleColor.Gray, ConsoleColor.DarkRed);

				pollPlugins = false;
			}
		}

		public MemeMachine4()
		{
			results = IniParser.ParseIniFile("C:\\discordbot.ini");
			Section sect = results.GetSection();

			if(sect.HasKey("ffmpeg"))
			{
				ah = new AudioHandler(sect.GetValue("ffmpeg"));
			}
			else
			{
				ah = new AudioHandler();
			}

			if(sect.HasKey("game"))
			{
				game = sect.GetValue("game");
			}

			if(sect.HasKey("pluginLocation"))
			{
				string location = sect.GetValue("pluginLocation");
				if(Directory.Exists(location))
				{
					pluginLoc = location;
				}
				else
				{
					VerifyLocationExistance();
				}
			}
			else
			{
				VerifyLocationExistance();
			}
			bool debug = false;
#if DEBUG
			debug = true;
			if (sect.HasKey("debugToken"))
			{
				token = sect.GetValue("debugToken");
			}
			else
			{
				token = sect.GetValue("token");
			}
#else
			token = sect.GetValue("token");
#endif
			if(token == null)
			{
				string tokenText = (debug) ? "debugToken or a token" : "token";
				Utilities.SetColor(ConsoleColor.Red, ConsoleColor.Black);
				Console.WriteLine("FATAL ERROR: The discord bot token was not found!\n" +
									$"Please make sure that your ini file has a {tokenText} value.");
				Utilities.SetColor(ConsoleColor.Gray, ConsoleColor.DarkRed);
				startClient = false;
			}
		}

		static void Main(string[] args) => new Program().Start();

		private static Task CompletedTask()
		{
			return Task.CompletedTask;
		}
		public async Task BotMain()
		{
			DiscordSocketConfig dsc = new DiscordSocketConfig();

#if DEBUG
			dsc.LogLevel = LogSeverity.Debug;
#else
			dsc.LogLevel = LogSeverity.Info;
#endif

			//Used for setting a custom websocket.
			if (Environment.OSVersion.Platform == PlatformID.Win32NT)
			{
				dsc.WebSocketProvider = Discord.Net.Providers.WS4Net.WS4NetProvider.Instance;
			}

			client = new DiscordSocketClient(dsc);

			foreach(Plugin plug in plugins)
			{
				plug.InjectSocketClient(client);
			}

			if(game != string.Empty)
			{
				await client.SetGameAsync(game);
			}

			client.Log += Client_Log;

			client.MessageReceived	+= Client_MessageReceived;
			client.MessageDeleted	+= Client_MessageDeleted;
			client.MessageUpdated	+= Client_MessageUpdated;
			client.ReactionAdded	+= Client_ReactionAdded;
			client.ReactionRemoved	+= Client_ReactionRemoved;
			
			await client.LoginAsync(TokenType.Bot, token);
			await client.StartAsync();

			await Task.Delay(-1);
		}

		private Task Client_Log(LogMessage arg)

		{
			ConsoleColor cc = Console.ForegroundColor;

			Utilities.SetColor(ConsoleColor.White);
			Console.Write("[{0}] ", DateTime.Now);

			switch (arg.Severity)
			{
				case LogSeverity.Critical:
					Utilities.SetColor(ConsoleColor.Red);
					break;

				case LogSeverity.Debug:
					Utilities.SetColor(ConsoleColor.Blue);
					break;

				case LogSeverity.Error:
					Utilities.SetColor(ConsoleColor.DarkRed);
					break;

				case LogSeverity.Info:
					Utilities.SetColor(ConsoleColor.Green);
					break;

				case LogSeverity.Verbose:
					Utilities.SetColor(ConsoleColor.Gray);
					break;

				case LogSeverity.Warning:
					Utilities.SetColor(ConsoleColor.Yellow);
					break;
			}

			Console.Write("{0}\n", arg.Message);
			Utilities.SetColor();
			
			return CompletedTask();
		}

		private async Task EvaluateAndExecute(char Id, Func<Plugin, Task> func)
		{
			foreach (Plugin plug in plugins)
			{
				if ((plug.UsedFunctions & Id) != 0)
				{
					try
					{
						await func(plug);
					}
					//Just in case someone screws up and forgetst to implement the function.
					//Keeps the program running in case of such an error.
					//The rest of the exception handling should be done inside of the function.
					catch(Exception e)
					{
						Utilities.SetColor(ConsoleColor.Red);
						Console.WriteLine($"An Exception was thrown.\nMessage:{e.Message}\n---===STACK TRACE===---\n{e.StackTrace}");
						Utilities.SetColor();
					}
				}
			}
		}

#region PluginWrappers
		private async Task Client_MessageUpdated(	Cacheable<IMessage, ulong> messageCach,
													SocketMessage message,
													ISocketMessageChannel channel)
		{
			await EvaluateAndExecute(Plugin.Id_Message_Updated, (plug) =>
			{
				return plug.MessageUpdated(messageCach, message, channel);
			});
		}

		private async Task Client_MessageReceived(	SocketMessage message)
		{
			await EvaluateAndExecute(Plugin.Id_Message_Received, (plug) =>
			{
				return plug.MessageReceived(message);
			});
		}

		private async Task Client_MessageDeleted(	Cacheable<IMessage, ulong> messageCach,
													ISocketMessageChannel message)
		{
			await EvaluateAndExecute(Plugin.Id_Message_Deleted, (plug) =>
			{
				return plug.MessageDeleted(messageCach, message);
			});
		}

		private async Task Client_ReactionRemoved(	Cacheable<IUserMessage, ulong> messageCach,
													ISocketMessageChannel message,
													SocketReaction reaction)
		{
			await EvaluateAndExecute(Plugin.Id_Reaction_Removed, (plug) =>
			{
				return plug.ReactionRemoved(messageCach, message, reaction);
			});
		}

		private async Task Client_ReactionAdded(	Cacheable<IUserMessage, ulong> messageCach,
													ISocketMessageChannel message,
													SocketReaction reaction)
		{
			await EvaluateAndExecute(Plugin.Id_Reaction_Added, (plug) =>
			{
				return plug.ReactionAdded(messageCach, message, reaction);
			});
		}
#endregion

		private async Task Logout()
		{
			await client.LogoutAsync();
			await client.StopAsync();
		}

		private DirectoryInfo[] GetPluginDirectories()
		{
			List<DirectoryInfo> dirList = new List<DirectoryInfo>();

			foreach(DirectoryInfo di in new DirectoryInfo(pluginLoc).EnumerateDirectories())
			{
				dirList.Add(di);
			}

			return dirList.ToArray();
		}

		private static string RemoveExtension(string path)
		{
			string pattern = @"\\?\/?([^\\/\r\n]+)\.[^\\/\r\n]+$";
			Match match = Regex.Match(path, pattern);

			return (match.Success) ? match.Groups[1].Value : null;
		}

		public void Start()
		{
			if(pollPlugins)
			{
				DirectoryInfo[] dInfo = GetPluginDirectories();

				foreach(DirectoryInfo dir in dInfo)
				{
					string name = dir.Name;
					Plugin plug = null;

					//Gets all dll files
					//This is done so if the author needs to elaborat on it for whatever reason, they can add more data
					//Ex: MyAwsomePlugin/MyAwsomePlugin-V1.dll is a valid plugin file.
					foreach(FileInfo fi in dir.EnumerateFiles($"*.dll"))
					{
						//Sees if it starts with the name
						if(fi.Name.StartsWith(name))
						{
							PluginArgs pag = new PluginArgs()
							{
								workingDir = dir.FullName,
								audioHandler = ah,
								section = results.GetSection(RemoveExtension(fi.Name))
							};
							Plugin test = Ph.LoadPlugin(fi.FullName, pag);

							if(test != null)
							{
								plug = test;
								Utilities.SetColor(ConsoleColor.Blue);
								Console.WriteLine($"Loaded {test.Name}");
								Utilities.SetColor();
							}
						}
					}
					
					if(plug != null)
					{
						plugins.Add(plug);
					}
				}
			}

			if(startClient)
			{
				Task task = Task.Run(BotMain);

				Console.WriteLine("Press any key to finish.");
				Console.ReadKey(true);

				//Stopping bot.
				Task disconnect = Task.Run(Logout);
				try
				{
					Console.WriteLine("Waiting for bot disconnect.");
					disconnect.Wait();
				}
				catch (Exception tce)
				{
					Console.WriteLine(tce.Message);
				}

				foreach(Plugin plug in plugins)
				{
					Ph.UnloadPlugin(plug);
				}
			}
			else
			{
				Console.WriteLine("Press any key to kill the process.");
				Console.ReadKey(true);
			}
		}
	}
}
