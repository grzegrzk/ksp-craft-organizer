using System;
using UnityEngine;

namespace KspCraftOrganizer
{
	public static class COLogger
	{
		public static void Log(object toLog){
			Debug.Log ("[GK]" + toLog);
		}
	}
}

