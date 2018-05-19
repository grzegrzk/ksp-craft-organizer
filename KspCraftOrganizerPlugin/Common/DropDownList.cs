using System.Collections.Generic;
using UnityEngine;
using System;

namespace KspNalCommon {

	public delegate void DrawOverlay();

	public interface IGuiOverlayContainer {

		void addOverlayAtStart(DrawOverlay drawOverlay);

		void addOverlayAtEnd(DrawOverlay drawOverlay);

		float maxX { get; }
}

	public class DropDownList<T> {

		public delegate string Stringizer(T value);

		private Texture2D downArrowImage;
		private List<T> _items;
		private Stringizer stringizer;
		private Texture2D hoverBackgroundTexture = UiUtils.createSingleColorTexture(new Color(200, 200, 200));
		private float openedListMaxItemWidth;
		private float openedListItemHeight;
		private Vector2 openedListScrollPosition = new Vector2(0, 0);
		private Rect openedListRect = new Rect(0, 0, 0, 0);
		private Rect openedListViewRect = new Rect(0, 0, 0, 0);
		private Rect dropDownRect = new Rect(0, 0, 0, 0);
		private bool itemChangedFlag = false;

		public DropDownList(ICollection<T> items, Stringizer stringizer) {
			this._items = new List<T>(items);
			this.stringizer = stringizer;
			this.maxOpenedListVisibleItemsCount = 6;
		}

		public List<T> items {
			get {
				return _items;
			}
			set {
				T oldSelectedItem = this.selectedItem;
				_items = value;
				this.selectedItem = oldSelectedItem;
			}

		}

		public void onGui(IGuiOverlayContainer overlayContainer, int width) {
			if (downArrowImage == null) {
				downArrowImage = UiUtils.loadIcon("dropdown-list.png");
			}
			GUIStyle style = new GUIStyle(GUI.skin.button);

			string label;
			if (someItemSelected) {
				label = stringizer(selectedItem);
			} else {
				label = "<none>";
			}
			GUIContent content = new GUIContent(label);

			float dropDownArrowSize = style.CalcSize(content).y;

			style.padding.right = (int)dropDownArrowSize;
			style.alignment = TextAnchor.MiddleLeft;

			Rect dropDownRectTemp = GUILayoutUtility.GetRect(content, style, GUILayout.Width(width));
			if (Event.current.type == EventType.Repaint) {
				dropDownRect = dropDownRectTemp;
				Vector2 perfectSize = style.CalcSize(content);

				if (perfectSize.x > dropDownRect.width) {
					while (perfectSize.x > dropDownRect.width && content.text.Length > 3) {
						content.text = content.text.Substring(0, content.text.Length - 1);
						perfectSize = style.CalcSize(content);
					}
				}
			}

			if (GUI.Button(dropDownRect, content, style)) {
				this.opened = !this.opened;
			}

			float dropDownArrorMargin = dropDownRect.height/3;
			Rect dropDownArrowRect = new Rect(dropDownRect.x + dropDownRect.width - dropDownRect.height + dropDownArrorMargin, dropDownRect.y + dropDownArrorMargin, dropDownRect.height - dropDownArrorMargin * 2, dropDownRect.height - dropDownArrorMargin * 2);
			GUI.DrawTexture(dropDownArrowRect, downArrowImage);

			if (opened) {
				GUIStyle scrollbarStyle = GUI.skin.verticalScrollbar;
				float scrollbarWidth = scrollbarStyle.CalcSize(new GUIContent("")).x + scrollbarStyle.margin.left;
				if (Event.current.type == EventType.Repaint) {
					int displayedCount = this.maxOpenedListVisibleItemsCount;
					if (displayedCount > _items.Count) {
						displayedCount = _items.Count;
					}
					bool scrollbarVisible = displayedCount < _items.Count;
					float optionalScrollbarWidth = scrollbarVisible ? scrollbarWidth : 0;
					openedListRect = new Rect(dropDownRect.x, dropDownRect.y + dropDownRect.height, openedListMaxItemWidth + optionalScrollbarWidth, openedListItemHeight * displayedCount);
					if (openedListRect.width < dropDownRect.width) {
						openedListRect.width = dropDownRect.width;
					}
					if (openedListRect.x + openedListRect.width > overlayContainer.maxX) {
						openedListRect.x = dropDownRect.x + dropDownRect.width - openedListRect.width;
					}
					openedListViewRect = new Rect(0, 0, openedListRect.width - optionalScrollbarWidth, openedListItemHeight*_items.Count);
				}

				overlayContainer.addOverlayAtStart(delegate () {
					if (Event.current.type != EventType.Repaint) {
						drawOpenedList();
						if (Event.current.type == EventType.MouseDown) {
							if (!openedListRect.Contains(Event.current.mousePosition)) {
								this.opened = false;
							}
						}
					}

				});
				overlayContainer.addOverlayAtEnd(delegate () {
					//
					//this is to prevent tooltips-through-list:
					//
					if (openedListRect.Contains(Event.current.mousePosition)) {
						GUI.tooltip = "";
					}

					if (Event.current.type == EventType.Repaint) {
						drawOpenedList();
					}

				});
			}
		}

