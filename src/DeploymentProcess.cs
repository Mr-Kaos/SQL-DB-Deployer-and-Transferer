using Godot;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;
using System.Linq;

public partial class DeploymentProcess : GodotObject
{
	private readonly Database db;
	private string destinationDB;
	private string collationName;
	private readonly ScriptCategory[] scripts;
	private readonly ScriptCategory[] transferScripts;
	private bool deploymentComplete = false;

	public DeploymentProcess(Database dbObj)
	{
		db = dbObj;
		scripts = new ScriptCategory[7];
	}

	public Database DBConnection
	{
		get => db;
	}

	public ScriptCategory[] Scripts
	{
		get => scripts;
	}

	public string DestinationDB
	{
		get => destinationDB;
		set => destinationDB = value;
	}

	public string CollationName
	{
		get => collationName == "" ? "Latin1_General_100_CI_AI_SC_UTF8" : collationName;
		set => collationName = value;
	}

	public bool DeploymentComplete
	{
		get => deploymentComplete;
	}

	public ScriptCategory GetScriptCategory(int index)
	{
		return scripts[index];
	}

	public int GetScriptCount()
	{
		int count = 0;
		foreach (ScriptCategory c in scripts)
		{
			if (c != null)
			{
				count += c.ScriptCount;
			}
		}

		return count;
	}

	public void AddScripts(int index, string categoryName, string[] files)
	{
		if (scripts[index] is null)
		{
			scripts[index] = new ScriptCategory(categoryName);
		}

		foreach (string file in files)
		{
			scripts[index].AddScript(file);
		}
	}

	public void AddScripts(int index, string categoryName, string file)
	{
		if (scripts[index] is null)
		{
			scripts[index] = new ScriptCategory(categoryName);
		}

		scripts[index].AddScript(file);
	}

	public void MoveScripts(int sourceList, int targetList, int[] fileIndexes)
	{
		// Add files to target list
		foreach (int index in fileIndexes)
		{
			scripts[targetList].AddScript(scripts[sourceList].ScriptPath(index));
		}

		// remove files from source list
		fileIndexes = fileIndexes.OrderByDescending(c => c).ToArray();
		foreach (int index in fileIndexes)
		{
			scripts[sourceList].RemoveScript(index);
		}
	}

	public void RemoveScript(int index, string file)
	{
		scripts[index]?.RemoveScript(file);
	}

	public void ClearScripts()
	{
		for (int i = 0; i < scripts.Length; i++)
		{
			scripts[i] = null;
		}
	}

	public void CommenceDeployment()
	{
		// Update connection to use database name
		db.NewConnection(DestinationDB);

		// Loop through all deployment scripts in order and execute them
		foreach (ScriptCategory list in scripts)
		{
			if (list != null)
			{
				GD.Print(list.Category);
				foreach (string script in list.ScriptPaths)
				{
					ProcessSQLFile(db.Connection, script);
				}
			}
		}

		deploymentComplete = true;
	}

	/**
	*	Checks if the specified database exists on the server. If it does, it deploys scripts to there.
	Else, it creates a new database with the specified name on the server.
	*/
	public bool CheckDatabase()
	{
		bool exists = true;
		SqlCommand cmd = new($"SELECT name FROM master.sys.databases WHERE name = '{destinationDB}'", db.Connection);
		string result = (string)cmd.ExecuteScalar();
		if (result is null)
		{
			exists = false;
		}
		return exists;
	}

	public bool CreateDatabase()
	{
		bool success = true;
		SqlCommand cmd = new($"CREATE DATABASE [{destinationDB}] COLLATE {CollationName}", db.Connection);

		try
		{
			cmd.ExecuteScalar();
		}
		catch (SqlException e)
		{
			GD.Print(e);
			success = false;
		}
		return success;
	}

	private static void ProcessSQLFile(SqlConnection conn, string path)
	{
		FileInfo file = new(path);
		string[] splitCommands = file.OpenText().ReadToEnd().Split(new string[] { "GO\r\n" }, StringSplitOptions.RemoveEmptyEntries);
		// GD.PrintT("Running Script", path);

		foreach (string command in splitCommands)
		{
			SqlCommand cmd = conn.CreateCommand();
			SqlTransaction transaction = conn.BeginTransaction();

			cmd.Connection = conn;
			cmd.Transaction = transaction;

			try
			{
				cmd.CommandText = command;
				cmd.ExecuteNonQuery();
				transaction.Commit();
			}
			catch (Exception e)
			{
				GD.Print("Failed to commit transaction: {0}", e.GetType());
				GD.Print("Message: {0}", e.Message);

				try
				{
					transaction.Rollback();
				}
				catch (Exception e2)
				{
					GD.Print("Rollback Exception Type: {0}", e2.GetType());
					GD.Print("  Message: {0}", e2.Message);
				}
			}
		}
	}
}
