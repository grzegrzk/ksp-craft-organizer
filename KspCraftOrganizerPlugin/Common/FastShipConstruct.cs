using KSP.UI.Screens;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KspNalCommon;

internal class FastShipConstruct {

	public static List<Part> partPool = new List<Part>();


	// 
	// Static Fields
	//
	public static int lastCompatibleMajor;

	public static int lastCompatibleMinor = 18;

	public static int lastCompatibleRev;

	//
	// Fields
	//
	public List<Part> parts;

	public Vector3 shipSize;

	public bool shipPartsUnlocked = true;

	public EditorFacility shipFacility;

	public string shipDescription = string.Empty;

	public string shipName;

	//
	// Properties
	//
	public int Count {
		get {
			return this.parts.Count;
		}
	}

	public List<Part> Parts {
		get {
			return this.parts;
		}
	}

	//
	// Indexer
	//
	public Part this[int index] {
		get {
			return this.parts[index];
		}
		set {
			this.parts[index] = value;
		}
	}

	public FastShipConstruct() {
		this.parts = new List<Part>();
		this.shipSize = Vector3.zero;
	}

	//
	// Methods
	//
	public void Add(Part p) {
		this.parts.Add(p);
	}

	private void AddToConstruct(Part rootPart) {
		if (!this.parts.Contains(rootPart)) {
			this.parts.Add(rootPart);
		}
		foreach (Part current in rootPart.children) {
			this.AddToConstruct(current);
		}
	}

	public bool AreAllPartsConnected() {
		foreach (Part current in this.parts) {
			if (current.parent == null && current.children.Count == 0) {
				return false;
			}
		}
		return true;
	}

	public void Clear() {
		this.parts.Clear();
	}

	public bool Contains(Part p) {
		return this.parts.Contains(p);
	}


	public IEnumerator<Part> GetEnumerator() {
		return this.parts.GetEnumerator();
	}

	public float GetShipCosts(out float dryCost, out float fuelCost) {
		float num = 0f;
		dryCost = 0f;
		fuelCost = 0f;
		foreach (Part current in this.parts) {
			AvailablePart partInfo = current.partInfo;
			float num2 = partInfo.cost + current.GetModuleCosts(partInfo.cost);
			float num3 = 0f;
			foreach (PartResource partResource in current.Resources) {
				PartResourceDefinition info = partResource.info;
				num2 -= info.unitCost * (float)partResource.maxAmount;
				num3 += info.unitCost * (float)partResource.amount;
			}
			dryCost += num2;
			fuelCost += num3;
		}
		num += dryCost + fuelCost;
		return num;
	}

	public float GetShipMass(out float dryMass, out float fuelMass) {
		float num = 0f;
		dryMass = 0f;
		fuelMass = 0f;
		foreach (Part current in this.parts) {
			AvailablePart partInfo = current.partInfo;
			float num2 = partInfo.partPrefab.mass + current.GetModuleMass(partInfo.partPrefab.mass);
			float num3 = 0f;
			foreach (PartResource partResource in current.Resources) {
				PartResourceDefinition info = partResource.info;
				num3 += info.density * (float)partResource.amount;
			}
			dryMass += num2;
			fuelMass += num3;
		}
		num += dryMass + fuelMass;
		return num;
	}

	public float GetTotalMass() {
		float num;
		float num2;
		return this.GetShipMass(out num, out num2);
	}

