using System;
using UnityEngine;
using System.Collections.Generic;

namespace KspNalCommon {
	public class ParagraphBoxDrawer {

		private static readonly float MARGINX = 10;
		private static readonly float MARGINY = 0;

		private class ParagraphDrawData {
			public string text;
			public GUIStyle style;
			public float height;
		}

		private float width;
		private float contentHeight = MARGINY * 2;
		private List<ParagraphDrawData> paragraphs = new List<ParagraphDrawData>();

		public ParagraphBoxDrawer(float width) {
			this.width = width;
		}

		public void addParagraph(string text, GUIStyle style) {
			ParagraphDrawData data = new ParagraphDrawData();
			data.text = text;
			data.style = style;
			data.height = style.CalcHeight(new GUIContent(text), width - MARGINX * 2);
			contentHeight += data.height;
			paragraphs.Add(data);
		}

		public void drawAt(Vector2 position) {
			float curY = position.y;
			foreach (ParagraphDrawData p in paragraphs) {
				GUI.Label(new Rect(position.x + MARGINX, curY + MARGINY, width - MARGINX * 2, p.height), p.text, p.style);
				curY += p.height;
			}
		}

		public Vector2 contentSize {
			get {
				return new Vector2(width, contentHeight);
			}
		}
	}
}

