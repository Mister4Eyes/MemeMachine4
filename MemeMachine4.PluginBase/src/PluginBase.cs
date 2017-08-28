using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using Discord;
using Discord.WebSocket;
using MemeMachine4.Audio;
using Mister4Eyes.IniParser;

namespace MemeMachine4.PluginBase
{
	//We are using a seperate struct in order to make it easier for the proper objects to be injected.
	//This is due to the design of the plugin handler. It would requre a rework of the function every time a new feature needs to be added.
	//So having one object keeps code from being messy.
	public struct PluginArgs
	{
		public string workingDir;
		public AudioHandler audioHandler;
		public Section section;

		public PluginArgs(string w, AudioHandler a, Section s)
		{
			workingDir = w;
			audioHandler = a;
			section = s;
		}
	}


	public class Plugin
	{
		public enum Functions
		{
			None					= 0x00000,
			ChannelCreated			= 0x00001,
			ChannelDestroyed		= 0x00002,
			ChannelUpdated			= 0x00004,
			MessageUpdated			= 0x00008,
			MessageReceived			= 0x00010,
			MessageDeleted			= 0x00020,
			ReactionAdded			= 0x00040,
			ReactionRemoved			= 0x00080,
			RoleCreated				= 0x00100,
			RoleDeleted				= 0x00200,
			RoleUpdated				= 0x00400,
			UserBanned				= 0x00800,
			UserIsTyping			= 0x01000,
			UserJoined				= 0x02000,
			UserLeft				= 0x04000,
			UserUnbanned			= 0x08000,
			UserUpdated				= 0x10000,
			UserVoiceStateUpdated	= 0x20000,
			GuildMemberUpdated		= 0x40000,
			GuildUpdated			= 0x80000
		}

		public string Name								{ get; protected set; }	= string.Empty;
		public Functions UsedFunctions					{ get; protected set; }	= Functions.None;
		public string WorkingDirectory					{ protected get; set; }	= string.Empty;
		protected DiscordSocketClient DiscordSockClient	{ get; private set; }	= null;
		protected Section PluginSection					{ get; private set; }	= null;
		private AudioHandler Ah = null;

		public Plugin(PluginArgs pag)
		{
			WorkingDirectory = pag.workingDir;
			Ah = pag.audioHandler;
			PluginSection = pag.section;
		}
		//This region was made so the plugins have some functions to send audio information.
		//It's meant to abstract the audio handler class. Even though audio handler is in of itself, an abstraction
		//This provides a few more things like automatic finding of voice channels, built in thread safety, and exception handling.
		#region Audio Sending Functions
		//The wrappers for the files
		protected async Task<bool> SendAudioFile(SocketGuildUser user, string file)
		{
			return await SendAudioFile(user.VoiceChannel, file);
		}

		protected async Task<bool> SendAudioFile(IVoiceChannel voiceChannel, string file)
		{
			if(!File.Exists(file))
			{
				return false;
			}

			return await Ah.SendFile(voiceChannel, file);
		}
		//The wrappers for the audio streams
		protected async Task<bool> SendAudioStream(SocketGuildUser user, Stream stream)
		{
			return await SendAudioStream(user.VoiceChannel, stream);
		}

		protected async Task<bool> SendAudioStream(IVoiceChannel voiceChannel, Stream stream)
		{
			if(Ah == null || voiceChannel == null)
			{
				return false;
			}

			await Ah.SendStream(voiceChannel, stream);

			return true;
		}

		protected async Task StopCurrentAudio(IVoiceChannel voiceChannel)
		{
			await Ah.StopAudio(voiceChannel);
		}
		#endregion

		#region File Loading and saving functions.
		public string GetPath(string path)
		{
			if(Path.IsPathRooted(path))
			{
				return path;
			}
			else
			{
				return Path.Combine(WorkingDirectory, path);
			}
		}

		//Wrappers for string
		public void SaveText(string path, string text)
		{
			byte[] data = Encoding.UTF8.GetBytes(text);
			SaveBytes(path, data);
		}

		public string LoadText(string path)
		{
			byte[] data = LoadBytes(path);

			//Checks for failure
			if(data == null)
			{
				return null;
			}
			return Encoding.UTF8.GetString(data);
		}

		private void MakeDirIfNonexistant(string path)
		{
			string dir = Path.GetDirectoryName(path);
			if (!Directory.Exists(dir))
			{
				Directory.CreateDirectory(dir);
			}
		}
		public void SaveBytes(string path, byte[] data)
		{
			path = GetPath(path);

			MakeDirIfNonexistant(path);

			File.WriteAllBytes(path, data);
		}

		public byte[] LoadBytes(string path)
		{
			path = GetPath(path);

			if(File.Exists(path))
			{
				return File.ReadAllBytes(path);
			}
			else
			{
				return null;
			}
		}

