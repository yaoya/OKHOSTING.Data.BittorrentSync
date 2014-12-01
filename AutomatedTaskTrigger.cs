using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OKHOSTING.ERP.HR;

namespace OKHOSTING.Data.BittorrentSync
{
	public class AutomatedTaskTrigger
	{
		public const string CommandDirectoryName = "OKHOSTING.Tasks.AutomatedTask";

		public static string CommandDirectoryPath
		{
			get
			{
				return System.IO.Path.Combine(OKHOSTING.Tools.DefaultPaths.Custom, "AutomatedTask", Computer.GetCurrentComputer().Name);
			}
		}

		public void Start()
		{
			DataBase.Current.AfterInsert += AutomatedTask_AfterInsert;

			//run btsync
			Service.StartService();

			//make sure Directory exists					
			var commandDirectory = (from d in DataBase.Current.Set<Directory>() where d.Name == CommandDirectoryName select d).SingleOrDefault<Directory>();

			if (commandDirectory == null)
			{
				commandDirectory = new Directory();
				commandDirectory.Name = "OKHOSTING.ERP.HR.AutomatedTask";
				DataBase.Current.Set<Directory>().Add(commandDirectory);
			}

			//make sure the current computer is registered
			var currentComputer = Computer.GetCurrentComputer();

			//make sure command's SharedDirectory exist
			var localSharedDirectory = (from sd in DataBase.Current.Set<SharedDirectory>() where sd.Computer == currentComputer && sd.Directory == commandDirectory select sd).SingleOrDefault<SharedDirectory>();

			if (!System.IO.Directory.Exists(CommandDirectoryPath))
			{
				System.IO.Directory.CreateDirectory(CommandDirectoryPath);
			}

			if (localSharedDirectory == null)
			{
				localSharedDirectory = new SharedDirectory();
				localSharedDirectory.Computer = currentComputer;
				localSharedDirectory.Directory = commandDirectory;
				localSharedDirectory.Path = CommandDirectoryPath;
				DataBase.Current.Set<Directory>().Add(commandDirectory);
			}

			DataBase.Current.SaveChanges();

			SharedDirectory.SyncDirectories();

			//listen for commands
			System.IO.FileSystemWatcher watcher = new System.IO.FileSystemWatcher();
			watcher.Path = CommandDirectoryPath;
			watcher.Filter = "*.*";
			watcher.Created += CommandFile_Created;
			watcher.EnableRaisingEvents = true;

			System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
		}

		static void AutomatedTask_AfterInsert(DataBase db, DataBaseOperationEventArgs e)
		{
			if (!(e.Object is AutomatedTask))
			{
				return;
			}

			if (!System.IO.Directory.Exists(CommandDirectoryPath))
			{
				System.IO.Directory.CreateDirectory(CommandDirectoryPath);
			}

			System.IO.File.WriteAllText(System.IO.Path.Combine(CommandDirectoryPath, ((AutomatedTask)e.Object).Oid.ToString()), string.Empty);
		}

		void CommandFile_Created(object sender, System.IO.FileSystemEventArgs e)
		{
			AutomatedTask task = (from t in DataBase.Current.Set<AutomatedTask>() where t.Oid.ToString() == e.Name && t.Computer.Oid == Computer.GetCurrentComputer().Oid select t).SingleOrDefault<AutomatedTask>();

			if (task != null)
			{
				task.Execute();
				System.IO.File.Delete(e.FullPath);
			}
		}

		protected void Stop()
		{
			Service.StopService();
		}
	}
}
