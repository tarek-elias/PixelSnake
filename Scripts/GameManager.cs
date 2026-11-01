
using Godot;

public partial class GameManager : Node2D
{
	// Grid configuration
	private const int CellSize = 24;
	private const int Columns = 20;
	private const int Rows = 15;

	// Board dimensions
	private const int BoardWidth = Columns * CellSize;
	private const int BoardHeight = Rows * CellSize;

	// Center the board inside the 640 x 480 window
	private const int OffsetX = (640 - BoardWidth) / 2;
	private const int OffsetY = (480 - BoardHeight) / 2;

	public override void _Ready()
	{
		GD.Print("PixelSnake initialized successfully!");
		QueueRedraw();
	}

	public override void _Draw()
	{
		// Background
		DrawRect(
			new Rect2(0, 0, 640, 480),
			new Color("#101820")
		);

		// Game board
		DrawRect(
			new Rect2(OffsetX, OffsetY, BoardWidth, BoardHeight),
			new Color("#182C32")
		);

		// Vertical grid lines
		for (int x = 0; x <= Columns; x++)
		{
			float positionX = OffsetX + x * CellSize;

			DrawLine(
				new Vector2(positionX, OffsetY),
				new Vector2(positionX, OffsetY + BoardHeight),
				new Color("#29434A"),
				1.0f
			);
		}

		// Horizontal grid lines
		for (int y = 0; y <= Rows; y++)
		{
			float positionY = OffsetY + y * CellSize;

			DrawLine(
				new Vector2(OffsetX, positionY),
				new Vector2(OffsetX + BoardWidth, positionY),
				new Color("#29434A"),
				1.0f
			);
		}

		// Board border
		DrawRect(
			new Rect2(OffsetX, OffsetY, BoardWidth, BoardHeight),
			new Color("#5C8D89"),
			false,
			2.0f
		);
	}
}
