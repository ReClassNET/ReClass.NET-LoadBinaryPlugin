using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ReClassNET.Plugins;
using RGiesecke.DllExport;
using static ReClassNET.Memory.NativeHelper;

namespace LoadBinaryPlugin
{
	public class LoadBinaryPluginExt : Plugin
	{
		private static object sync = new object();

		private static IPluginHost host;

		private static string currentFile;

		private static Dictionary<IntPtr, MemoryMappedFile> openFiles;

		public override Image Icon => Properties.Resources.icon;

		public override bool Initialize(IPluginHost host)
		{
			Contract.Requires(host != null);

			//System.Diagnostics.Debugger.Launch();

			if (host == null)
			{
				throw new ArgumentNullException(nameof(host));
			}

			openFiles = new Dictionary<IntPtr, MemoryMappedFile>();

			return true;
		}

		public override void Terminate()
		{
			foreach (var kv in openFiles)
			{
				kv.Value.Dispose();
			}
			openFiles.Clear();

			host = null;
		}

		/// <summary>Gets a <see cref="MemoryMappedFile"/> by its plugin internal identifier.</summary>
		/// <param name="id">The identifier.</param>
		/// <returns>The file or null if the identifier doesn't exist.</returns>
		private static MemoryMappedFile GetMappedFileById(IntPtr id)
		{
			MemoryMappedFile file;
			openFiles.TryGetValue(id, out file);
			return file;
		}

		/// <summary>Logs the exception and removes the file.</summary>
		/// <param name="id">The identifier.</param>
		/// <param name="ex">The exception.</param>
		private static void LogErrorAndRemoveFile(IntPtr id, Exception ex)
		{
			Contract.Requires(ex != null);

			MemoryMappedFile file;
			if (openFiles.TryGetValue(id, out file))
			{
				file.Dispose();
			}

			openFiles.Remove(id);

			host.Logger.Log(ex);
		}

		/// <summary>Queries if the file is valid.</summary>
		/// <param name="process">The file to check.</param>
		/// <returns>True if the file is valid, false if not.</returns>
		[DllExport(CallingConvention = CallingConvention.StdCall)]
		public static bool IsProcessValid(IntPtr process)
		{
			lock (sync)
			{
				return GetMappedFileById(process) != null;
			}
		}

		/// <summary>Opens the file.</summary>
		/// <param name="pid">The file id.</param>
		/// <param name="desiredAccess">The desired access. (ignored)</param>
		/// <returns>A plugin internal handle to the file.</returns>
		[DllExport(CallingConvention = CallingConvention.StdCall)]
		private static IntPtr OpenRemoteProcess(int pid, int desiredAccess)
		{
			lock (sync)
			{
				try
				{
					var file = MemoryMappedFile.CreateFromFile(currentFile);

					var handle = file.SafeMemoryMappedFileHandle.DangerousGetHandle();

					openFiles.Add(handle, file);

					return handle;
				}
				catch (Exception ex)
				{
					host.Logger.Log(ex);
				}
			}

			return IntPtr.Zero;
		}

		/// <summary>Closes the file.</summary>
		/// <param name="process">The file to close.</param>
		[DllExport(CallingConvention = CallingConvention.StdCall)]
		private static void CloseRemoteProcess(IntPtr process)
		{
			lock (sync)
			{
				MemoryMappedFile file;
				if (openFiles.TryGetValue(process, out file))
				{
					openFiles.Remove(process);

					file.Dispose();
				}
			}
		}

		/// <summary>Reads memory of the file.</summary>
		/// <param name="process">The process to read from.</param>
		/// <param name="address">The address to read from.</param>
		/// <param name="buffer">The buffer to read into.</param>
		/// <param name="size">The size of the memory to read.</param>
		/// <returns>True if it succeeds, false if it fails.</returns>
		[DllExport(CallingConvention = CallingConvention.StdCall)]
		private static bool ReadRemoteMemory(IntPtr process, IntPtr address, IntPtr buffer, int size)
		{
			lock (sync)
			{
				var file = GetMappedFileById(process);
				if (file != null)
				{
					try
					{
						using (var stream = file.CreateViewStream(address.ToInt64(), size))
						{
							var tempBuffer = new byte[size];
							stream.Read(tempBuffer, 0, size);

							Marshal.Copy(tempBuffer, 0, buffer, size);

							return true;
						}
					}
					catch (UnauthorizedAccessException)
					{
						// address + size >= file size
					}
					catch (Exception ex)
					{
						LogErrorAndRemoveFile(process, ex);
					}
				}

				return false;
			}
		}

		/// <summary>Not supported.</summary>
		/// <param name="process">The file to write to.</param>
		/// <param name="address">The address to write to.</param>
		/// <param name="buffer">The memory to write.</param>
		/// <param name="size">The size of the memory to write.</param>
		/// <returns>True if it succeeds, false if it fails.</returns>
		[DllExport(CallingConvention = CallingConvention.StdCall)]
		private static bool WriteRemoteMemory(IntPtr process, IntPtr address, IntPtr buffer, int size)
		{
			// No write support.

			return false;
		}

		/// <summary>Opens a file browser dialog and reports the selected file.</summary>
		/// <param name="callbackProcess">The callback which gets called for the selected file.</param>
		[DllExport(CallingConvention = CallingConvention.StdCall)]
		private static void EnumerateProcesses(EnumerateProcessCallback callbackProcess)
		{
			if (callbackProcess == null)
			{
				return;
			}

			using (var ofd = new OpenFileDialog())
			{
				ofd.Filter = "All|*.*";

				if (ofd.ShowDialog() == DialogResult.OK)
				{
					currentFile = ofd.FileName;

					callbackProcess(currentFile.GetHashCode(), currentFile);
				}
			}
		}

		/// <summary>Not supported.</summary>
		/// <param name="process">The process.</param>
		/// <param name="callbackSection">The callback which gets called for every section.</param>
		/// <param name="callbackModule">The callback which gets called for every module.</param>
		[DllExport(CallingConvention = CallingConvention.StdCall)]
		private static void EnumerateRemoteSectionsAndModules(IntPtr process, EnumerateRemoteSectionCallback callbackSection, EnumerateRemoteModuleCallback callbackModule)
		{
			// No modules and sections here.
		}
	}
}
