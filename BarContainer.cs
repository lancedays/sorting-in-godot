using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class BarContainer : Control
{
	private SpinBox _spinBox;
	private HSlider _speedSlider;
	private OptionButton _algorithmSelect;
	private Button _playButton;
	private Button _resetButton;
	private CheckButton _muteButton;
	private Button _pauseButton;
	private AudioStreamPlayer _audioStreamPlayer;
	private int _totalRectangles = 10;
	private List<float> _heights = new List<float>();
	private bool _isSorting = false;
	private bool _isPaused = false;
	private bool _isMuted = false;
	private Color _defaultBarColor = Colors.Green;
	private Color _evaluatedBarColor = Colors.Blue;
	private int _evaluatedIndex = -1;

	public override void _Ready()
	{
		// Get the UI controls
		_spinBox = GetNode<SpinBox>("/root/Main/UI/ListSizeSelect");
		_speedSlider = GetNode<HSlider>("/root/Main/UI/SpeedSlider");
		_algorithmSelect = GetNode<OptionButton>("/root/Main/UI/AlgorithmSelect");
		_playButton = GetNode<Button>("/root/Main/UI/PlayButton");
		_resetButton = GetNode<Button>("/root/Main/UI/ResetButton");
		_pauseButton = GetNode<Button>("/root/Main/UI/PauseButton");
		_audioStreamPlayer = GetNode<AudioStreamPlayer>("/root/Main/Sound/AudioStreamPlayer");
		_muteButton = GetNode<CheckButton>("/root/Main/UI/MuteButton");

		// Connect signals to methods
		_spinBox.ValueChanged += OnSpinBoxValueChanged;
		_playButton.Pressed += OnPlayButtonPressed;
		_resetButton.Pressed += OnResetButtonPressed;
		_pauseButton.Pressed += OnPauseButtonPressed;
		_muteButton.Pressed += OnMuteButtonPressed;

		// Add items to the OptionButton
		_algorithmSelect.AddItem("Bubble Sort");
		_algorithmSelect.AddItem("Insertion Sort");
		_algorithmSelect.AddItem("Merge Sort");
		_algorithmSelect.AddItem("Selection Sort");

		// Set the initial total rectangles from the SpinBox value
		_totalRectangles = (int)_spinBox.Value;

		// Initialize the heights of the rectangles
		InitializeRectangles();

		// Initial draw call
		QueueRedraw();

		// Shuffle the heights initially
		ShuffleRectangles();
	}
	private void InitializeRectangles()
	{
		_heights.Clear();

		// Get the size of the viewport
		Vector2 viewportSize = GetViewportRect().Size;
		float viewportHeight = viewportSize.Y;

		// Parameters for the rectangles
		float margin = 5.0f; // Margin from the edges

		// Calculate the available height and the height increment for each rectangle
		float availableHeight = viewportHeight - (2 * margin);
		float heightIncrement = availableHeight / _totalRectangles;

		for (int i = 0; i < _totalRectangles; i++)
		{
			// Calculate the height of the current rectangle
			float currentHeight = heightIncrement * (i + 1);
			// Store the height
			_heights.Add(currentHeight);
		}
	}
	public override void _Draw()
	{
		// Get the size of the viewport
		Vector2 viewportSize = GetViewportRect().Size;
		float viewportWidth = viewportSize.X;
		float viewportHeight = viewportSize.Y;
		float margin = 5.0f; // Margin from the edges

		// Get the width of each rectangle
		float availableWidth = viewportWidth - (2 * margin);
		float width = availableWidth / _totalRectangles;

		for (int i = 0; i < _totalRectangles; i++)
		{
			// Get the height of the current rectangle
			float currentHeight = _heights[i];
			// Calculate the X position of the current rectangle
			float currentX = margin + (width * i);
			// Calculate the Y position to align the bottoms of the rectangles
			float currentY = viewportHeight - margin - currentHeight;

			// Set the color for the current rectangle
			Color color = (i == _evaluatedIndex) ? _evaluatedBarColor : _defaultBarColor;

			// Draw the rectangle
			DrawRect(new Rect2(currentX, currentY, width, currentHeight), color);
		}
	}
	private void OnSpinBoxValueChanged(double value)
	{
		// Update the total rectangles from the SpinBox value
		_totalRectangles = (int)value;

		// Reinitialize and shuffle the heights of the rectangles
		InitializeRectangles();
		ShuffleRectangles();

		// Redraw the rectangles
		QueueRedraw();
	}
	private void OnPlayButtonPressed()
	{
		if (!_isSorting)
		{
			_isPaused = false;
			_isSorting = true;
			string selectedAlgorithm = _algorithmSelect.Text;
			Task.Run(() => Sort(selectedAlgorithm));
		}
	}
	private void OnResetButtonPressed()
	{
		if (_isSorting)
		{
			_isSorting = false;
			_isPaused = false;
		}

		// Reinitialize and shuffle the heights of the rectangles
		InitializeRectangles();
		ShuffleRectangles();

		// Redraw the rectangles
		QueueRedraw();
	}
	private void OnPauseButtonPressed()
	{
		_isPaused = !_isPaused;
	}
	private void OnMuteButtonPressed()
	{
		_isMuted = !_isMuted;
	}
	private async void Sort(string algorithm)
	{
		switch (algorithm)
		{
			case "Bubble Sort":
				await BubbleSort();
				break;
			case "Insertion Sort":
				await InsertionSort();
				break;
			case "Merge Sort":
				await MergeSort(0, _heights.Count - 1);
				break;
			case "Selection Sort":
				await SelectionSort();
				break;
		}
		_isSorting = false;
	}
	private async Task BubbleSort()
	{
		int n = _heights.Count;
		float maxDelay = 1.0f; // 1 second delay for the slowest speed
		for (int i = 0; i < n - 1 && _isSorting; i++)
		{
			for (int j = 0; j < n - i - 1 && _isSorting; j++)
			{
				_evaluatedIndex = j;

				if (_heights[j] > _heights[j + 1])
				{
					// Swap the heights
					float temp = _heights[j];
					_heights[j] = _heights[j + 1];
					_heights[j + 1] = temp;

					// Redraw the rectangles
					CallDeferred(nameof(DeferredQueueRedraw));

					// Play sound
					if (!_isMuted) { CallDeferred(nameof(DeferredPlaySound)); }

					// Calculate wait time based on slider value
					float waitTime = (float)(maxDelay / Math.Max(1, _speedSlider.Value));
					await Task.Delay(TimeSpan.FromSeconds(waitTime));

					// Wait if paused
					while (_isPaused)
					{
						await Task.Delay(100);
					}
				}

				// Redraw the rectangles
				CallDeferred(nameof(DeferredQueueRedraw));

				// Calculate wait time based on slider value
				float waitTimeStep = (float)(maxDelay / Math.Max(1, _speedSlider.Value));
				await Task.Delay(TimeSpan.FromSeconds(waitTimeStep));

				// Wait if paused
				while (_isPaused)
				{
					await Task.Delay(100);
				}
			}
		}
		_evaluatedIndex = -1;
	}
	private async Task InsertionSort()
	{
		int n = _heights.Count;
		float maxDelay = 1.0f; // 1 second delay for the slowest speed
		for (int i = 1; i < n && _isSorting; i++)
		{
			float key = _heights[i];
			int j = i - 1;

			while (j >= 0 && _heights[j] > key && _isSorting)
			{
				_heights[j + 1] = _heights[j];
				_evaluatedIndex = j;
				j = j - 1;

				// Redraw the rectangles
				CallDeferred(nameof(DeferredQueueRedraw));

				// Play sound
				if (!_isMuted) { CallDeferred(nameof(DeferredPlaySound)); }

				// Calculate wait time based on slider value
				float waitTime = (float)(maxDelay / Math.Max(1, _speedSlider.Value));
				await Task.Delay(TimeSpan.FromSeconds(waitTime));

				// Wait if paused
				while (_isPaused)
				{
					await Task.Delay(100);
				}
			}
			_heights[j + 1] = key;

			// Redraw the rectangles
			CallDeferred(nameof(DeferredQueueRedraw));

			// Calculate wait time based on slider value
			float waitTimeStep = (float)(maxDelay / Math.Max(1, _speedSlider.Value));
			await Task.Delay(TimeSpan.FromSeconds(waitTimeStep));

			// Wait if paused
			while (_isPaused)
			{
				await Task.Delay(100);
			}
		}
		_evaluatedIndex = -1;
	}
	private async Task Merge(int left, int mid, int right)
	{
		int n1 = mid - left + 1;
		int n2 = right - mid;

		float[] leftArray = new float[n1];
		float[] rightArray = new float[n2];

		for (int i = 0; i < n1; i++)
			leftArray[i] = _heights[left + i];
		for (int j = 0; j < n2; j++)
			rightArray[j] = _heights[mid + 1 + j];

		int k = left;
		int iIndex = 0, jIndex = 0;

		float maxDelay = 1.0f; // 1 second delay for the slowest speed

		while (iIndex < n1 && jIndex < n2 && _isSorting)
		{
			_evaluatedIndex = k;
			if (leftArray[iIndex] <= rightArray[jIndex])
			{
				_heights[k] = leftArray[iIndex];
				iIndex++;
			}
			else
			{
				_heights[k] = rightArray[jIndex];
				jIndex++;
			}
			k++;

			// Redraw the rectangles
			CallDeferred(nameof(DeferredQueueRedraw));

			// Play sound
			if (!_isMuted) { CallDeferred(nameof(DeferredPlaySound)); }

			// Calculate wait time based on slider value
			float waitTime = (float)(maxDelay / Math.Max(1, _speedSlider.Value));
			await Task.Delay(TimeSpan.FromSeconds(waitTime));

			// Wait if paused
			while (_isPaused)
			{
				await Task.Delay(100);
			}
		}

		while (iIndex < n1 && _isSorting)
		{
			_heights[k] = leftArray[iIndex];
			iIndex++;
			k++;

			// Redraw the rectangles
			CallDeferred(nameof(DeferredQueueRedraw));

			float waitTime = (float)(maxDelay / Math.Max(1, _speedSlider.Value));
			await Task.Delay(TimeSpan.FromSeconds(waitTime));
		}

		while (jIndex < n2 && _isSorting)
		{
			_heights[k] = rightArray[jIndex];
			jIndex++;
			k++;

			// Redraw the rectangles
			CallDeferred(nameof(DeferredQueueRedraw));

			float waitTime = (float)(maxDelay / Math.Max(1, _speedSlider.Value));
			await Task.Delay(TimeSpan.FromSeconds(waitTime));
		}

		_evaluatedIndex = -1;
	}
	private async Task MergeSort(int left, int right)
	{
		if (left < right && _isSorting)
		{
			int mid = left + (right - left) / 2;

			await MergeSort(left, mid);
			await MergeSort(mid + 1, right);

			await Merge(left, mid, right);
		}
	}
	private async Task SelectionSort()
	{
		int n = _heights.Count;
		float maxDelay = 1.0f; // 1 second delay for the slowest speed

		for (int i = 0; i < n - 1 && _isSorting; i++)
		{
			int minIndex = i;
			for (int j = i + 1; j < n && _isSorting; j++)
			{
				_evaluatedIndex = j;
				if (_heights[j] < _heights[minIndex])
				{
					minIndex = j;
				}

				// Redraw the rectangles
				CallDeferred(nameof(DeferredQueueRedraw));

				float waitTimeStep = (float)(maxDelay / Math.Max(1, _speedSlider.Value));
				await Task.Delay(TimeSpan.FromSeconds(waitTimeStep));

				// Wait if paused
				while (_isPaused)
				{
					await Task.Delay(100);
				}
			}

			// Swap the found minimum element with the first element
			float temp = _heights[minIndex];
			_heights[minIndex] = _heights[i];
			_heights[i] = temp;

			// Redraw the rectangles
			CallDeferred(nameof(DeferredQueueRedraw));

			// Play sound
			if (!_isMuted) { CallDeferred(nameof(DeferredPlaySound)); }

			// Calculate wait time based on slider value
			float waitTime = (float)(maxDelay / Math.Max(1, _speedSlider.Value));
			await Task.Delay(TimeSpan.FromSeconds(waitTime));

			// Wait if paused
			while (_isPaused)
			{
				await Task.Delay(100);
			}
		}
		_evaluatedIndex = -1;
	}
	private void ShuffleRectangles()
	{
		Random rng = new Random();
		int n = _heights.Count;
		while (n > 1)
		{
			n--;
			int k = rng.Next(n + 1);
			var value = _heights[k];
			_heights[k] = _heights[n];
			_heights[n] = value;
		}

		// Redraw the rectangles
		QueueRedraw();
	}
	private void DeferredQueueRedraw()
	{
		QueueRedraw();
	}
	private void DeferredPlaySound()
	{
		_audioStreamPlayer.Play();
	}
}
