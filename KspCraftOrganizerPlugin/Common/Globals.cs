using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace KspNalCommon
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

		public static string join(ICollection<string> collection, string separator) {
			return join(collection, s => s, separator);
		}

		public static string join<T>(ICollection<T> collection, Function<string, T> function, string separator) { 
			StringBuilder sb = new StringBuilder();
			bool first = true;
			foreach (T element in collection) {
				if (!first) {
					sb.Append(separator);
				}
				sb.Append(function(element));
				first = false;
			}
			return sb.ToString();
		}
	}
}

