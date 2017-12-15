using System.Diagnostics.Contracts;
using System.IO.MemoryMappedFiles;

namespace LoadBinaryPlugin
{
	internal class MemoryMappedFileInfo
	{
		public string Path { get; }
		public int Size { get; }
		public MemoryMappedFile File { get; }

		public MemoryMappedFileInfo(string path, int size, MemoryMappedFile file)
		{
			Contract.Requires(path != null);
			Contract.Requires(file != null);

			Path = path;
			Size = size;
			File = file;
		}
	}
}
