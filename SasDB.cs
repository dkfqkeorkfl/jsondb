using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace Sas
{
	public class JsonDB
	{
		public readonly char[] delim = new char[]{ '\\' };

		public enum PROPERTY
		{
			TYPE,
		}

		public enum Mode
		{
			R = 0x01,
			W = 0x02,
			WR = R | W
		}

		enum RESERVE
		{
			NULL = 0,
			DIR = 'd',
			OBJ = 'o',
		}


		readonly JObject mCachedDir = new JObject ();

		public Mode mode { get; set; }

		public string package { get; private set; }
		public JsonDB(string package, Mode mode = Mode.R)
		{
			Init (package, mode);
		}

		void Init (string package, Mode mode = Mode.R)
		{
			this.package = package;
			SetMode (mode);
//			PlayerPrefs.DeleteAll ();
		}

		public void SetMode (Mode mode = Mode.R)
		{
			if (this.mode != mode)
				this.mode = mode;
		}

		public void Clear ()
		{
			Del (package);
		}

		public JToken Root ()
		{
			return GetImpl (package);
		}

		public JToken Get (string path)
		{
			return GetImpl (MakePath (package, path));
		}

		public JToken Get (string path, PROPERTY prop)
		{
			var str = PlayerPrefs.GetString (GetPropertyVal (prop) + MakePath (package, path));
			if (string.IsNullOrEmpty (str))
				return JValue.CreateNull ();

			return JToken.Parse (str);
		}

		public void Put (string key, JToken json)
		{
			var path = MakePath (package, key);
			MkDir (package, path);
			PutImpl (path, json);
			Flush ();
		}

		void PutImpl (string cur, JToken obj)
		{
			switch (obj.Type) {
			case JTokenType.Object:
				{
					var parent = (JObject)obj;
					foreach (var key in parent.Properties()) {
						var path = MakePath (cur, key.Name);
						var child = parent [key.Name];
						MkDir (cur, path);
						PutImpl (path, child);
					}
				}
				break;
			case JTokenType.Array:
				{
					var array = (JArray)obj;
					for (var i = 0; i < array.Count; ++i) {
						var child = obj [i];
						var path = MakeArray (cur, i);
						MkDir (cur, path);
						PutImpl (path, child);
					}

				}
				break;
			default:
				PutObj (cur, obj);
				break;
			}
		}


		public void Del (string path)
		{
			var root = MakePath (package, path);
			DelObj (root);

			var cur = root;
			var sz = cur.LastIndexOfAny (delim);
			for (; sz != cur.Length; sz = cur.LastIndexOfAny (delim)) {

				var parents = cur.Substring (0, sz);
				var str = PlayerPrefs.GetString (parents);
				if (string.IsNullOrEmpty (str))
					break;

				var json = JArray.Parse (str);
				json.Remove (json.Single (data => (string)data == cur));
				if (json.Count != 0) {
					PutDir (parents, json.ToString ());
					break;
				}

				DelObj (parents);
				cur = parents;
			}
		}


		JToken GetImpl (string path)
		{
			var str = PlayerPrefs.GetString (path);
			if (string.IsNullOrEmpty (str))
				return JValue.CreateNull ();

			var json = JToken.Parse (str);
			if ((json is JArray) == false)
				return json;


			var rgx = new System.Text.RegularExpressions.Regex ("\\[[0-9]\\]");
			var items = new List<JProperty> ();
			JToken ret = new JArray ();
			foreach (var child in (JArray)json) {
				var sub = (string)child;
				var f = sub.LastIndexOf ('\\') + 1;
				var name = sub.Substring (f, sub.Length - f);

				if (ret is JArray) {
					var m = rgx.Match (name);
					if (m.Length != name.Length)
						ret = new JObject ();
				}

				items.Add (new JProperty (name, GetImpl (sub)));
			}

			foreach (var child in items) {
				if (ret is JArray)
					((JArray)ret).Add (child.Value);
				else
					((JObject)ret).Add (child);
			}
			return ret;
		}

		void PutObj (string path, JToken obj)
		{
			if (GetKind (path) == RESERVE.DIR)
				Del (path);
			var v = (JValue)obj;
			var str = v.Type == JTokenType.String || v.Type == JTokenType.Date ? string.Format ("\"{0}\"", v.ToString ()) : v.ToString ();
			PlayerPrefs.SetString (path, str);
			PlayerPrefs.SetString (GetPropertyVal (PROPERTY.TYPE) + path, new JValue (RESERVE.OBJ.GetHashCode ()).ToString ());
		}

		void PutDir (string path, string child)
		{
			PlayerPrefs.SetString (path, child);
			PlayerPrefs.SetString (GetPropertyVal (PROPERTY.TYPE) + path, new JValue (RESERVE.DIR.GetHashCode ()).ToString ());
		}

		void DelObj (string path)
		{
			PlayerPrefs.DeleteKey (path);
			PlayerPrefs.DeleteKey (GetPropertyVal (PROPERTY.TYPE) + path);
		}

		RESERVE GetKind (string path)
		{
			if (mCachedDir.GetValue (path) != null)
				return RESERVE.DIR;

			var str = PlayerPrefs.GetString (GetPropertyVal (PROPERTY.TYPE) + path);
			if (string.IsNullOrEmpty (str))
				return RESERVE.NULL;

			var json = JValue.Parse (str);
			var val = (int)json;

			var ret = (RESERVE)System.Enum.ToObject (typeof(RESERVE), val);
			return ret;

		}

		void Flush ()
		{
			foreach (var key in mCachedDir.Properties()) {
				var child = key.Value;
				PutDir (key.Name, child.ToString ());
			}
			mCachedDir.RemoveAll ();
			PlayerPrefs.Save ();
		}

		void MkDir (string parents, string child)
		{
			var json = mCachedDir [parents];
			if (json == null) {
				try {
					var str = PlayerPrefs.GetString (parents, "");
					json = JArray.Parse (str);
				} catch (System.Exception e) {
					json = new JArray ();
				}

				mCachedDir [parents] = json;
			}
			var array = (JArray)json;
			var i = array.LowerBound (0, array.Count, child, (l, r) => {
				var str = (string)l;
				return str.CompareTo (r) < 0;
			});

			if (i < array.Count) {
				var v = array [i];
				var str = (string)v;
				if (str.CompareTo (child) == 0)
					return;
			}
			array.Insert (i, child);
		}

		static string GetPropertyVal (PROPERTY prop)
		{
			return prop.ToString () + ":";
		}

		public string MakePath (string parents, string child)
		{
			return string.Format ("{0}{1}{2}", parents, new string (delim), child);
		}

		public string MakeArray (string path, int i)
		{
			return string.Format ("{0}{1}[{2}]", path, new string (delim), i);
		}
	}
}
