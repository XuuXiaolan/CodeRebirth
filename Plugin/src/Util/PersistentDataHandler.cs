using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine;

namespace CodeRebirth.src.Util;

public abstract class SaveableData(string fileName)
{
	[JsonIgnore]
	public string FileName { get; private set; } = fileName;

	public virtual void Save() {
		PersistentDataHandler.Save(FileName, this);
	}
}

public static class PersistentDataHandler
{
	const string SAVE_EXTENSION = ".coderebirth_data";

	private static string GetFullPath(string fileName)
	{
		string path = Path.Combine(Application.persistentDataPath, "CodeRebirth");
		Directory.CreateDirectory(path);
		return Path.Combine(path, fileName);
	}
    
	internal static T Load<T>(string fileName)
	{
		string fullPath = GetFullPath($"{fileName}{SAVE_EXTENSION}");

		if (!File.Exists(fullPath))
		{
			Plugin.ExtendedLogging("Data does not exist! Using default constructor!");
			T result = (T) typeof(T).GetConstructor([typeof(string)]).Invoke([fileName]);
			Save(fileName, result);
			return result;
		}
		else
		{
			Plugin.ExtendedLogging("Save exists! Attempting to load!");
			
			// maybe add more error handling here?
			return JsonConvert.DeserializeObject<T>(File.ReadAllText(fullPath), new JsonSerializerSettings {
				ContractResolver = new IncludePrivateSetterContractResolver()
			})!;
		}
	}

	internal static void Save<T>(string fileName, T data)
	{
		File.WriteAllText(GetFullPath($"{fileName}{SAVE_EXTENSION}"), JsonConvert.SerializeObject(data));
	}

	internal static bool TryDelete(string fileName)
	{
		string fullPath = GetFullPath($"{fileName}{SAVE_EXTENSION}");
		if (File.Exists(fullPath))
		{
			File.Delete(fullPath);
			return true;
		}

		return false;
	}
}

// utility class to deseralizize private setters in properties.
internal class IncludePrivateSetterContractResolver : DefaultContractResolver
{
	protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
	{
		JsonProperty property = base.CreateProperty(member, memberSerialization);

		// If the property is not writable and it's a property with a private setter, set it to writable
		if (!property.Writable && member is PropertyInfo propInfo && !property.Ignored)
		{
			property.Writable = propInfo.GetSetMethod(true) != null;
		}

		return property;
	}
}