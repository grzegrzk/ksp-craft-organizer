using System;
using UnityEngine;

namespace KspNalCommon {

	public interface CommonPluginProperties {

		bool canGetIsDebug();

		string getPluginLogName();

		string getPluginDirectory();

		bool isDebug();

		GUISkin kspSkin();

		int getInitialWindowId();
	}

	public static class PluginCommons {

		public static CommonPluginProperties instance { get; private set;}

		public static void init(CommonPluginProperties properties) {
			instance = properties;
		}

}

}

