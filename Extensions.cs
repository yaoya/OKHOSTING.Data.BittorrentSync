using Arendee.BTSyncLib;
using Arendee.BTSyncLib.Model;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OKHOSTING.Data.BittorrentSync
{
	public static class Extensions
	{
		public static bool ExistFolder(this BTSyncClient apiClient, string secret)
		{
			var folderTask = apiClient.GetFolder(secret);

			try
			{
				folderTask.Wait();
			}
			catch
			{
				return false;
			}

			return true;
		}

		public static List<SyncFolder> GetFoldersNoWait(this BTSyncClient apiClient)
		{
			var foldersTask = apiClient.GetFolders();
			foldersTask.Wait();
			return foldersTask.Result;
		}

		public static List<SyncPeer> GetFoldersPeersNoWait(this BTSyncClient apiClient, string secret)
		{
			var foldersTask = apiClient.GetFolderPeers(secret);
			foldersTask.Wait();

			return foldersTask.Result;
		}

		public static bool IsSynched(this BTSyncClient apiClient, string secret, string path)
		{
			var peers = apiClient.GetFoldersPeersNoWait(secret);
			long currentBandwith = 0;

			if (peers == null)
			{
				return true;
			}

			foreach (SyncPeer p in peers)
			{
				currentBandwith += p.Upload;
				currentBandwith += p.Download;
			}

			return currentBandwith == 0;
		}
	}
}