using System;
using System.Collections;
using System.IO;
using System.Xml;
using System.Text;
using System.Globalization;
using UnityEngine;
 
public class PListManager {
 
	public PListManager() { }
 
	private const string SUPPORTED_VERSION = "1.0";
 
	public static bool ParsePListFile(string xmlFile, ref Hashtable plist) {
		if (!File.Exists(xmlFile)) {
			Debug.LogError("File doesn't exist: " + xmlFile);
			return false;
		}
 
		StreamReader sr = new StreamReader(xmlFile);
		string txt = sr.ReadToEnd();
		sr.Close();
 
		XmlDocument xml = new XmlDocument();
				xml.XmlResolver = null; //Disable schema/DTD validation, it's not implemented for Unity.
		xml.LoadXml(txt);
 
		XmlNode plistNode = xml.LastChild;
		if (!plistNode.Name.Equals("plist")) {
			Debug.LogError("plist file missing <plist> nodes." + xmlFile);
			return false;
		}
 
		string plistVers = plistNode.Attributes["version"].Value;
		if (plistVers == null || !plistVers.Equals(SUPPORTED_VERSION)) {
			Debug.LogError("This is an unsupported plist version: " + plistVers + ". Required version:a " + SUPPORTED_VERSION);
			return false;
		}
 
		XmlNode dictNode = plistNode.FirstChild;
		if (!dictNode.Name.Equals("dict")) {
			Debug.LogError("Missing root dict from plist file: " + xmlFile);
			return false;
		}
 
		return LoadDictFromPlistNode(dictNode, ref plist);
	}
 
 
	#region LOAD_PLIST_PRIVATE_METHODS
	private static bool LoadDictFromPlistNode(XmlNode node, ref Hashtable dict) {
		if (node == null) {
			Debug.LogError("Attempted to load a null plist dict node.");
			return false;
		}
		if (!node.Name.Equals("dict")) {
			Debug.LogError("Attempted to load an dict from a non-array node type: " + node + ", " + node.Name);
			return false;
		}
		if (dict == null) {
			dict = new Hashtable();
		}
 
		int cnodeCount = node.ChildNodes.Count;
		for (int i = 0; i+1 < cnodeCount; i+=2) {
			// Select the key and value child nodes
			XmlNode keynode = node.ChildNodes.Item(i);
			XmlNode valuenode = node.ChildNodes.Item(i+1);
 
			// If this node isn't a 'key'
			if (keynode.Name.Equals("key")) {
				// Establish our variables to hold the key and value.
				string key = keynode.InnerText;
				ValueObject value = new ValueObject();
 
				// Load the value node.
				// If the value node loaded successfully, add the key/value pair to the dict hashtable.
				if (LoadValueFromPlistNode(valuenode, ref value)) {
					// This could be one of several different possible data types, including another dict.
					// AddKeyValueToDict() handles this by replacing existing key values that overlap, and doing so recursively for dict values.
					// If this not successful, post a message stating so and return false.
					if (!AddKeyValueToDict(ref dict, key, value)) {
						Debug.LogError("Failed to add key value to dict when loading plist from dict");
						return false;
					}
				} else {
					Debug.LogError("Did not load plist value correctly for key in node: " + key + ", " + node);
					return false;
				}
			} else {
				Debug.LogError("The plist being loaded may be corrupt.");
				return false;
			}
 
		} //end for
 
		return true;
	}
 
	private static bool LoadValueFromPlistNode(XmlNode node, ref ValueObject value) {
		if (node == null) {
			Debug.LogError("Attempted to load a null plist value node.");
			return false;
		}
		if (node.Name.Equals("string")) { value.val = node.InnerText; }
		else if (node.Name.Equals("integer")) { value.val = int.Parse(node.InnerText); }
		else if (node.Name.Equals("real")) { value.val = float.Parse(node.InnerText); }
		else if (node.Name.Equals("date")) { value.val = DateTime.Parse(node.InnerText, null, DateTimeStyles.None); } // Date objects are in ISO 8601 format
		else if (node.Name.Equals("data")) { value.val = node.InnerText; } // Data objects are just loaded as a string
		else if (node.Name.Equals("true")) { value.val = true; } // Boollean values are empty objects, simply identified with a name being "true" or "false"
		else if (node.Name.Equals("false")) { value.val = false; }
		// The value can be an array or dict type.  In this case, we need to recursively call the appropriate loader functions for dict and arrays.
		// These functions will in turn return a boolean value for their success, so we can just return that.
		// The val value also has to be instantiated, since it's being passed by reference.
		else if (node.Name.Equals("dict")) {
			value.val = new Hashtable();
			Hashtable htRef = (Hashtable)value.val;
			return LoadDictFromPlistNode(node, ref htRef);
		}
		else if (node.Name.Equals("array")) {
			value.val = new ArrayList();
			ArrayList alRef = (ArrayList)value.val;
			return LoadArrayFromPlistNode(node, ref alRef);
		} else {
			Debug.LogError("Attempted to load a value from a non value type node: " + node + ", " + node.Name);
			return false;
		}
 
		return true;
	}
 
