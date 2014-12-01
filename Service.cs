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
	/// Controls the execution of btsync on the local computer, and it's configuration
	/// </summary>
	public static class Service
	{
		/// <summary>
		/// Creates a btsync client for interacting directly with btsync
		/// </summary>
		/// <returns></returns>
		public static BTSyncClient CreateApiClient()
		{
			if (!IsServiceRunning())
			{
				StartService();
			}

			return new BTSyncClient("127.0.0.1", 8877, "admin", "Jah286Hsy74549SdjfkgbRnJ824HsjSO854HDFgjs");
		}

		/// <summary>
		/// Starts the btsync service, if its not already running
		/// </summary>
		public static void StartService()
		{
			if (IsServiceRunning())
			{
				return;
			}

			try 
			{
				//change machine name, 
				string configPath = System.IO.Path.Combine(OKHOSTING.Tools.DefaultPaths.Custom, "BittorrentSync", "btsync.config");
				string configContent = File.ReadAllText(configPath);
				if(configContent.Contains("<@CurrentComputer.Name>"))
				{
					configContent = configContent.Replace("<@CurrentComputer.Name>", Computer.GetCurrentComputer().Name);
					File.WriteAllText(configPath, configContent);
				}

				ProcessStartInfo proc = new ProcessStartInfo();
				proc.FileName = System.IO.Path.Combine(OKHOSTING.Tools.DefaultPaths.Custom, "BittorrentSync", "BTSync.exe");
				proc.Arguments = string.Format(@"/config {0}", configPath);
				proc.RedirectStandardInput = false;
				proc.RedirectStandardOutput = true;
				proc.UseShellExecute = false;
				Process.Start(proc);
			}
			catch (Exception e)
			{
				OKHOSTING.Tools.Log.Write(e.Message, e.ToString(), OKHOSTING.Tools.Log.Exception);				
			}
			
			//OKHOSTING.Tools.AutoStart.SetAutoStart(proc.FileName + " " + proc.Arguments);
		}

		/// <summary>
		/// Is btsync service running?
		/// </summary>
		public static bool IsServiceRunning()
		{
			Process[] processlist = Process.GetProcesses();

			foreach (Process theprocess in processlist)
			{
				if (theprocess.ProcessName == "BTSync")
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Stops the btsync service, if its not already stopped
		/// </summary>
		public static void StopService()
		{
			if (IsServiceRunning())
			{
				CreateApiClient().Shutdown();
			}
		}
	}
}