		private void drawOpenedList() {
			GUIStyle listStyle = GUI.skin.window;

			GUI.Box(openedListRect, "", listStyle);
			using (GUI.ScrollViewScope scrollViewScrope = new GUI.ScrollViewScope(openedListRect, openedListScrollPosition, openedListViewRect)) {

				openedListScrollPosition = scrollViewScrope.scrollPosition;

				float itemX = 0;
				float itemY = 0;
				float itemWidth = openedListViewRect.width;
				int currentIndex = 0;
				this.openedListMaxItemWidth = 0;
				foreach (T item in _items) {
					GUIContent itemContent = new GUIContent(stringizer(item));

					GUIStyle itemStyle = new GUIStyle();
					itemStyle.normal.textColor = new Color(200, 200, 200);
					if (currentIndex == selectedItemIndex) {
						itemStyle.hover.textColor = Color.black;
						itemStyle.normal.background = hoverBackgroundTexture;
					}

					itemStyle.hover.background = hoverBackgroundTexture;
					itemStyle.onHover.background = hoverBackgroundTexture;
					itemStyle.hover.textColor = Color.black;
					itemStyle.onHover.textColor = Color.black;

					itemStyle.padding = new RectOffset(4, 4, 4, 4);
					Vector2 itemSize = itemStyle.CalcSize(itemContent);
					float itemHeight = itemStyle.CalcSize(itemContent).y;
					this.openedListItemHeight = itemHeight;
					this.openedListMaxItemWidth = Math.Max(itemSize.x, openedListMaxItemWidth);
					if (GUI.Button(new Rect(itemX, itemY, itemWidth, itemHeight), itemContent, itemStyle)) {
						this.selectedItemIndex = currentIndex;
						this.itemChangedFlag = true;
						this.opened = false;
					}
					itemY += itemHeight;
					++currentIndex;
				}
			}
		}

		internal bool getAndClearItemChangedByUserFlag() {
			bool toRet = itemChangedFlag;
			itemChangedFlag = false;
			return toRet;
		}

		public int maxOpenedListVisibleItemsCount { get; set;}

		public bool opened { get; private set;}

		public int selectedItemIndex { get; set; }

		public bool someItemSelected {
			get {
				return selectedItemIndex < _items.Count && selectedItemIndex >= 0;
			}
		}

		public T selectedItem {
			get {
				if (someItemSelected) {
					return _items[selectedItemIndex];
				} else {
					return default(T);
				}
			}
			set {
				int index = 0;
				bool found = false;
				int oldSelected = selectedItemIndex;
				foreach (T item in _items) {
					if (EqualityComparer<T>.Default.Equals(item, value)) {
						selectedItemIndex = index;
						found = true;
					}
					++index;					
				}
				if (!found) {
					selectedItemIndex = -1;
				}
			}
		}
	}

}