	private static bool LoadArrayFromPlistNode(XmlNode node, ref ArrayList array ) {
		// If we were passed a null node object, then post an error stating so and return false
		if (node == null) {
			Debug.LogError("Attempted to load a null plist array node.");
			return false;
		}
		// If we were passed a non array node, then post an error stating so and return false
		if (!node.Name.Equals("array")) {
			Debug.LogError("Attempted to load an array from a non-array node type: " + node + ", " + node.Name);
			return false;
		}
 
		// We can be passed an empty array object.  If so, initialize it
		if (array == null) { array = new ArrayList(); }
 
		// Itterate through the child nodes for this array object
		int nodeCount = node.ChildNodes.Count;
		for (int i = 0; i < nodeCount; i++) {
			// Establish variables to hold the child node of the array, and it's value
			XmlNode cnode = node.ChildNodes.Item(i);
			ValueObject element = new ValueObject();
			// Attempt to load the value from the current array node.
			// If successful, add it as an element of the array.  If not, post and error stating so and return false.
			if (LoadValueFromPlistNode(cnode, ref element)) {
				array.Add(element.val);
			} else {
				return false;
			}
		}
 
		// If we made it through the array without errors, return true
		return true;
	}
 
	private static bool AddKeyValueToDict(ref Hashtable dict, string key, ValueObject value) {
		// Make sure that we have values that we can work with.
		if (dict == null || key == null || key.Length < 1 || value == null) {
			Debug.LogError("Attempted to AddKeyValueToDict() with null objects.");
			return false;
		}
		// If the hashtable doesn't already contain the key, they we can just go ahead and add it.
		if (!dict.ContainsKey(key)) {
			dict.Add(key, value.val);
			return true;
		}
		// At this point, the dict contains already contains the key we're trying to add.
		// If the value for this key is of a different type between the dict and the new value, then we have a type mismatch.
		// Post an error stating so, but go ahead and overwrite the existing key value.
		if (value.val.GetType() != dict[key].GetType()) {
			Debug.LogWarning("Value type mismatch for overlapping key (will replace old value with new one): " + value.val + ", " + dict[key] + ", " + key);
			dict[key] = value.val;
		}
		// If the value for this key is a hashtable, then we need to recursively add the key values of each hashtable.
		else if (value.val.GetType() == typeof(Hashtable)) {
			// Itterate through the elements of the value's hashtable.
			Hashtable htTmp = (Hashtable)value.val;
			foreach (object element in htTmp) {
				// Recursively attempt to add/repalce the elements of the value hashtable to the dict's value hashtable.
				// If this fails, post a message stating so and return false.
				Hashtable htRef = (Hashtable)dict[key];
				if (!AddKeyValueToDict(ref htRef, (string)element, new ValueObject(htTmp[element]))) {
					Debug.LogError("Failed to add key value to dict: " + element + ", " + htTmp[element] + ", " + dict[key]);
					return false;
				}
			}
		}
		// If the value is an array, then there's really no way we can tell which elements to overwrite, because this is done based on the congruent keys.
		// Thus, we'll just add the elements of the array to the existing array.
		else if (value.val.GetType() == typeof(ArrayList)) {
			ArrayList alTmp = (ArrayList)value.val;
			ArrayList alAddTmp = (ArrayList)dict[key];
			foreach (object element in alTmp) {
				alAddTmp.Add(element);
			}
		}
		// If the key value is not an array or a hashtable, then it's a primitive value that we can easily write over.
		else {
			dict[key] = value.val;
		}
 
		return true;
	}
	#endregion
} //end PListManager class
 
class ValueObject {
	public object val;
	public ValueObject() {}
	public ValueObject(object aVal) { val = aVal; }
}
