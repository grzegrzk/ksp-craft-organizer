using System;
using UnityEngine;

namespace KspNalCommon
{
	public static class PluginLogger
	{
		public static string pluginPrefix = "<undefined-plugin>";
		private static bool debug_ = false;
		private static bool whileReadingSettings = false;

		public static void logTrace(object toLog) {
			if (debug) {
				Debug.Log("[" + pluginPrefix + "]" + toLog);
			}
		}

		public static bool debug { get {
				if (!whileReadingSettings && PluginCommons.instance.canGetIsDebug()) {
					try {
						whileReadingSettings = true;
						debug_ = PluginCommons.instance.isDebug();
					} finally {
						whileReadingSettings = false;
					}
				}
				return debug_;
			} 
		}

		public static void logDebug(object toLog){
			if (debug) {
				Debug.LogWarning("[CraftOrganizer]" + toLog);
			} else {
				Debug.Log("[CraftOrganizer]" + toLog);
			}
		}

		internal static void logError(string toLog) {
			Debug.LogError("[CraftOrganizer]" + toLog);
		}
}
}

