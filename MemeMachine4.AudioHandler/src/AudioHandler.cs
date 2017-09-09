using System;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Discord;
using Discord.Audio;

using MemeMachine4.Audio.src;

using Mister4Eyes.GeneralUtilities;

namespace MemeMachine4.Audio
{
	public class AudioHandler
	{
		Task aloop;
		string ffmpegLoc;
		ConcurrentQueue<Tuple<IVoiceChannel, Stream>> AudioQueue = new ConcurrentQueue<Tuple<IVoiceChannel, Stream>>();

		ConcurrentDictionary<IVoiceChannel, ConcurrentQueue<Stream>> SendingStreams = new ConcurrentDictionary<IVoiceChannel, ConcurrentQueue<Stream>>();
		ConcurrentDictionary<IVoiceChannel, bool> StopRequest = new ConcurrentDictionary<IVoiceChannel, bool>();
		int QueueLength
		{
			get
			{
				return AudioQueue.Count;
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
				Console.WriteLine("Getting file stream.");
				Stream outStream = CreateStream(path);

				if(outStream != null)
				{
					Console.WriteLine("Enqueuing.");
					PushQueue(channel, outStream);
				}
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
			AudioQueue.Enqueue(new Tuple<IVoiceChannel, Stream>(channel, stream));
		}

		private Tuple<IVoiceChannel, Stream> PopQueue()
		{
			if (AudioQueue.IsEmpty)
			{
				return null;
			}
			else
			{
				Tuple<IVoiceChannel, Stream> outp;
				if (AudioQueue.TryDequeue(out outp))
				{
					return outp;
				}
				else
				{
					return null;
				}
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

					if (!SendingStreams.ContainsKey(channel))
					{
						if(SendingStreams.TryAdd(channel, new ConcurrentQueue<Stream>()))
						{
							if(StopRequest.TryAdd(channel, false))
							{
								SendingStreams[channel].Enqueue(data);

								//No need to await here
								Task.Run(() => SendAudio(channel));
							}
							else
							{
								ConcurrentQueue<Stream> cq;
								SendingStreams.TryRemove(channel, out cq);
								Console.WriteLine("Unknown failure...");
							}
						}
					}
					else
					{
						SendingStreams[channel].Enqueue(data);
					}
				}

				//Here to not eat up a shitload of cpu for no reason.
				// 1/10th of a second is fast enough where someone wont notice any delay according to this
				// http://www.atsc.org/wp-content/uploads/pdf/audio_seminar/12%20-%20JONES%20-%20Audio%20and%20Video%20synchronization-Status.pdf
				await Task.Delay(100);
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
		
		private AudioFileStream CreateStream(string path)
		{
			FileInfo file = new FileInfo(path);

			//Name without the extension
			string name		= file.Name.Substring(0, file.Name.Length - file.Extension.Length);
			string rawFile	= $"./RawData/{name}.raw";
			if (!Directory.Exists("./RawData"))
			{
				Directory.CreateDirectory("./RawData");
			}
			if (!File.Exists(rawFile))
			{
				var ffmpeg = new ProcessStartInfo
				{
					FileName = ffmpegLoc,//TODO: Get config file to change the location of ffmpeg
					Arguments = $"-i {path} -ac 2 -f s16le -ar 48000 -acodec pcm_s16le ./RawData/{name}.raw",
					UseShellExecute = false,
					RedirectStandardOutput = true,
				};
				Process process = Process.Start(ffmpeg);
				process.WaitForExit();

				Console.WriteLine(rawFile);
				if (!File.Exists(rawFile))
				{
					return null;
				}
			}

			return new AudioFileStream(rawFile);
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

		private async Task SendAudio(IVoiceChannel channel)
		{
			IAudioClient client = await JoinChannel(channel);
			const int lengthOfSecond = 192000;
			const int minSize = lengthOfSecond * 5;
			AudioOutStream discord = client.CreatePCMStream(AudioApplication.Mixed);

			int length;
			do
			{
				length = SendingStreams[channel].Count;

				if (length != 0)
				{
					Stream cStream;
					if(!SendingStreams[channel].TryDequeue(out cStream))
					{
						cStream = null;
					}
					byte[] Chunk = new byte[minSize];

					Console.WriteLine("Sending new audio.");

					//Doing this bit syncronously in hopes it sends everything nicely.
					while(!StopRequest[channel] && 0 != cStream.Read(Chunk, 0, minSize))
					{
						discord.Write(Chunk, 0, minSize);
						Chunk = new byte[minSize];
					}
					discord.Flush();
					cStream.Dispose();
					StopRequest[channel] = false;
				}
			} while (length != 0);

			Console.WriteLine("Sent all audio.");

			await client.StopAsync();

			ConcurrentQueue<Stream> data;
			bool sq;
			SendingStreams.TryRemove(channel, out data);
			StopRequest.TryRemove(channel, out sq);

			//Cleans up any data remaining that somehow got through
			//More of a sanity check than anything
			//And with threads, sanity checks are a logical thing to have
			while (!data.IsEmpty)
			{
				Stream str;
				if(data.TryDequeue(out str))
				{
					str.Dispose();
				}
			}
		}

		public Task StopAudio(IVoiceChannel channel)
		{
			if (StopRequest.ContainsKey(channel))
			{
				StopRequest[channel] = true;
			}

			return Task.CompletedTask;
		}
	}
}
