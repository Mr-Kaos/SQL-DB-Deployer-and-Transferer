using Godot;

public partial class Deploy : Node
{
	private ProgramState state;
	private Database db;
	private DeploymentProcess dp;
	private ConfirmationDialog confirm;
	private Window lastWindow;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		state = ProgramState.INIT;
		Window initWindow = GetNode<Window>("Win_Connection");
		initWindow.GetNode<Button>("btnConnect").Pressed += ConnectDB;
		initWindow.Show();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		switch (state)
		{
			case ProgramState.LICENSE:
				break;
			case ProgramState.INIT:
				break;
			case ProgramState.PRE_SETUP:
				if (HasNode("Win_Connection"))
				{
					RemoveChild(GetNode<Window>("Win_Connection"));
				}

				AcceptDialog setupInfo = GetNode<AcceptDialog>("Win_SetupInfo");
				if (!setupInfo.Visible)
				{
					setupInfo.Show();
				}
				break;
			case ProgramState.FILE_SELECT:
				if (HasNode("Win_SetupInfo"))
				{
					RemoveChild(GetNode<Window>("Win_SetupInfo"));
				}

				SetupProcess setup = (SetupProcess)GetNode("Win_SetupProcess");
				if (!setup.Visible)
				{
					lastWindow = setup;
					setup.Show();
				}
				break;
			case ProgramState.DB_NAME:
				AcceptDialog dbNameDialog = GetNode<AcceptDialog>("Win_DatabaseName");

				if (!dbNameDialog.Visible)
				{
					lastWindow = dbNameDialog;
					dbNameDialog.GetOkButton().Disabled = true;
					dbNameDialog.Show();
				}
				else
				{
					string dbName = dbNameDialog.GetNode<LineEdit>("VContainer/Inputs/In_DBName").Text;
					dp.DestinationDB = dbName;
					dp.CollationName = dbNameDialog.GetNode<LineEdit>("VContainer/Inputs/In_Collation").Text;
					dbNameDialog.GetOkButton().Disabled = dbName == "";
				}

				break;
			case ProgramState.PRE_DEPLOY:
				// If database exists, ask user for confirmation to deploy.
				if (dp.CheckDatabase())
				{
					PreDeployment deploymentConfirm = (PreDeployment)GetNode("Win_DeploymentConfirm");

					if (!deploymentConfirm.Visible)
					{
						lastWindow = deploymentConfirm;
						deploymentConfirm.Show();
					}
					else if (deploymentConfirm.Commence)
					{
						deploymentConfirm.Hide();
						state++;
					}
				}
				// Else, ask user if they would like to create a new database with the given name
				else
				{
					if (lastWindow is null)
					{
						ConfirmationDialog confirmDBName = new ConfirmationDialog()
						{
							OkButtonText = "Yes",
							CancelButtonText = "No",
							Title = "Confirm Database Creation",
							InitialPosition = Window.WindowInitialPosition.CenterPrimaryScreen,
							WrapControls = true,
							MinSize = new Vector2I(400, 200),
							Name = "ConfirmDBName"
						};
						confirmDBName.AddChild(new Label
						{
							Text = $"The database \"{dp.DestinationDB}\" does not exist. Are you sure you want to create it with the collation \"{dp.CollationName}\"?",
							AutowrapMode = TextServer.AutowrapMode.Word
						});
						confirmDBName.Confirmed += ConfirmDBCreation;
						confirmDBName.Canceled += CancelDBCreation;
						confirmDBName.Show();

						AddChild(confirmDBName);
						lastWindow = confirmDBName;
					}
				}
				break;
			case ProgramState.DEPLOY:
				if (!dp.DeploymentComplete)
				{
					// Free previous windows and commence deployment
					GetNode("Win_SetupProcess").Free();
					GetNode("Win_DatabaseName").Free();
					GetNode("Win_DeploymentConfirm").Free();
					dp.CommenceDeployment();
				}
				else
				{
					state++;
				}
				break;
			case ProgramState.DEPLOY_COMPLETE:
				if (lastWindow is null)
				{
					AcceptDialog complete = GetNode<AcceptDialog>("Win_DeployComplete");
					complete.Confirmed += QuitProgram;
					complete.Show();
					lastWindow = complete;
				}
				break;
		}
	}

	private void ConnectDB()
	{
		Window window = GetNode<Window>("Win_Connection");
		GridContainer container = window.GetNode<GridContainer>("GridContainer");
		string server = container.GetNode<LineEdit>("In_Server").Text;
		string username = container.GetNode<LineEdit>("In_Username").Text;
		string password = container.GetNode<LineEdit>("In_Password").Text;
		string dbName = container.GetNode<LineEdit>("In_DBName").Text;
		db = new Database(server, username, password, dbName);

		if (db.NewConnection(dbName))
		{
			window.GetNode<Label>("Label_Error").Text = "";
			ProceedWindow();
			window.Hide();
			dp = new DeploymentProcess(db);
		}
		else
		{
			window.GetNode<Label>("Label_Error").Text = "ERR_CONNECT";
		}
	}

	public DeploymentProcess GetDeploymentProcess
	{
		get => dp;
	}

	/**
	 * Proceeds the program by incrementing the ProgramState enum and hiding the currently active window.
	 */
	private void ProceedWindow()
	{
		state++;
		lastWindow?.Hide();
		GD.Print(lastWindow is null ? "No window unset." : "Unset window " + lastWindow.Title);
		lastWindow = null;
	}

	private void ReturnToSetup()
	{
		state = ProgramState.FILE_SELECT;
		lastWindow?.Hide();
		lastWindow = null;
	}

	private void CancelDBCreation()
	{
		lastWindow.Hide();
		lastWindow.Free();
		state = ProgramState.DB_NAME;
	}

	private void ConfirmDBCreation()
	{
		if (dp.CreateDatabase())
		{
			GD.Print("successfully created database.");
			RemoveChild(lastWindow);
			lastWindow = null;
		}
	}

	private void QuitProgram()
	{
		GetTree().Quit();
	}
}

enum ProgramState
{
	LICENSE = 0,
	INIT = 1,
	PRE_SETUP = 2,
	FILE_SELECT = 3,
	DB_NAME = 4,
	PRE_DEPLOY = 5,
	DEPLOY = 6,
	DEPLOY_COMPLETE = 7,
	TRANSFER = 8
}
