using Godot;
using System.Data.SqlClient;

public partial class Database : GodotObject
{
	private string serverName;
	private string username;
	private string password;
	private string databaseName;
	private SqlConnection conn;

	public Database(string server, string uName, string pwd, string database = "master")
	{
		GD.PrintT(server, username, password, database);
		serverName = server;
		username = uName;
		password = pwd;
		databaseName = database == "" ? "master" : database;
	}

	private string ServerName
	{
		get => serverName;
		set => serverName = value;
	}

	private string Username
	{
		get => username;
		set => username = value;
	}

	private string Password
	{
		get => password;
		set => password = value;
	}

	public string DatabaseName
	{
		get => databaseName;
		set => databaseName = value;
	}

	public SqlConnection Connection
	{
		get => conn;
	}

	/**
	 * Tests a connection to the Database Engine
	 */
	// public bool TestConnection(string catalog = "master")
	// {
	// 	bool success = false;
	// 	SqlConnection conn = NewConnection(catalog);

	// 	try
	// 	{
	// 		conn.Close();
	// 		success = true;
	// 	}
	// 	catch (SqlException e)
	// 	{
	// 		GD.Print(e.Message);
	// 		success = false;
	// 	}
	// 	return success;
	// }

	/**
	 * Returns an SqlConnection object and opens the connection.
	 * This function should be executed after attempting TestConnection with the same catalog name.
	 */
	public bool NewConnection(string catalog)
	{
		bool success = false;

		string connectionString = "Data Source=" + ServerName + ";Initial Catalog=" + catalog +
			";User ID=" + Username + ";Password=" + Password;
		GD.Print(connectionString);
		SqlConnection conn = new SqlConnection(connectionString);
		try
		{
			conn.Open();
			success = true;
		}
		catch (SqlException e)
		{
			GD.Print(e.Message);
			conn = null;
		}

		this.conn = conn;
		return success;
	}
}
