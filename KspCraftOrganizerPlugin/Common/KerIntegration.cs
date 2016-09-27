using System;
using System.Collections.Generic;
using System.Reflection;

namespace KspNalCommon {
	public class KerIntegration {

		private Type vesselSimulationType;
		private MethodInfo prepareSimulation;
		private MethodInfo runSimulation;

		private Type stageType;
		private FieldInfo totalDeltaVField;

		private object vesselSimulation;

		public KerIntegration() {
			
		}

		public double getTotalDeltaV(List<Part> parts) {

			PluginLogger.logDebug("getTotalDeltaV - start");
			foreach (var a in AssemblyLoader.loadedAssemblies) {
				if (a.path.Contains("KerbalEngineer.dll")) {
					
					vesselSimulationType = a.assembly.GetType("KerbalEngineer.VesselSimulator.Simulation");
					if (vesselSimulationType == null) {
						PluginLogger.logDebug("Cannot find vesselSimulationType in " + a.path);
					}
					prepareSimulation = vesselSimulationType.GetMethod("PrepareSimulation");
					runSimulation = vesselSimulationType.GetMethod("RunSimulation");

					stageType = a.assembly.GetType("KerbalEngineer.VesselSimulator.Stage");
					if (stageType == null) {
						PluginLogger.logDebug("Cannot find Stage");
					}
					totalDeltaVField = stageType.GetField("inverseTotalDeltaV");

					PluginLogger.logDebug("getTotalDeltaV - CreateInstance");
					vesselSimulation = Activator.CreateInstance(vesselSimulationType);

					PluginLogger.logDebug("getTotalDeltaV - prepareSimulation");
					prepareSimulation.Invoke(vesselSimulation, new object[] {
						parts,//List<Part> parts,
						1.0,//double theGravity,
						0.0,//double theAtmosphere = 0, 
						0.0,//double theMach = 0,
						false,//bool dumpTree = false, 
						false,//bool vectoredThrust = false, 
						false//bool fullThrust = false
					});


					PluginLogger.logDebug("getTotalDeltaV - runSimulation");
					object[] simulationResult = (object[])runSimulation.Invoke(vesselSimulation, new object[0]);

					PluginLogger.logDebug("getTotalDeltaV - runSimulation end");
					if (simulationResult.Length > 0) {

					PluginLogger.logDebug("getTotalDeltaV - get totalDeltaVField");
						double totalDeltaV = (double)(totalDeltaVField.GetValue(simulationResult[0]));


						PluginLogger.logDebug("getTotalDeltaV - end");
						return totalDeltaV;
					} else {
						PluginLogger.logDebug("getTotalDeltaV - end0");
						return 0;
					}
				}
			}

			PluginLogger.logDebug("getTotalDeltaV - end-1");
			return -1;

		}
	}
}
