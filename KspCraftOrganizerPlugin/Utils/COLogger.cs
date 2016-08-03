using System;
using UnityEngine;

namespace KspCraftOrganizer
{
	/**
	 * COLogger = Craft Organizer Logger
	 */
	public static class COLogger
	{
		private static bool debug_ = false;
		private static bool whileReadingSettings = false;

		public static void logTrace(object toLog) {
			if (debug) {
				Debug.Log("[CraftOrganizer]" + toLog);
			}
		}

		public static bool debug { get {
				if (!whileReadingSettings && SettingsService.instance != null) {
					try {
						whileReadingSettings = true;
						debug_ = SettingsService.instance.getPluginSettings().debug;
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

