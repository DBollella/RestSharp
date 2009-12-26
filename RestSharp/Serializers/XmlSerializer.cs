﻿#region License
//   Copyright 2009 John Sheehan
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License. 
#endregion

using System;
using System.Collections;
using System.Linq;
using System.Xml.Linq;
using RestSharp.Extensions;

namespace RestSharp.Serializers
{
	public class XmlSerializer : ISerializer
	{
		public XmlSerializer() {
		}

		public XmlSerializer(string @namespace) {
			Namespace = @namespace;
		}

		public XDocument Serialize(object obj) {
			var doc = new XDocument();

			var t = obj.GetType();
			var root = new XElement(t.Name.AsNamespaced(Namespace));

			Map(root, obj);

			if (RootElement.HasValue()) {
				var wrapper = new XElement(RootElement.AsNamespaced(Namespace), root);
				doc.Add(wrapper);
			}
			else {
				doc.Add(root);
			}

			return doc;
		}

		private void Map(XElement root, object obj) {
			var props = obj.GetType().GetProperties().Where(p => p.CanRead && p.CanWrite);

			foreach (var prop in props) {
				var name = prop.Name;
				var rawValue = prop.GetValue(obj, null);

				if (rawValue == null) {
					continue;
				}

				var value = GetSerializedValue(rawValue);
				var propType = prop.PropertyType;

				var useAttribute = false;
				var settings = prop.GetAttribute<SerializeAsAttribute>();
				if (settings != null) {
					name = settings.Name.HasValue() ? settings.Name : name;
					useAttribute = settings.Attribute;
				}

				var transform = prop.GetAttribute<SerializeTransformAttribute>();
				if (transform != null) {
					name = transform.Shazam(name);
				}

				var nsName = name.AsNamespaced(Namespace);
				var element = new XElement(nsName);

				if (propType.IsPrimitive || propType.IsValueType || propType == typeof(string)) {
					if (useAttribute) {
						root.Add(new XAttribute(name, value));
						continue;
					}
					else {
						element.Value = value;
					}
				}
				else if (rawValue is IList) {
					var itemTypeName = "";
					foreach (var item in (IList)rawValue) {
						if (itemTypeName == "") {
							itemTypeName = item.GetType().Name;
						}
						var instance = new XElement(itemTypeName);
						Map(instance, item);
						element.Add(instance);
					}
				}
				else {
					Map(element, rawValue);
				}

				root.Add(element);
			}
		}

		private string GetSerializedValue(object obj) {
			var output = obj;

			if (obj is DateTime) {
				// check for DateFormat when adding date props
				if (DateFormat != DateFormat.None) {
					output = ((DateTime)obj).ToString(DateFormat.GetFormatString());
				}
			}
			// else if... if needed for other types

			return output.ToString();
		}

		public string RootElement { get; set; }
		public string Namespace { get; set; }
		public DateFormat DateFormat { get; set; } // Currently unused
	}
}