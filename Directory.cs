using Arendee.BTSyncLib;
using Arendee.BTSyncLib.Model;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using OKHOSTING.Tools.Extensions;

namespace OKHOSTING.Data.BittorrentSync
{
	/// <summary>
	/// A directory is an group of secrets, that not necessary are installed or synced in any computer, it's just the secrets
	/// </summary>
	public class Directory
	{
		[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public Guid Oid
		{
			get;
			set;
		}
		
		[StringLength(200)]
		[Required]
		public String Name
		{
			get; set;
		}

		[StringLength(33)]
		public String ReadWriteSecret
		{
			get; set;
		}

		[StringLength(33)]
		public String ReadOnlySecret
		{
			get; set;
		}

		[StringLength(33)]
		public String EncryptionSecret
		{
			get; set;
		}

		/// <summary>
		/// List of shared directories, which have this directory synched in a computer
		/// </summary>
		public virtual System.Linq.IQueryable<SharedDirectory> SharedDirectories
		{
			get; set;
		}

		public Directory()
		{
		}

		/// <summary>
		/// Creates new secrets, use for creating new sync directories
		/// </summary>
		public void CreateNewSecrets()
		{
			BTSyncClient client = Service.CreateApiClient();

			var task = client.GenerateSecrets();
			task.Wait();

			this.ReadWriteSecret = task.Result.ReadWrite;
			this.ReadOnlySecret = task.Result.ReadOnly;
			this.EncryptionSecret = task.Result.Encryption;
		}
	}
}