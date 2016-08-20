using System;
namespace KspNalCommon {

	public class GuiStyleOption {

		public static readonly GuiStyleOption Ksp = new GuiStyleOption("KSP", "KSP");
		public static readonly GuiStyleOption Default = new GuiStyleOption("Default", "Default");

		public static readonly string[] SKIN_DISPLAY_OPTIONS = { GuiStyleOption.Default.displayName, GuiStyleOption.Ksp.displayName };
		public static readonly GuiStyleOption[] SKIN_STATES = { GuiStyleOption.Default, GuiStyleOption.Ksp };

		private string _id;
		private string _displayName;

		public string id { get { return _id; } }
		public string displayName { get { return _displayName; } }

		private GuiStyleOption(string id, string displayName) {
			this._id = id;
			this._displayName = displayName;
		}

	}
}

