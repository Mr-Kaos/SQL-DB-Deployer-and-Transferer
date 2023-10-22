using Godot;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public partial class ScriptCategory : GodotObject
{
	private readonly string category;
	private List<string> scriptNames;
	private List<string> scriptPaths;

	public ScriptCategory(string category)
	{
		this.category = category;
		scriptNames = new List<string>();
		scriptPaths = new List<string>();
	}

	public List<string> ScriptNames
	{
		get => scriptNames;
	}

	public List<string> ScriptPaths
	{
		get => scriptPaths;
	}

	public string Category
	{
		get => category;
	}

	public string ScriptName(int index)
	{
		string value = null;
		if (index < scriptNames.Count)
		{
			value = scriptNames[index];
		}
		return value;
	}

	public string ScriptPath(int index)
	{
		string value = null;
		if (index < scriptPaths.Count)
		{
			value = scriptPaths[index];
		}
		return value;
	}

	public int ScriptCount
	{
		get => scriptPaths.Count;
	}
	
	/**
	 *	Adds a new script to the scripts arrays.
	 *	
	 */
	public void AddScript(string path)
	{
		Match name = Regex.Match(path, @"[^\\\/]+$");
		if (!scriptPaths.Contains(path))
		{
			scriptNames.Add(name.Value);
			scriptPaths.Add(path);
		}
	}

	public void RemoveScript(string name)
	{
		for (int i = 0; i < scriptNames.Count; i++)
		{
			if (scriptNames[i] == name)
			{
				scriptNames.Remove(name);
				scriptPaths.RemoveAt(i);
			}
		}
	}

	public void RemoveScript(int index)
	{
		scriptNames.RemoveAt(index);
		scriptPaths.RemoveAt(index);
	}

	public void MoveScript(int currentIndex, int newIndex)
	{
		string temp;
		if (currentIndex < scriptPaths.Count && newIndex >= 0 && newIndex < scriptPaths.Count && currentIndex != newIndex)
		{
			temp = scriptNames[currentIndex];
			scriptNames[currentIndex] = scriptNames[newIndex];
			scriptNames[newIndex] = temp;

			temp = scriptPaths[currentIndex];
			scriptPaths[currentIndex] = scriptPaths[newIndex];
			scriptPaths[newIndex] = temp;
		}
		else
		{
			GD.PrintErr("Move index out of bounds or invalid");
		}
	}
}