	public bool LoadShip(ConfigNode root) {
		this.parts = new List<Part>();
		List<List<uint>> allLinksList = new List<List<uint>>();
		List<List<uint>> allSymList = new List<List<uint>>();
		this.shipFacility = EditorFacility.None;
		foreach (ConfigNode.Value value in root.values) {
			string name = value.name;
			switch (name) {
				case "ship":
					this.shipName = value.value;
					break;
				case "type":
					this.shipFacility = (EditorFacility)((int)Enum.Parse(typeof(EditorFacility), value.value));
					break;
				case "version": {
						VersionCompareResult versionCompareResult = KSPUtil.CheckVersion(value.value, ShipConstruct.lastCompatibleMajor, ShipConstruct.lastCompatibleMinor, ShipConstruct.lastCompatibleRev);
						if (versionCompareResult != VersionCompareResult.COMPATIBLE) {
							PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), "Loading Failed", "Cannot load the craft. File format is incompatible\nwith this version of KSP.", "OK", true, HighLogic.UISkin, true, string.Empty);
							bool result = false;
							return result;
						}
						break;
					}
				case "description":
					this.shipDescription = value.value.Replace('¨', '\n');
					break;
				case "size":
					this.shipSize = KSPUtil.ParseVector3(value.value);
					break;
			}
		}
		List<string> notAvailablePartsList = new List<string>();
		foreach (ConfigNode configNode in root.nodes) {
			string[] array = configNode.GetValue("part").Split(new char[] {
				'_'
			});
			AvailablePart partInfoByName = PartLoader.getPartInfoByName(array[0]);
			if ((partInfoByName == null || !partInfoByName.partPrefab) && !notAvailablePartsList.Contains(array[0])) {
				notAvailablePartsList.Add(array[0]);
			}
		}
		if (notAvailablePartsList.Count > 0) {
			string text = string.Empty;
			foreach (string current in notAvailablePartsList) {
				text = text + current + "\n";
			}
			PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), "Craft Loading Error", "Craft " + this.shipName + " was not loaded because\nit had the following parts missing:\n" + text, "Ok", true, HighLogic.UISkin, true, string.Empty);
			return false;
		}
		foreach (ConfigNode partNode in root.nodes) {
			Part part = null;
			List<uint> partLinksList = new List<uint>();
			List<uint> partSymList = new List<uint>();

			foreach (ConfigNode.Value valueInPartNode in partNode.values) {
				string name = valueInPartNode.name;
				PluginLogger.logDebug("Reading " + valueInPartNode.name + ":" + valueInPartNode.value);
				switch (name) {
					case "part": {
							string[] partNameAndId = valueInPartNode.value.Split(new char[] {
						'_'
					});
							AvailablePart partInfoByName = PartLoader.getPartInfoByName(partNameAndId[0]);
							if (partInfoByName == null || !partInfoByName.partPrefab) {
								PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), "Loading Failed", "Part " + partNameAndId[0] + " is missing.", "OK", true, HighLogic.UISkin, true, string.Empty);
								bool result = false;
								return result;
							}
							part = fastPartInstantiate(partInfoByName.partPrefab);
							//part = UnityEngine.Object.Instantiate<Part>(partInfoByName2.partPrefab);


							part.gameObject.SetActive(true);
							part.name = partNameAndId[0];
							part.craftID = uint.Parse(partNameAndId[1]);
							part.partInfo = partInfoByName;
							part.symMethod = ((this.shipFacility != EditorFacility.SPH) ? SymmetryMethod.Radial : SymmetryMethod.Mirror);
							if (!ResearchAndDevelopment.PartTechAvailable(partInfoByName)) {
								this.shipPartsUnlocked = false;
							}
							break;
						}
					case "pos":
						part.transform.position = KSPUtil.ParseVector3(valueInPartNode.value);
						break;
					case "attPos":
						part.attPos = KSPUtil.ParseVector3(valueInPartNode.value);
						break;
					case "attPos0":
						part.attPos0 = KSPUtil.ParseVector3(valueInPartNode.value);
						break;
					case "rot":
						part.transform.rotation = KSPUtil.ParseQuaternion(valueInPartNode.value);
						break;
					case "attRot":
						part.attRotation = KSPUtil.ParseQuaternion(valueInPartNode.value);
						break;
					case "attRot0":
						part.attRotation0 = KSPUtil.ParseQuaternion(valueInPartNode.value);
						break;
					case "mir":
						part.SetMirror(KSPUtil.ParseVector3(valueInPartNode.value));
						break;
					case "symMethod":
						part.symMethod = (SymmetryMethod)((int)Enum.Parse(typeof(SymmetryMethod), valueInPartNode.value));
						break;
					case "istg":
						part.inverseStage = int.Parse(valueInPartNode.value);
						break;
					case "dstg":
						part.defaultInverseStage = int.Parse(valueInPartNode.value);
						break;
					case "sqor":
						part.manualStageOffset = int.Parse(valueInPartNode.value);
						break;
					case "sepI":
						part.separationIndex = int.Parse(valueInPartNode.value);
						break;
					case "sidx":
						part.inStageIndex = int.Parse(valueInPartNode.value);
						break;
					case "link":
						partLinksList.Add(uint.Parse(valueInPartNode.value.Split(new char[] {
						'_'
					})[1]));
						break;
					case "sym":
						partSymList.Add(uint.Parse(valueInPartNode.value.Split(new char[] {
						'_'
					})[1]));
						break;
					case "pSym":
						partSymList.Add(uint.Parse(valueInPartNode.value));
						break;
					case "attm":
						part.attachMode = (AttachModes)int.Parse(valueInPartNode.value);
						break;
					case "cData":
						part.customPartData = valueInPartNode.value;
						break;
					case "srfN":
						part.srfAttachNode.attachedPartId = uint.Parse(valueInPartNode.value.Split(new char[] {
						','
					})[1].Split(new char[] {
						'_'
					})[1].Trim());
						break;
					case "attN": {
							string[] array3 = valueInPartNode.value.Split(new char[] {
						','
					});
							part.findAttachNode(array3[0].Trim()).attachedPartId = uint.Parse(array3[1].Split(new char[] {
						'_'
					})[1].Trim());
							break;
						}
				}
			}
			int outFromLoadModule = 0;
			foreach (ConfigNode nodeInPartNode in partNode.nodes) {
				string name = nodeInPartNode.name;
				PluginLogger.logDebug("Reading " + nodeInPartNode.name + " in " + part.name);
				switch (name) {
					case "MODULE":
						part.LoadModule(nodeInPartNode, ref outFromLoadModule);
						break;
					case "EVENTS":
						part.Events.OnLoad(nodeInPartNode);
						break;
					case "ACTIONS":
						part.Actions.OnLoad(nodeInPartNode);
						break;
					case "EFFECTS":
						part.LoadEffects(nodeInPartNode);
						break;
					case "RESOURCE":
						part.SetResource(nodeInPartNode);
						break;
					case "PARTDATA":
						part.OnLoad(nodeInPartNode);
						break;
				}
			}
			this.parts.Add(part);
			allLinksList.Add(partLinksList);
			allSymList.Add(partSymList);
		}
		for (int i = 0; i < this.parts.Count; i++) {
			foreach (uint linkedPartUid in allLinksList[i]) {
				Part part = this.parts.Find((Part p) => p.craftID == linkedPartUid);
				part.setParent(this.parts[i]);
				part.transform.parent = this.parts[i].transform;
			}
			foreach (uint symUid in allSymList[i]) {
				Part item = this.parts.Find((Part p) => p.craftID == symUid);
				this.parts[i].symmetryCounterparts.Add(item);
			}
			if (this.parts[i].srfAttachNode != null) {
				this.parts[i].srfAttachNode.owner = this.parts[i];
				this.parts[i].srfAttachNode.FindAttachedPart(this.parts);
			}
			foreach (AttachNode current2 in this.parts[i].attachNodes) {
				current2.owner = this.parts[i];
				current2.FindAttachedPart(this.parts);
			}
		}
		foreach (Part part in this.parts) {
			part.partTransform = part.transform;
			part.orgPos = part.transform.root.InverseTransformPoint(part.transform.position);
			part.orgRot = Quaternion.Inverse(part.transform.root.rotation) * part.transform.rotation;
			part.packed = true;
			part.InitializeModules();
		}
		Debug.Log(this.shipName + " loaded! -- Fast");
		return true;
	}

	Part fastPartInstantiate(Part prefabPart) {
		Part fromPool = getPartFromPool();
		if (fromPool == null) {
			return UnityEngine.Object.Instantiate<Part>(prefabPart);
		} else {
			//properties used by KER:
			//
			//transform
			//partInfo	will be assigned later
			//attachMode
			//fuelCrossFeed
			//attachNodes
			//NoCrossFeedNodeKey
			//Modules
			//inverseStage
			//physicalSignificance
			//mass
			//prefabMass
			//Resources
			//vessel
			//vesselType
			//initialVesselName
			//name
			//parent	will be assinged later
			//ActivatesEvenIfDisconnected
			//attachRules
			//fuelLookupTargets

			fromPool.Modules.Clear();
			fromPool.Events.Clear();
			fromPool.Actions.Clear();
			fromPool.Resources.list.Clear();

			fromPool.transform.position = prefabPart.transform.position;//struct
			fromPool.transform.rotation = prefabPart.transform.rotation;//struct
			fromPool.transform.parent = null;//will be assigned later

			fromPool.attachMode = prefabPart.attachMode;//enum
			fromPool.fuelCrossFeed = prefabPart.fuelCrossFeed;//bool

			fromPool.attachNodes.Clear();
			foreach (AttachNode prefabAn in prefabPart.attachNodes) {
				AttachNode newAn = new AttachNode();

				newAn.id = prefabAn.id;
				newAn.nodeTransform = prefabAn.nodeTransform;
				newAn.size = prefabAn.size;
				newAn.attachMethod = prefabAn.attachMethod;
				newAn.icon = null; // left unasigned to avoid copying GameObject. It can be tested if it can be shallow reference assign but for now it is not going to be used anyway
				newAn.attachedPart = prefabAn.attachedPart;
				newAn.attachedPartId = prefabAn.attachedPartId;
				newAn.breakingForce = prefabAn.breakingForce;
				newAn.breakingTorque = prefabAn.breakingTorque;
				newAn.contactArea = prefabAn.contactArea;
				newAn.nodeType = prefabAn.nodeType;
				newAn.offset = prefabAn.offset;
				newAn.orientation = prefabAn.orientation;
				newAn.originalOrientation = prefabAn.originalOrientation;
				newAn.originalPosition = prefabAn.originalPosition;
				newAn.originalSecondaryAxis = prefabAn.originalSecondaryAxis;
				newAn.overrideDragArea = prefabAn.overrideDragArea;
				newAn.owner = prefabAn.owner;
				newAn.position = prefabAn.position;
				newAn.radius = prefabAn.radius;
				newAn.requestGate = prefabAn.requestGate;
				newAn.ResourceXFeed = prefabAn.ResourceXFeed;
				newAn.secondaryAxis = prefabAn.secondaryAxis;

				fromPool.attachNodes.Add(newAn);
			}

			fromPool.NoCrossFeedNodeKey = prefabPart.NoCrossFeedNodeKey;
			fromPool.Modules.Clear();//will be filled later
			foreach (PartModule m in prefabPart.Modules) {
				fromPool.Modules.Add(UnityEngine.Object.Instantiate<PartModule>(m));
			}

			fromPool.inverseStage = prefabPart.inverseStage;
			fromPool.physicalSignificance = prefabPart.physicalSignificance;
			fromPool.mass = prefabPart.mass;
			fromPool.prefabMass = prefabPart.prefabMass;

			fromPool.Resources.list.Clear();//will be filled later

			fromPool.vessel = prefabPart.vessel;
			fromPool.vesselType = prefabPart.vesselType;
			fromPool.initialVesselName = prefabPart.initialVesselName;
			fromPool.name = prefabPart.name;
			fromPool.ActivatesEvenIfDisconnected = prefabPart.ActivatesEvenIfDisconnected;

			fromPool.attachRules = new AttachRules();
			fromPool.attachRules.allowCollision = prefabPart.attachRules.allowCollision;
			fromPool.attachRules.allowDock = prefabPart.attachRules.allowDock;
			fromPool.attachRules.allowRoot = prefabPart.attachRules.allowRoot;
			fromPool.attachRules.allowRotate = prefabPart.attachRules.allowRotate;
			fromPool.attachRules.allowSrfAttach = prefabPart.attachRules.allowSrfAttach;
			fromPool.attachRules.allowStack = prefabPart.attachRules.allowStack;
			fromPool.attachRules.srfAttach = prefabPart.attachRules.srfAttach;
			fromPool.attachRules.stack = prefabPart.attachRules.stack;

			fromPool.fuelLookupTargets.Clear();
			foreach (Part flt in prefabPart.fuelLookupTargets) {
				fromPool.fuelLookupTargets.Add(flt);
			}

			return fromPool;
		}
	}

	Part getPartFromPool() {
		if (partPool.Count == 0) {
			return null;
		} else {
			Part toRet = partPool[partPool.Count - 1];
			partPool.RemoveAt(partPool.Count - 1);
			return toRet;
		}
	}

	public void Release() {
		foreach (Part p in parts) {
			partPool.Add(p);
		}
		this.parts.Clear();
	}

	private Vector3 CalculateCraftSize() {
		if (this.parts.Count == 0) {
			return Vector3.zero;
		}
		Bounds bounds = default(Bounds);
		Vector3 orgPos = this.parts[0].orgPos;
		bounds.center = orgPos;
		List<Bounds> list = new List<Bounds>();
		foreach (Part current in this.parts) {
			Bounds[] partRendererBounds = PartGeometryUtil.GetPartRendererBounds(current);
			Bounds[] array = partRendererBounds;
			for (int i = 0; i < array.Length; i++) {
				Bounds bounds2 = array[i];
				Bounds bounds3 = bounds2;
				bounds3.size *= current.boundsMultiplier;
				Vector3 size = bounds3.size;
				bounds3.Expand(current.GetModuleSize(size));
				list.Add(bounds2);
			}
		}
		if (list.Count == 0) {
			return Vector3.zero;
		}
		return PartGeometryUtil.MergeBounds(list.ToArray(), this.parts[0].transform.root).size;
	}
}
