using Godot;

/**
 * Class that controls interaction with the Window_PreDeployment window node.
 */
public partial class PreDeployment : ConfirmationDialog
{
	private bool commence;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		commence = false;

		Confirmed += ConfirmedPress;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
	{
    }

	public bool Commence
	{
		get => commence;
	}

	private void ConfirmedPress()
	{
        commence = true;
    }
}
