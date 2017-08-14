using Discord;
using Discord.Audio;
using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

using Mister4Eyes.GeneralUtilities;

namespace MemeMachine4.Audio
{
	public class AudioHandler
	{
		Task aloop;
		string ffmpegLoc;
		Queue<Tuple<IVoiceChannel, Stream>> AudioQueue = new Queue<Tuple<IVoiceChannel, Stream>>();
		int QueueLength
		{
			get
			{
				lock(AudioQueue)
				{
					return AudioQueue.Count;
				}
			}
		}

		#region Public Functions
		public async Task<bool> SendFile(IVoiceChannel channel, string path)
		{
			if (ffmpegLoc == null)
			{
				return false;
			}

			//Runs this asyncronously
			await Task.Run(() =>
			{
				Console.WriteLine("Creating ffmpeg process.");
				Process ffmpeg = CreateStream(path);

				Console.WriteLine("Enquing data.");
				Stream outStream = ffmpeg.StandardOutput.BaseStream;

				Console.WriteLine("Loading file into memory.");

				//Loads file into memory.
				//Keeps it from studdering.
				MemoryStream ms = new MemoryStream();
				outStream.CopyTo(ms);
				ms.Seek(-ms.Position, SeekOrigin.Current);
				PushQueue(channel, ms);
			});

			return true;
		}

		public Task SendStream(IVoiceChannel channel, Stream output)
		{
			PushQueue(channel, output);
			return Task.CompletedTask;
		}
		#endregion

		#region QueueWrappers
		private void PushQueue(IVoiceChannel channel, Stream stream)
		{
			lock(AudioQueue)
			{
				AudioQueue.Enqueue(new Tuple<IVoiceChannel, Stream>(channel, stream));
			}
		}

		private Tuple<IVoiceChannel, Stream> PopQueue()
		{
			if (QueueLength > 0)
			{
				lock (AudioQueue)
				{
					return AudioQueue.Dequeue();
				}
			}
			else
			{
				return null;
			}
		}

		//Returns false if there is no more items in the queue
		private bool PopQueue(out IVoiceChannel voiceChannel, out Stream stream)
		{
			Tuple<IVoiceChannel, Stream> tuple = PopQueue();

			if(tuple == null)
			{
				voiceChannel = null;
				stream = null;
				return false;
			}
			
			voiceChannel = tuple.Item1;
			stream = tuple.Item2;
			return true;
		}
#endregion

		private async Task AudioLoop()
		{
			while (true)
			{
				if (QueueLength > 0)
				{
					Stream data;
					IVoiceChannel channel;

					//No need for testing due to it allready tested.
					//This is the only thread that removes data from it as well.
					PopQueue(out channel, out data);

					IAudioClient client = await JoinChannel(channel);
					await SendAudio(client, data);

					await client.StopAsync();
				}
			}
		}

		private void SearchForFfmpeg()
		{
			string file = Utilities.FindFile("ffmpeg.exe");
			if(file == null)
			{
				Console.WriteLine("Could not find ffmpeg on the system.");
			}
			ffmpegLoc = file;
		}

		public AudioHandler()
		{
			SearchForFfmpeg();
			aloop = Task.Run(AudioLoop); 
		}

		public AudioHandler(string ffmpeg)
		{
			if(File.Exists(ffmpeg) && new FileInfo(ffmpeg).Name == "ffmpeg.exe")
			{
				ffmpegLoc = ffmpeg;
			}
			else
			{
				Console.WriteLine("The inputed file for ffmpeg is incorrect. Finding it myself.");
				SearchForFfmpeg();
			}
			aloop = Task.Run(AudioLoop);
		}
		
		private Process CreateStream(string path)
		{
		
			var ffmpeg = new ProcessStartInfo
			{
				FileName = ffmpegLoc,//TODO: Get config file to change the location of ffmpeg
				Arguments = $"-i {path} -ac 2 -f s16le -ar 48000 pipe:1",
				UseShellExecute = false,
				RedirectStandardOutput = true,
			};

			return Process.Start(ffmpeg);
		}

		private async Task<IAudioClient> JoinChannel(IVoiceChannel channel)
		{
			try
			{
				return await channel.ConnectAsync();
			}
			catch(Exception e)
			{
				Console.WriteLine(e.Message);
				throw e;
			}
		}

		private async Task SendAudio(IAudioClient client, Stream output)
		{
			const int minSize = 192000; //This is due to a bug with discordapi where it will hang if a sound less than 1 second is played.

			if(output.Length < minSize)
			{
				int paddingLength = minSize - (int)output.Length;

				if (output.CanSeek)
				{
					output.Seek(0, SeekOrigin.End);

					output.Write(new byte[paddingLength], 0, paddingLength);

					output.Seek(0, SeekOrigin.Begin);
				}
				else
				{
					Console.WriteLine(
						"Could not seek the output. Therefor the bug cannot be mitagated." +
						"Stopping before it hangs.");

					return;
				}
			}

			AudioOutStream discord = client.CreatePCMStream(AudioApplication.Mixed);

			await output.CopyToAsync(discord);
			await discord.FlushAsync();
			Console.WriteLine("Sent audio.");
		}
	}
}
