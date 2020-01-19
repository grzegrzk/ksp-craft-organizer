using System;
using UnityEngine;

namespace KspNalCommon
{
	public static class PluginLogger
	{
		private static IPluginLogger instance_;
		public static IPluginLogger instance
		{
			get
			{
				if(instance_ == null)
				{
					instance_ = new PluginLoggerImpl();
				}
				return instance_;
			}
			set
			{
				instance_ = value;
			}
		}

		public static void logTrace(object toLog) {
			instance.logTrace(toLog);
		}

		public static bool debug { get {
				return instance.debug;
			} 
		}

		public static void logDebug(object toLog){
			instance.logDebug(toLog);
		}

		internal static void logError(string toLog) {
			instance.logError(toLog);
		}

		internal static void logError(string toLog, Exception ex) {
			instance.logError(toLog, ex);
		}
	}

	public interface IPluginLogger
	{
		void logTrace(object toLog);
		bool debug { get; }

		void logDebug(object toLog);

		void logError(string toLog);

		void logError(string toLog, Exception ex);
	}

	public class PluginLoggerImpl: IPluginLogger
	{
		public static string pluginPrefix = "<undefined-plugin>";
		private static bool debug_ = false;
		private static bool whileReadingSettings = false;

		public void logTrace(object toLog)
		{
			if (debug)
			{
				Debug.Log("[" + pluginPrefix + "]" + toLog);
			}
		}

		public bool debug
		{
			get
			{
				if (!whileReadingSettings && PluginCommons.instance != null)
				{
					try
					{
						whileReadingSettings = true;
						pluginPrefix = PluginCommons.instance.getPluginLogName();
						debug_ = PluginCommons.instance.isDebug();
					}
					finally
					{
						whileReadingSettings = false;
					}
				}
				return debug_;
			}
		}

		public void logDebug(object toLog)
		{
			try
			{
				if (debug)
				{
					Debug.LogWarning("[" + pluginPrefix + "]" + toLog);
				}
				else
				{
					Debug.Log("[" + pluginPrefix + "]" + toLog);
				}
			}
			catch (Exception ex)
			{
				Debug.Log("Exception while logging");
				Debug.LogException(ex);
			}
		}

		public void logError(string toLog)
		{
			Debug.LogError("[" + pluginPrefix + "]" + toLog);
		}

		public void logError(string toLog, Exception ex)
		{
			logError(toLog);
			Debug.LogException(ex);
		}
	}
}

