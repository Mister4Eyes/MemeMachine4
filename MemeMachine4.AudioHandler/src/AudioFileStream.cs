using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemeMachine4.Audio.src
{
	class AudioFileStream : Stream
	{
		//We seperate these so it makes it easier to manipulate if we decide to change settings
#if DEBUG
		const int TimeOfChunks = 5; //How long each buffer should be in seconds
#else
		const int TimeOfChunks = 16; //How long each buffer should be in seconds
#endif
		const int SizeOfSecond = 192000; //Sizeof second in bytes

		const int TotalSize = TimeOfChunks * SizeOfSecond;

		string file;
		FileStream fileStream;
		MemoryStream TrashBuffer;
		MemoryStream ActiveBuffer;
		MemoryStream PassiveBuffer = null;
		private long FileLength = 0;
		bool initializeLater = false;
		//We assume the file coming in is raw data
		public AudioFileStream(string filename)
		{
			file = filename;
			if(File.Exists(filename))
			{
				try
				{
					Initialize();
				}
				catch (IOException)
				{
					initializeLater = true;
				}
			}
			else 
			{
				throw new FileNotFoundException();
			}
		}

		private void Initialize()
		{
			FileInfo fi = new FileInfo(file);
			Console.WriteLine("Initializing...");
			FileLength = fi.Length;
			fileStream = new FileStream(file, FileMode.Open, FileAccess.Read);

			if (FileLength > TotalSize)
			{
				byte[] buffer = new byte[TotalSize];
				fileStream.Read(buffer, 0, TotalSize);
				ActiveBuffer = new MemoryStream(buffer);
				LoadPassive();
			}
			else
			{
				//Since fileLength is less than an integer, we can use a cast without losing relevant data
				byte[] buffer = new byte[FileLength];
				fileStream.Read(buffer, 0, (int)FileLength);
				ActiveBuffer = new MemoryStream(buffer);
			}
		}
		private void LoadPassive()
		{
			Console.WriteLine("Loading new passive.");
			//Ensures passive buffer is null while this is happening.
			if(PassiveBuffer != null)
			{
				PassiveBuffer.Dispose();
				PassiveBuffer = null;
			}

			byte[] buffer = new byte[TotalSize];
			int length = fileStream.Read(buffer, 0, TotalSize);

			//Keeps null when 
			if(length == 0)
			{
				return;
			}
			else if(length != TotalSize)
			{
				byte[] tBuffer = new byte[length];
				for(int i = 0; i < length; ++i)
				{
					tBuffer[i] = buffer[i];
				}
				buffer = tBuffer;
			}

			PassiveBuffer = new MemoryStream(buffer);
		}

		~AudioFileStream()
		{
			fileStream.Close();
			fileStream.Dispose();
			ActiveBuffer.Dispose();
			if(PassiveBuffer!= null)
			{
				PassiveBuffer.Dispose();
			}
		}

		public override bool CanRead => true;

		public override bool CanSeek => false;

		public override bool CanWrite => false;

		public override long Length => FileLength;

		private long AbsolutePosition = 0;
		public override long Position
		{
			get => AbsolutePosition;

			//We aren't setting positions here
			//That would complicate things.
			//And this class is only being used for one thing (hopefully)
			set => throw new NotImplementedException(); }

		public override void Flush()
		{
			ActiveBuffer.Flush();
			PassiveBuffer.Flush();
		}
		
		public override int Read(byte[] buffer, int offset, int count)
		{
			//This is so you can queue multiple files at once.
			if (initializeLater)
			{
				initializeLater = false;
				Initialize();
			}
			int length = ActiveBuffer.Read(buffer, offset, count);
			if (length != count)
			{
				if(PassiveBuffer != null)
				{
					length += PassiveBuffer.Read(buffer, 0, count - length);

					TrashBuffer = ActiveBuffer;
					ActiveBuffer = PassiveBuffer;
					PassiveBuffer = null;

					Task.Run(() =>
					{
						TrashBuffer.Dispose();
						LoadPassive();
						return Task.CompletedTask;
					});
				}
				else
				{
					return length;
				}
			}

			AbsolutePosition += offset+length;
			return length;
		}
		
		//Not needed
		public override void SetLength(long value)
		{
			throw new NotImplementedException();
		}

		//Not needed
		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotImplementedException();
		}

		//Not needed
		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new NotImplementedException();
		}
	}
}
