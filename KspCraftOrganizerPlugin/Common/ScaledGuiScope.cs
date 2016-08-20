using System;
using System.Collections.Generic;
using UnityEngine;

namespace KspNalCommon {
	public class ScaledGuiScope : IDisposable {
		private Matrix4x4 oldMatrix;
		public ScaledGuiScope(float scale, float x, float y) {

			//We want to scale the gui in that way gui widget with position x will remain on position x,
			//but everything beyond will be scaled, so that:
			//
			//previous point position -> new point position
			//						x -> x
			//					(x+1) -> (x+1*scale)
			//					(x+2) -> (x+2*scale)
			//
			//To do so we create scaling matrix, but it would translate things that way:
			//						x -> x*scale
			//					 (x+1)->(x*scale) + (1*scale)
			//
			//So we add translation vector to correct it: 
			//					x * (1-scale) 
			//
			//So it becomes:
			//
			//						x -> x*scale + x*(1-scale) = x
			//					  x+1 -> x*scale + 1*scale + x*(1-scale) = x + 1 * scale
			//
			//We have to take into account that "x" is not absolute because somewhere in the Unity GUI subsystem
			//there is stored information about current clip rect used for windows, scroll rects, groups, etc, so everywhere where
			//there is "x" in above examples we need to take (x + origin). It would be good to obtain origin using 
			//GUIUtility.GUIToScreenPoint(new Vector2(x, y)) but this function seems to be implemented incorrectly if there is some
			//GUI transformation matrix. I do not know how to obtain true origin of GUI element, so we track the origin by ourselves.
			//

			this.oldMatrix = GUI.matrix;
			Vector2 origin = GroupScope.origin;
			float multiplier = (1 - scale);
			GUI.matrix = oldMatrix * Matrix4x4.TRS(
				new Vector3((origin.x + x) * multiplier, (origin.y + y) * multiplier, 0),
				Quaternion.identity,
				new Vector3(scale, scale, 1));
		}

		public ScaledGuiScope(float scale, Vector2 from) : this(from.x, from.y, scale) {
			//
		}

		public void Dispose() {
			GUI.matrix = oldMatrix;
		}
	}

	public class InsideWindowScope : IDisposable {
		public InsideWindowScope(Vector2 windowPos) {
			GroupScope.putOrigin(windowPos);
		}

		public void Dispose() {
			GroupScope.popOrigin();
		}
	}


	public class LayoutScrollScope : IDisposable {

		public Vector2 scroll { get; set;}

		public LayoutScrollScope(Rect rect, Vector2 scroll) {
			this.scroll = GUILayout.BeginScrollView(scroll);
			GroupScope.putOrigin(rect.position);
			GroupScope.putOrigin(-this.scroll);
		}

		public void Dispose() {
			GroupScope.popOrigin();
			GroupScope.popOrigin();
			GUILayout.EndScrollView();
		}
	}

	public class GroupScope : IDisposable {
		private static List<Vector2> origins = new List<Vector2>();
		public GroupScope(Rect rect) {
			GUI.BeginGroup(rect);
			putOrigin(rect.position);
		}

		public static void putOrigin(Vector2 origin) {
			origins.Add(GroupScope.origin + origin);
		}

		public static void popOrigin() {
			origins.RemoveAt(origins.Count - 1);
		}

		public static Vector2 origin {
			get {
				if (origins.Count == 0) {
					return Vector2.zero;
				} else {
					return origins[origins.Count - 1];
				}
			}
		}

		public void Dispose() {
			GUI.EndGroup();
			popOrigin();
		}
	}
}