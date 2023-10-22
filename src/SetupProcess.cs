using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.Intrinsics.Arm;

enum Tabs
{
	Prereqs = 0,
	Tables = 1,
	Functions = 2,
	Views = 3,
	Procs = 4,
	Triggers = 5,
	Postreqs = 6
}
/**
 * Class controls interactions with the SetupProcess window.
 */
public partial class SetupProcess : Window
{
	private int currentTab;
	private FileDialog fileDialog;
	private DeploymentProcess dp;
	private string lastDirVisited;
	private bool movingToTab = false;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		currentTab = 0;
		TabBar tabs = GetNode<TabBar>("TabBar");
		tabs.TabChanged += ChangeTab;
		tabs.TabClicked += MoveFilesToTab;

		GetNode<Button>("Control/btn_AddFiles").Pressed += ShowDialogOpen;
		GetNode<Button>("Control/btn_MoveUp").Pressed += MoveFilesUp;
		GetNode<Button>("Control/btn_MoveDown").Pressed += MoveFilesDown;
		GetNode<Button>("Control/ButtonControls/btn_Move").Pressed += EnableTabMove;

		Deploy m = (Deploy)GetParent();
		dp = m.GetDeploymentProcess;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (dp != null)
		{
			// check if at least on script has been added. If one has, enable Confirm button
			Button confirm = GetNode<Button>("Control/ButtonControls/btn_Confirm");
			if (dp.GetScriptCount() > 0)
			{
				confirm.Disabled = false;
			}
			else
			{
				confirm.Disabled = true;
			}
		}
	}

	/**
	 *	Updates the window when a tab is changed. Refreshes the file list and information label.
	 *	This only occurs when the moving mode is not active.
	 */
	private void ChangeTab(long tab)
	{
		if (!movingToTab)
		{
			Label info = GetNode<Label>("Control/Label_TabInfo");
			info.Text = tab switch
			{
				(long)Tabs.Prereqs => "INFO_PREREQ",
				(long)Tabs.Tables => "INFO_TABLES",
				(long)Tabs.Functions => "INFO_FUNCTIONS",
				(long)Tabs.Views => "INFO_VIEWS",
				(long)Tabs.Procs => "INFO_PROCEDURES",
				(long)Tabs.Triggers => "INFO_TRIGGERS",
				(long)Tabs.Postreqs => "INFO_POSTREQ",
				_ => "INFO_GENERIC",
			};
			currentTab = (int)tab;
			UpdateFileList();
		}
	}

	/**
	 * Updates the Itemlist container with the list of files for the currently active category. 
	 */
	private void UpdateFileList()
	{
		if (dp != null)
		{
			ItemList list = GetNode<ItemList>("Control/FileList");
			ScriptCategory scripts = dp.GetScriptCategory(currentTab) ?? null;
			list.Clear();
			if (scripts != null)
			{
				foreach (string file in scripts.ScriptNames)
				{
					list.AddItem(file);
				}
			}
		}
		ToggleMoveButtons(-1, false);
	}

	/**
	 * Used as a signal action for adding files the the item list.
	 */
	private void ShowDialogOpen()
	{
		ShowFileDialog(FileDialog.FileModeEnum.OpenFiles, new string[] { "*.sql" });
	}

	/**
	 * Opens the file dialog for adding files to the deployment and for saving/loading a deployment preset.
	 */
	private void ShowFileDialog(FileDialog.FileModeEnum mode, string[] filters)
	{
		if (fileDialog is null)
		{
			fileDialog = new FileDialog
			{
				InitialPosition = WindowInitialPosition.CenterMainWindowScreen,
				Size = new Vector2I(950, 575),
				Access = FileDialog.AccessEnum.Filesystem,
				FileMode = mode,
				Filters = filters,
				CurrentDir = lastDirVisited,
				Transient = false,
				Mode = ModeEnum.Maximized
			};
		}

		switch (mode)
		{
			// Add Files
			case FileDialog.FileModeEnum.OpenFiles:
				fileDialog.FilesSelected += AddFiles;
				break;
			// Load preset
			case FileDialog.FileModeEnum.OpenFile:
				fileDialog.FileSelected += LoadPreset;
				break;
			// Save preset
			case FileDialog.FileModeEnum.SaveFile:
				fileDialog.FileSelected += SavePreset;
				break;
		}

		fileDialog.CloseRequested += FocusWindow;
		fileDialog.Canceled += FocusWindow;
		GetParent().AddChild(fileDialog);

		Unfocusable = true;
		fileDialog.Show();
	}

	/**
	 * Sets the unfocasable property of the SetupProcess window to false and destroys the file dialog object.
	 */
	private void FocusWindow()
	{
		Unfocusable = false;

		lastDirVisited = fileDialog.CurrentDir;
		GetParent().RemoveChild(fileDialog);
		fileDialog = null;
	}

	/**
	 * Adds the selected files from the file dialog to the current category's list.
	 */
	private void AddFiles(string[] paths)
	{
		FocusWindow();

		if (dp is null)
		{
			Deploy m = (Deploy)GetParent();
			dp = m.GetDeploymentProcess;
		}

		dp.AddScripts(currentTab, GetNode<TabBar>("TabBar").GetTabTitle(currentTab), paths);
		UpdateFileList();
	}

	/**
	 * Removes the selected files in the ItemList from the list and ScriptCategory object.
	 */
	private void RemoveFiles()
	{
		ItemList fileList = GetNode<ItemList>("Control/FileList");
		int[] files = fileList.GetSelectedItems();
		string fileName;
		files = files.OrderByDescending(c => c).ToArray();

		foreach (int file in files)
		{
			fileName = fileList.GetItemText(file);
			dp.RemoveScript(currentTab, fileName);
		}
		UpdateFileList();
	}

	private void EnableTabMove()
	{
		movingToTab = true;

		GetNode<TabBar>("TabBar").SetTabDisabled(currentTab, true);
		GetNode<ColorRect>("ColorRect").Visible = true;
	}

	private void MoveFilesToTab(long destination)
	{
		if (movingToTab)
		{
			TabBar tabs = GetNode<TabBar>("TabBar");
			ItemList fileList = GetNode<ItemList>("Control/FileList");
			dp.MoveScripts(CurrentTab, (int)destination, fileList.GetSelectedItems());

			tabs.SetTabDisabled(currentTab, false);
			tabs.CurrentTab = currentTab;
			movingToTab = false;
			GetNode<ColorRect>("ColorRect").Visible = false;
			UpdateFileList();
		}
	}

	/**
	 * Signal action to move files up in the ItemList and ScriptCategory object.
	 */
	private void MoveFilesUp()
	{
		MoveSelectedFiles(true);
	}

	/**
	 * Signal action to move files down in the ItemList and ScriptCategory object.
	 */
	private void MoveFilesDown()
	{
		MoveSelectedFiles(false);
	}

	/**
	 * Moves the currently selected files in the item list and their respective ScriptCategory lists.
	 */
	private void MoveSelectedFiles(bool moveUp)
	{
		ItemList fileList = GetNode<ItemList>("Control/FileList");
		int[] selected = fileList.GetSelectedItems();
		string fileName;
		int newIndex;
		selected = selected.OrderByDescending(c => c).ToArray();

		foreach (int index in selected)
		{
			newIndex = moveUp ? index - 1 : index + 1;
			fileName = fileList.GetItemText(index);
			if (index < fileList.ItemCount && newIndex >= 0 && newIndex < fileList.ItemCount && index != newIndex)
			{
				dp.Scripts[currentTab].MoveScript(index, newIndex);
				fileList.MoveItem(index, newIndex);
			}
		}
	}

	private void ToggleMoveButtons(int index, bool selected)
	{
		GetNode<Button>("Control/btn_MoveUp").Disabled = !selected;
		GetNode<Button>("Control/btn_MoveDown").Disabled = !selected;
		GetNode<Button>("Control/ButtonControls/btn_Move").Disabled = !selected;
		GetNode<Button>("Control/ButtonControls/btn_Remove").Disabled = !selected;
	}

	private void ShowSaveDialog()
	{
		ShowFileDialog(FileDialog.FileModeEnum.SaveFile, new string[] { "*.txt" });
	}

	private void ShowLoadDialog()
	{
		ShowFileDialog(FileDialog.FileModeEnum.OpenFile, new string[] { "*.txt" });
	}

	/**
	 * Saves the currently selected files in the setup window for each tab category.
	 */
	private void SavePreset(string dir)
	{
		using var presetFile = Godot.FileAccess.Open(dir, Godot.FileAccess.ModeFlags.Write);
		string data = "";
		int index = 0;

		foreach (ScriptCategory script in dp.Scripts)
		{
			if (script != null)
			{
				presetFile.StoreLine($"CAT:{index},{script.Category}");
				for (int i = 0; i < script.ScriptCount; i++)
				{
					presetFile.StoreLine($"{script.ScriptName(i)},{script.ScriptPath(i)}");
				}
			}
			index++;
		}
		presetFile.StoreString(data);
		FocusWindow();
	}

	/**
	 * Loads the preset content from the selected file into the setup window and ScriptCategory object.
	 * Also checks to see if the files stored in the preset file exist. Any files that don't exist will be recorded and displayed to the user.
	 */
	private void LoadPreset(string dir)
	{
		string errorMsg = null;

		if (dp is null)
		{
			Deploy m = (Deploy)GetParent();
			dp = m.GetDeploymentProcess;
		}

		if (Godot.FileAccess.FileExists(dir))
		{
			using var presetFile = Godot.FileAccess.Open(dir, Godot.FileAccess.ModeFlags.Read);
			dp.ClearScripts();
			string category = null;
			int tabNum = 0;

			while (presetFile.GetPosition() < presetFile.GetLength())
			{
				// Get the name and tab number for the corresponding category
				string line = presetFile.GetLine();
				if (line.Contains("CAT:") && line.Replace("CAT:", "") != category)
				{
					string[] cat = line.Replace("CAT:", "").Split(',');
					category = cat[1];
					tabNum = cat[0].ToInt();
				}
				else
				{
					string[] file = line.Split(',');
					dp.AddScripts(tabNum, category, file[1]);
				}
			}
		}
		else
		{
			errorMsg = "The specified file does not exist.";
		}

		if (errorMsg != null)
		{
			GD.Print(errorMsg);
		}
		FocusWindow();
		UpdateFileList();
	}

	public int CurrentTab
	{
		get => currentTab;
	}
}
