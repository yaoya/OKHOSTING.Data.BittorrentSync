using Arendee.BTSyncLib;
using Arendee.BTSyncLib.Model;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OKHOSTING.Data.BittorrentSync
{
	/// <summary>
	/// A shared/synched directory configured on a computer
	/// </summary>
	[System.ComponentModel.DefaultProperty("Name")]
	public class SharedDirectory 
	{
		[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public Guid Oid
		{
			get;
			set;
		}
		
		/// <summary>
		/// Directory that this shared dir is synched to
		/// </summary>
		public Directory Directory
		{
			get; set;
		}

		/// <summary>
		/// The computer where this shared directory is synched on
		/// </summary>
		public Computer Computer
		{
			get; set;
		}

		/// <summary>
		/// Physical path of the directory, on the computer, where syn is configured
		/// </summary>
		public string Path
		{
			get; set;
		}

		public SharedDirectory()
		{
		}

		/// <summary>
		/// Checks the database, looking for shared directories that are supposed to be configured on the local computer.
		/// If shared directories are found in the database but not present in the local computer as sync folders, create them automatucally.
		/// If sunc fodlers are found locally but are not present in the database, delete them
		/// </summary>
		public static void SyncDirectories()
		{
			var localComputer = Computer.GetCurrentComputer();
			var apiClient = Service.CreateApiClient();
			var databaseDirectories = from sd in DataBase.Current.Set<SharedDirectory>() where sd.Computer == localComputer select sd;

			//loop database dirs and compare to btsync
			foreach (SharedDirectory dbDir in databaseDirectories)
			{
				//create folder if it doesnt exists yet
				//apiClient.RemoveFolder(dbDir.Directory.ReadWriteSecret).Wait();
				if (!apiClient.ExistFolder(dbDir.Directory.ReadWriteSecret))
				{
					apiClient.AddFolder(dbDir.Path, dbDir.Directory.ReadWriteSecret).Wait();

				}
			}

			//loop btsync folders and compare to database dirs
			foreach (SyncFolder syncFolder in apiClient.GetFoldersNoWait())
			{
				SharedDirectory dbDirectory = (from sd in DataBase.Current.Set<SharedDirectory>() where sd.Computer == localComputer && sd.Directory.ReadWriteSecret == syncFolder.Secret select sd).SingleOrDefault<SharedDirectory>();

				//delete sync folder if database dir no loger exists
				if (dbDirectory == null)
				{
					apiClient.RemoveFolder(syncFolder.Secret).Wait();
				}
			}
		}

		/// <summary>
		/// Creates the sync directory locally, waits until its synched and then destroy the sync folder.
		/// Usefull for one-time file/folder transfers from one machine to another
		/// </summary>
		/// <param name="sd"></param>
		public static void SyncOnceAndDestroy(SharedDirectory sd)
		{
			var apiClient = Service.CreateApiClient();
			OKHOSTING.Tools.Log.WriteDebug("" + sd.Oid);
			OKHOSTING.Tools.Log.WriteDebug("" + apiClient);

			//create folder
			if (!System.IO.Directory.Exists(sd.Path))
			{
				System.IO.Directory.CreateDirectory(sd.Path);
			}

			if (!apiClient.ExistFolder(sd.Directory.ReadWriteSecret))
			{
				apiClient.AddFolder(sd.Path, sd.Directory.ReadWriteSecret).Wait();
			}

			//wait until peer is found
			while (true)
			{
				var peers = apiClient.GetFoldersPeersNoWait(sd.Directory.ReadWriteSecret);

				if (peers != null)
				{
					if (peers.Count > 0)
					{
						break;
					}
				}

				System.Threading.Thread.Sleep(2000);
			}

			long lastUpload = long.MinValue, lastDownload = long.MinValue, lastSize = long.MinValue;

			while (true)
			{
				var peers = apiClient.GetFoldersPeersNoWait(sd.Directory.ReadWriteSecret);
				long currentUpload, currentDownload, currentSize;
				bool sameAsLast = false;

				var folder = apiClient.GetFolder(sd.Directory.ReadWriteSecret);
				folder.Wait();

				currentUpload = 0;
				currentDownload = 0;

				foreach (SyncPeer p in peers)
				{
					currentUpload += p.Upload;
					currentDownload += p.Download;
				}

				currentSize = folder.Result.Size;

				if (currentUpload == lastUpload && currentDownload == lastDownload && currentSize == lastSize)
				{
					sameAsLast = true;
				}
				else
				{
					sameAsLast = false;
				}

				if (!sameAsLast)
				{
					lastUpload = currentUpload;
					lastDownload = currentDownload;
					lastSize = currentSize;

					System.Threading.Thread.Sleep(60000);
				}
				else
				{
					break;
				}
			}
			apiClient.RemoveFolder(sd.Directory.ReadWriteSecret).Wait();
		}
	}
}