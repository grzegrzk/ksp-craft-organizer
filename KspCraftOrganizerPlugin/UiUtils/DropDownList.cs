using System.Collections.Generic;
using UnityEngine;

namespace KspCraftOrganizer {

	public delegate void DrawOverlay();

	public interface IGuiOverlayContainer {
		void addOverlay(DrawOverlay drawOverlay);
	}

	public class DropDownList<T> {

		public delegate string Stringizer(T value);
		Texture2D texture;
		private ICollection<T> items;
		private Stringizer stringizer;
		private Texture2D texBack = UiUtils.createSingleColorTexture(new Color(207, 207, 207));

		public DropDownList(ICollection<T> items, Stringizer stringizer) {
			this.items = items;
			this.stringizer = stringizer;
		}

		Vector2 openedListScrollPosition = new Vector2(0, 0);

		public void onGui(IGuiOverlayContainer overlayContainer) {
			if (texture == null) {
				texture = UiUtils.loadIcon("dropdown-list.png");
			}
			GUIStyle style = new GUIStyle(GUI.skin.button);
			//style.padding.left += (int)5;
			//style.padding.left = style.padding.right = 0;
			//style.border.left = style.border.right = 0;
			string label = "drop down list";// + " " + style.lineHeight;

			GUIContent content = new GUIContent(label);

			float height = style.CalcSize(content).y;
			//style.margin.right = (int)style.fixedHeight;
			//style.isHeightDependantOnWidth;
			style.padding.right = (int)height;
			style.alignment = TextAnchor.MiddleLeft;
			Rect rect = GUILayoutUtility.GetRect(content, style);
			//GUI.Label(new Rect(rect.position, new Vector2(rect.width + rect.height, rect.height)), label, style);
			if (GUI.Button(rect, label, style)) {
				this.opened = !this.opened;
			}
			float margin = rect.height/3;
			GUI.DrawTexture(new Rect(rect.x + rect.width - rect.height + margin, rect.y + margin, rect.height - margin*2, rect.height - margin*2), texture);

			if (opened) {
				overlayContainer.addOverlay(delegate () {
					Rect openedListPosition = new Rect(rect.x, rect.y + rect.height, rect.width, rect.height*4);
					float scrollbarWidth = GUI.skin.verticalScrollbar.CalcSize(new GUIContent("")).x;
					Rect openedListViewRect = new Rect(0, 0, openedListPosition.width - scrollbarWidth - 4, rect.height * 9);
					using (var scrollViewScrope = new GUI.ScrollViewScope(openedListPosition, openedListScrollPosition, openedListViewRect)) {
						openedListScrollPosition = scrollViewScrope.scrollPosition;

						float itemX = 0;//openedListPosition.x;
						float itemY = 0;//openedListPosition.y;
						float itemWidth = openedListViewRect.width;
						foreach (T item in items) {
							GUIContent itemContent = new GUIContent(stringizer(item));
							//GUIStyle itemStyle = GUI.skin.button;

							GUIStyle itemStyle = new GUIStyle();
							itemStyle.normal.textColor = new Color(207, 207, 207);
							itemStyle.hover.background = texBack;
							itemStyle.onHover.background = texBack;
							itemStyle.hover.textColor = Color.black;
							itemStyle.onHover.textColor = Color.black;
							itemStyle.padding = new RectOffset(4, 4, 3, 4);

							float itemHeight = itemStyle.CalcSize(itemContent).y;
							GUI.Button(new Rect(itemX, itemY, itemWidth, itemHeight), itemContent, itemStyle);
							itemY += itemHeight;
						}
					}
				});
			}
			//GUI.Label(new Rect(rect.position +  new Vector2(rect.width-1, 0), new Vector2(rect.height, rect.height)), "\\/", style);
		}

		public bool opened { get; private set;}

		public T selectedItem { get; private set; }
	}

}

