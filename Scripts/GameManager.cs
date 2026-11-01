
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
	private const double ScorePopDuration = 0.65;
	private const int SegmentInset = 2;

	private static readonly Color BackgroundColor = new("#101820");
	private static readonly Color BoardColor = new("#182C32");
	private static readonly Color BoardShadowColor = new("#091217");
	private static readonly Color GridColor = new("#29434A");
	private static readonly Color BorderColor = new("#5C8D89");
	private static readonly Color SnakeHeadColor = new("#B8E986");
	private static readonly Color SnakeBodyColor = new("#73C979");
	private static readonly Color FoodColor = new("#FF6B6B");
	private static readonly Color FoodHighlightColor = new("#FFD6A5");
	private static readonly Color FoodStemColor = new("#8BD17C");
	private static readonly Color EyeColor = new("#122026");
	private static readonly Color BackdropAccentColor = new("#1A3037");
	private static readonly Color ScorePopColor = new("#FFE66D");
	private static readonly Color TextColor = new("#F4F1DE");
	private static readonly Color MutedTextColor = new("#AAB7B8");
	private static readonly Color OverlayColor = new("#0A1119E6");
	private static readonly Color PanelColor = new("#14252CEB");

	private readonly List<Vector2I> _snake = new();
	private readonly RandomNumberGenerator _random = new();
	private Vector2I _direction = Vector2I.Right;
	private Vector2I _queuedDirection = Vector2I.Right;
	private Vector2I _food;
	private bool _hasQueuedTurn;
	private double _moveElapsed;
	private double _visualElapsed;
	private double _scorePopRemaining;
	private int _score;
	private int _bestScore;
	private GameState _state;

	public override void _Ready()
	{
		GD.Print("PixelSnake initialized successfully!");
		_random.Randomize();
		ShowTitle();
		QueueRedraw();
	}

	public override void _Process(double delta)
	{
		if (_state != GameState.Playing)
		{
			return;
		}

		_visualElapsed += delta;
		_scorePopRemaining = Mathf.Max(0.0f, (float)(_scorePopRemaining - delta));
		_moveElapsed += delta;

		while (_state == GameState.Playing && _moveElapsed >= CurrentMoveInterval)
		{
			_moveElapsed -= CurrentMoveInterval;
			MoveSnake();
		}

		QueueRedraw();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is not InputEventKey { Pressed: true, Echo: false } keyEvent)
		{
			return;
		}

		if (!HandleStateInput(keyEvent.Keycode))
		{
			TryQueueDirection(keyEvent.Keycode);
		}
	}

	public override void _Draw()
	{
		// Background
		DrawRect(
			new Rect2(0, 0, 640, 480),
			BackgroundColor
		);
		DrawBackdrop();

		DrawRect(
			new Rect2(OffsetX - 4, OffsetY - 4, BoardWidth + 8, BoardHeight + 8),
			BoardShadowColor
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

		if (_state != GameState.Title)
		{
			DrawSnake();
			if (_state != GameState.Victory)
			{
				DrawFood();
			}
		}

		DrawInterface();
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
		_scorePopRemaining = 0.0;
		_state = GameState.Playing;
		SpawnFood();
	}

	private void ShowTitle()
	{
		_snake.Clear();
		_hasQueuedTurn = false;
		_moveElapsed = 0.0;
		_score = 0;
		_state = GameState.Title;
		QueueRedraw();
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
			_bestScore = Mathf.Max(_bestScore, _score);
			_scorePopRemaining = ScorePopDuration;
			GD.Print($"Score: {_score}");
			SpawnFood();
		}
		else
		{
			_snake.RemoveAt(_snake.Count - 1);
		}

		QueueRedraw();
	}

	private bool HandleStateInput(Key keycode)
	{
		switch (_state)
		{
			case GameState.Title:
				if (keycode is Key.Enter or Key.Space)
				{
					ResetGame();
					QueueRedraw();
					return true;
				}
				return false;

			case GameState.Playing:
				if (keycode is Key.Space or Key.Escape)
				{
					_state = GameState.Paused;
					QueueRedraw();
					return true;
				}
				return false;

			case GameState.Paused:
				if (keycode is Key.Space or Key.Escape)
				{
					_state = GameState.Playing;
					QueueRedraw();
					return true;
				}
				return false;

			case GameState.GameOver:
			case GameState.Victory:
				if (keycode is Key.Enter or Key.Space)
				{
					ResetGame();
					QueueRedraw();
					return true;
				}

				if (keycode == Key.Escape)
				{
					ShowTitle();
					return true;
				}
				return false;

			default:
				return false;
		}
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

			if (index == 0)
			{
				DrawSnakeEyes(pixelX, pixelY);
			}
		}
	}

	private void DrawFood()
	{
		float pixelX = OffsetX + _food.X * CellSize;
		float pixelY = OffsetY + _food.Y * CellSize;
		float bobOffset = (int)(_visualElapsed * 5.0) % 2;

		DrawRect(new Rect2(pixelX + 11, pixelY + 3 + bobOffset, 3, 5), FoodStemColor);
		DrawRect(new Rect2(pixelX + 7, pixelY + 7 + bobOffset, 10, 14), FoodColor);
		DrawRect(new Rect2(pixelX + 5, pixelY + 10 + bobOffset, 14, 8), FoodColor);
		DrawRect(new Rect2(pixelX + 8, pixelY + 9 + bobOffset, 3, 3), FoodHighlightColor);
	}

	private void DrawSnakeEyes(float pixelX, float pixelY)
	{
		Vector2 firstEye;
		Vector2 secondEye;

		if (_direction == Vector2I.Right)
		{
			firstEye = new Vector2(13, 4);
			secondEye = new Vector2(13, 13);
		}
		else if (_direction == Vector2I.Left)
		{
			firstEye = new Vector2(4, 4);
			secondEye = new Vector2(4, 13);
		}
		else if (_direction == Vector2I.Up)
		{
			firstEye = new Vector2(4, 4);
			secondEye = new Vector2(13, 4);
		}
		else
		{
			firstEye = new Vector2(4, 13);
			secondEye = new Vector2(13, 13);
		}

		DrawRect(new Rect2(new Vector2(pixelX, pixelY) + firstEye, new Vector2(3, 3)), EyeColor);
		DrawRect(new Rect2(new Vector2(pixelX, pixelY) + secondEye, new Vector2(3, 3)), EyeColor);
	}

	private void DrawBackdrop()
	{
		DrawRect(new Rect2(34, 48, 24, 4), BackdropAccentColor);
		DrawRect(new Rect2(34, 56, 12, 4), BackdropAccentColor);
		DrawRect(new Rect2(582, 420, 24, 4), BackdropAccentColor);
		DrawRect(new Rect2(594, 428, 12, 4), BackdropAccentColor);
	}

	private void DrawInterface()
	{
		switch (_state)
		{
			case GameState.Title:
				DrawTitleScreen();
				break;

			case GameState.Playing:
				DrawHud();
				break;

			case GameState.Paused:
				DrawHud();
				DrawOverlay("PAUSED", "SPACE / ESC TO RESUME");
				break;

			case GameState.GameOver:
				DrawHud();
				DrawOverlay("GAME OVER", "ENTER / SPACE TO RESTART", "ESC TO RETURN TO TITLE");
				break;

			case GameState.Victory:
				DrawHud();
				DrawOverlay("BOARD CLEARED!", "ENTER / SPACE TO PLAY AGAIN", "ESC TO RETURN TO TITLE");
				break;
		}
	}

	private void DrawTitleScreen()
	{
		DrawRect(new Rect2(106, 95, 428, 290), PanelColor);
		DrawRect(new Rect2(106, 95, 428, 290), BorderColor, false, 2.0f);

		DrawCenteredText("PIXEL SNAKE", 175, 42, SnakeHeadColor);
		DrawCenteredText("RETRO ARCADE EDITION", 210, 16, MutedTextColor);
		DrawCenteredText("PRESS ENTER OR SPACE TO START", 275, 18, TextColor);
		DrawCenteredText("ARROWS / WASD  MOVE", 320, 16, MutedTextColor);
		DrawCenteredText("SPACE / ESC  PAUSE", 348, 16, MutedTextColor);
	}

	private void DrawHud()
	{
		DrawString(ThemeDB.FallbackFont, new Vector2(82, 38), $"SCORE  {_score:0000}",
			HorizontalAlignment.Left, -1, 18, TextColor);
		DrawString(ThemeDB.FallbackFont, new Vector2(420, 38), $"BEST  {_bestScore:0000}",
			HorizontalAlignment.Left, -1, 18, TextColor);

		if (_scorePopRemaining > 0.0)
		{
			float progress = 1.0f - (float)(_scorePopRemaining / ScorePopDuration);
			DrawCenteredText("+10", OffsetY + 32 - progress * 16, 20, ScorePopColor);
		}
	}

	private void DrawOverlay(string title, string primaryInstruction, string secondaryInstruction = null)
	{
		DrawRect(new Rect2(0, 0, 640, 480), OverlayColor);
		DrawRect(new Rect2(112, 165, 416, 150), PanelColor);
		DrawRect(new Rect2(112, 165, 416, 150), BorderColor, false, 2.0f);

		DrawCenteredText(title, 220, 30, TextColor);
		DrawCenteredText(primaryInstruction, 260, 16, MutedTextColor);

		if (secondaryInstruction is not null)
		{
			DrawCenteredText(secondaryInstruction, 286, 16, MutedTextColor);
		}
	}

	private void DrawCenteredText(string text, float baselineY, int fontSize, Color color)
	{
		DrawString(ThemeDB.FallbackFont, new Vector2(0, baselineY), text,
			HorizontalAlignment.Center, 640, fontSize, color);
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
		Title,
		Playing,
		Paused,
		GameOver,
		Victory
	}
}
