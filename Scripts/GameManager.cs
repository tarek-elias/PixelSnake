
using System.Collections.Generic;
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
	private const double InitialMoveInterval = 0.18;
	private const double MinimumMoveInterval = 0.08;
	private const double SpeedIncreasePerFood = 0.01;
	private const int SegmentInset = 2;

	private static readonly Color BackgroundColor = new("#101820");
	private static readonly Color BoardColor = new("#182C32");
	private static readonly Color GridColor = new("#29434A");
	private static readonly Color BorderColor = new("#5C8D89");
	private static readonly Color SnakeHeadColor = new("#B8E986");
	private static readonly Color SnakeBodyColor = new("#73C979");
	private static readonly Color FoodColor = new("#FF6B6B");

	private readonly List<Vector2I> _snake = new();
	private readonly RandomNumberGenerator _random = new();
	private Vector2I _direction = Vector2I.Right;
	private Vector2I _queuedDirection = Vector2I.Right;
	private Vector2I _food;
	private bool _hasQueuedTurn;
	private double _moveElapsed;
	private int _score;
	private GameState _state;

	public override void _Ready()
	{
		GD.Print("PixelSnake initialized successfully!");
		_random.Randomize();
		ResetGame();
		QueueRedraw();
	}

	public override void _Process(double delta)
	{
		if (_state != GameState.Playing)
		{
			return;
		}

		_moveElapsed += delta;

		while (_state == GameState.Playing && _moveElapsed >= CurrentMoveInterval)
		{
			_moveElapsed -= CurrentMoveInterval;
			MoveSnake();
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is not InputEventKey { Pressed: true, Echo: false } keyEvent)
		{
			return;
		}

		TryQueueDirection(keyEvent.Keycode);
	}

	public override void _Draw()
	{
		// Background
		DrawRect(
			new Rect2(0, 0, 640, 480),
			BackgroundColor
		);

		// Game board
		DrawRect(
			new Rect2(OffsetX, OffsetY, BoardWidth, BoardHeight),
			BoardColor
		);

		// Vertical grid lines
		for (int x = 0; x <= Columns; x++)
		{
			float positionX = OffsetX + x * CellSize;

			DrawLine(
				new Vector2(positionX, OffsetY),
				new Vector2(positionX, OffsetY + BoardHeight),
				GridColor,
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
				GridColor,
				1.0f
			);
		}

		// Board border
		DrawRect(
			new Rect2(OffsetX, OffsetY, BoardWidth, BoardHeight),
			BorderColor,
			false,
			2.0f
		);

		DrawSnake();
		if (_state != GameState.Victory)
		{
			DrawFood();
		}
	}

	private double CurrentMoveInterval => Mathf.Max(
		(float)MinimumMoveInterval,
		(float)(InitialMoveInterval - (_score / 10) * SpeedIncreasePerFood)
	);

	private void ResetGame()
	{
		_snake.Clear();
		_snake.Add(new Vector2I(10, 7));
		_snake.Add(new Vector2I(9, 7));
		_snake.Add(new Vector2I(8, 7));
		_direction = Vector2I.Right;
		_queuedDirection = Vector2I.Right;
		_hasQueuedTurn = false;
		_moveElapsed = 0.0;
		_score = 0;
		_state = GameState.Playing;
		SpawnFood();
	}

	private void MoveSnake()
	{
		if (_hasQueuedTurn)
		{
			_direction = _queuedDirection;
			_hasQueuedTurn = false;
		}

		Vector2I newHead = _snake[0] + _direction;
		bool ateFood = newHead == _food;

		if (!IsInsideBoard(newHead) || HitsSnake(newHead, ateFood))
		{
			_state = GameState.GameOver;
			GD.Print($"Game over. Score: {_score}");
			QueueRedraw();
			return;
		}

		_snake.Insert(0, newHead);

		if (ateFood)
		{
			_score += 10;
			GD.Print($"Score: {_score}");
			SpawnFood();
		}
		else
		{
			_snake.RemoveAt(_snake.Count - 1);
		}

		QueueRedraw();
	}

	private void TryQueueDirection(Key keycode)
	{
		if (_state != GameState.Playing || _hasQueuedTurn)
		{
			return;
		}

		Vector2I? requestedDirection = keycode switch
		{
			Key.Up or Key.W => Vector2I.Up,
			Key.Down or Key.S => Vector2I.Down,
			Key.Left or Key.A => Vector2I.Left,
			Key.Right or Key.D => Vector2I.Right,
			_ => null
		};

		if (requestedDirection is null || requestedDirection == _direction || requestedDirection == -_direction)
		{
			return;
		}

		_queuedDirection = requestedDirection.Value;
		_hasQueuedTurn = true;
	}

	private void DrawSnake()
	{
		for (int index = _snake.Count - 1; index >= 0; index--)
		{
			Color color = index == 0 ? SnakeHeadColor : SnakeBodyColor;
			Vector2I segment = _snake[index];
			float pixelX = OffsetX + segment.X * CellSize + SegmentInset;
			float pixelY = OffsetY + segment.Y * CellSize + SegmentInset;
			float segmentSize = CellSize - SegmentInset * 2;

			DrawRect(new Rect2(pixelX, pixelY, segmentSize, segmentSize), color);
		}
	}

	private void DrawFood()
	{
		float pixelX = OffsetX + _food.X * CellSize + SegmentInset;
		float pixelY = OffsetY + _food.Y * CellSize + SegmentInset;
		float foodSize = CellSize - SegmentInset * 2;

		DrawRect(new Rect2(pixelX, pixelY, foodSize, foodSize), FoodColor);
	}

	private bool IsInsideBoard(Vector2I cell)
	{
		return cell.X >= 0 && cell.X < Columns && cell.Y >= 0 && cell.Y < Rows;
	}

	private bool HitsSnake(Vector2I cell, bool growing)
	{
		if (!_snake.Contains(cell))
		{
			return false;
		}

		// Moving into the current tail is legal when it moves away this tick.
		return growing || cell != _snake[^1];
	}

	private void SpawnFood()
	{
		List<Vector2I> freeCells = new();

		for (int y = 0; y < Rows; y++)
		{
			for (int x = 0; x < Columns; x++)
			{
				Vector2I cell = new(x, y);
				if (!_snake.Contains(cell))
				{
					freeCells.Add(cell);
				}
			}
		}

		if (freeCells.Count == 0)
		{
			_state = GameState.Victory;
			GD.Print($"Board cleared! Final score: {_score}");
			return;
		}

		_food = freeCells[_random.RandiRange(0, freeCells.Count - 1)];
	}

	private enum GameState
	{
		Playing,
		GameOver,
		Victory
	}
}
