using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace KspCraftOrganizer
{
	public class Globals
	{

		public static readonly Rect ZERO_RECT = new Rect(0, 0, 0, 0);
		public delegate void Procedure();
		public delegate T Function<T, A>(A arg);

		public static string combinePaths(string firstPart, params string[] restPaths) {
			string toRet = firstPart;
			foreach(string p in restPaths) {
				toRet = Path.Combine(toRet, p);
			}
			return toRet;
		}

		public static string join<T>(ICollection<T> collection, Function<string, T> function, string spearator) { 
			StringBuilder sb = new StringBuilder();
			bool first = true;
			foreach (T element in collection) {
				if (!first) {
					sb.Append(spearator);
				}
				sb.Append(function(element));
				first = false;
			}
			return sb.ToString();
		}
	}
}

