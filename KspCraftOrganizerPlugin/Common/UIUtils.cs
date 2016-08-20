using System.IO;
using UnityEngine;

namespace KspNalCommon {
	
	public static class UiUtils {
		public static Texture2D loadIcon(string fileName) {

			Texture2D texture;
			if (fileName == null) {
				texture = Texture2D.blackTexture;
			} else {
				texture = UiUtils.loadTextureFrom(Globals.combinePaths(PluginCommons.instance.getPluginDirectory(), "icons", fileName));
			}
			return texture;

		}

		public static Texture2D loadTextureFrom(string file) {

			Texture2D tex = null;
			byte[] fileData;

			if (File.Exists(file)) {
				fileData = File.ReadAllBytes(file);
				tex = new Texture2D(2, 2);
				tex.LoadImage(fileData);
			} else {
				PluginLogger.logError("Cannot find " + file);
				tex = Texture2D.blackTexture;
			}

			return tex;
		}

		public static Texture2D createSingleColorTexture(Color color) {
			Texture2D tex = new Texture2D(1, 1);
			tex.SetPixel(0, 0, color);
			return tex;
		}
	}
}