		public FileStream LoadFileStream(string path)
		{
			path = GetPath(path);

			if(File.Exists(path))
			{
				return new FileStream(path, FileMode.Open);
			}
			else
			{
				return null;
			}
		}

		public FileStream SaveFileStream(string path)
		{
			path = GetPath(path);

			MakeDirIfNonexistant(path);

			FileStream fs = new FileStream(path, FileMode.OpenOrCreate);
			return fs;
		}
		#endregion
		
		//This is still needed because there is an order to how these things are made. Sadly this is at the ass end of that order.
		public void InjectSocketClient(DiscordSocketClient dsc)
		{
			DiscordSockClient = dsc;
		}

		public bool IsMentioningMe(SocketMessage message)
		{
			ulong id = DiscordSockClient.CurrentUser.Id;

			foreach(SocketUser user in message.MentionedUsers)
			{
				if(id == user.Id)
				{
					return true;
				}
			}

			return false;
		}

		//Using virtual instead of abstract in order to keep substidary functions clean.		
		virtual public Task ChannelCreated(SocketChannel arg)
		{
			throw new NotImplementedException($"Reaction added was not implemented {Name}.");
		}

		virtual public Task ChannelDestroyed(SocketChannel arg)
		{
			throw new NotImplementedException($"Reaction added was not implemented {Name}.");
		}

		virtual public Task ChannelUpdated(SocketChannel arg1, SocketChannel arg2)
		{
			throw new NotImplementedException($"Reaction added was not implemented {Name}.");
		}

		virtual public Task MessageUpdated(
			Cacheable<IMessage, ulong> messageCach,
			SocketMessage message,
			ISocketMessageChannel channel)
		{
			throw new NotImplementedException($"Message updated was not implemented in {Name}.");
		}

		virtual public Task MessageReceived(SocketMessage message)
		{
			throw new NotImplementedException($"Message recived was not implemented in {Name}.");
		}

		virtual public Task MessageDeleted(
			Cacheable<IMessage, ulong> messageCach,
			ISocketMessageChannel message)
		{
			throw new NotImplementedException($"Message deleted was not implemented {Name}.");
		}

		virtual public Task ReactionAdded(
			Cacheable<IUserMessage, ulong> messageCach,
			ISocketMessageChannel message,
			SocketReaction reaction)
		{
			throw new NotImplementedException($"Reaction added was not implemented {Name}.");
		}

		virtual public Task ReactionRemoved(
			Cacheable<IUserMessage, ulong> messageCach,
			ISocketMessageChannel message,
			SocketReaction reaction)
		{
			throw new NotImplementedException($"Reaction removed was not implemented {Name}.");
		}
		
		virtual public Task RoleCreated(SocketRole arg)
		{
			throw new NotImplementedException($"Role Created was not implemented {Name}.");
		}

		virtual public Task RoleDeleted(SocketRole arg)
		{
			throw new NotImplementedException($"Role Deleted was not implemented {Name}.");
		}

		virtual public Task RoleUpdated(SocketRole arg1, SocketRole arg2)
		{
			throw new NotImplementedException($"Role Updated was not implemented {Name}.");
		}

		virtual public Task UserBanned(SocketUser arg1, SocketGuild arg2)
		{
			throw new NotImplementedException($"User Banned was not implemented {Name}.");
		}
		
		virtual public Task UserIsTyping(SocketUser arg1, ISocketMessageChannel arg2)
		{
			throw new NotImplementedException($"User Is Typing was not implemented {Name}.");
		}

		virtual public Task UserJoined(SocketGuildUser arg)
		{
			throw new NotImplementedException($"User Joined was not implemented {Name}.");
		}

		virtual public Task UserLeft(SocketGuildUser arg)
		{
			throw new NotImplementedException($"User Left was not implemented {Name}.");
		}

		virtual public Task UserUnbanned(SocketUser arg1, SocketGuild arg2)
		{
			throw new NotImplementedException($"User Unbanned was not implemented {Name}.");
		}

		virtual public Task UserUpdated(SocketUser arg1, SocketUser arg2)
		{
			throw new NotImplementedException($"User Updated was not implemented {Name}.");
		}

		virtual public Task UserVoiceStateUpdated(SocketUser arg1, SocketVoiceState arg2, SocketVoiceState arg3)
		{
			throw new NotImplementedException($"User Voice State Updated was not implemented {Name}.");
		}

		virtual public Task GuildMemberUpdated(SocketGuildUser arg1, SocketGuildUser arg2)
		{
			throw new NotImplementedException($"Guild Member Updated was not implemented {Name}.");
		}

		virtual public Task GuildUpdated(SocketGuild arg1, SocketGuild arg2)
		{
			throw new NotImplementedException($"Guild Updated was not implemented {Name}.");
		}
	}
}
