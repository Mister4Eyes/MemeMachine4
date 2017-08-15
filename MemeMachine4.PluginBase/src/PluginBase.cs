﻿using System;
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
		public const char Id_Message_Updated	= (char)0x01;
		public const char Id_Message_Received	= (char)0x02;
		public const char Id_Message_Deleted	= (char)0x04;
		public const char Id_Reaction_Removed	= (char)0x08;
		public const char Id_Reaction_Added		= (char)0x10;

		public string Name								{ get; protected set; }	= string.Empty;
		public char UsedFunctions						{ get; protected set; }	= (char)0x00;
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

		virtual public Task ReactionRemoved(
			Cacheable<IUserMessage, ulong> messageCach,
			ISocketMessageChannel message,
			SocketReaction reaction)
		{
			throw new NotImplementedException($"Reaction removed was not implemented {Name}.");
		}

		virtual public Task ReactionAdded(
			Cacheable<IUserMessage, ulong> messageCach,
			ISocketMessageChannel message,
			SocketReaction reaction)
		{
			throw new NotImplementedException($"Reaction added was not implemented {Name}.");
		}
	}
}