namespace CameraPro.Recording.StateMachine;

public enum RecordingState
{
    Idle,
    Preparing,
    Recording,
    Paused,
    Encoding,
    Complete
}

public enum RecordingEvent
{
    Start,
    Pause,
    Resume,
    Stop,
    EncodeComplete,
    Error
}

public class RecordingStateMachine
{
    private RecordingState _currentState = RecordingState.Idle;
    public RecordingState CurrentState => _currentState;

    public event EventHandler<RecordingState>? StateChanged;

    private readonly Dictionary<(RecordingState, RecordingEvent), RecordingState> _transitions = new()
    {
        { (RecordingState.Idle, RecordingEvent.Start), RecordingState.Preparing },
        { (RecordingState.Preparing, RecordingEvent.Start), RecordingState.Recording },
        { (RecordingState.Recording, RecordingEvent.Pause), RecordingState.Paused },
        { (RecordingState.Recording, RecordingEvent.Stop), RecordingState.Encoding },
        { (RecordingState.Paused, RecordingEvent.Resume), RecordingState.Recording },
        { (RecordingState.Paused, RecordingEvent.Stop), RecordingState.Encoding },
        { (RecordingState.Encoding, RecordingEvent.EncodeComplete), RecordingState.Complete },
        { (RecordingState.Complete, RecordingEvent.Start), RecordingState.Idle },
    };

    public bool CanTransition(RecordingEvent recordingEvent)
    {
        return _transitions.ContainsKey((_currentState, recordingEvent));
    }

    public bool Transition(RecordingEvent recordingEvent)
    {
        var key = (_currentState, recordingEvent);
        if (!_transitions.TryGetValue(key, out var nextState))
            return false;

        _currentState = nextState;
        StateChanged?.Invoke(this, _currentState);
        return true;
    }

    public void Reset()
    {
        _currentState = RecordingState.Idle;
        StateChanged?.Invoke(this, _currentState);
    }

    public bool IsRecording => _currentState == RecordingState.Recording;
    public bool IsPaused => _currentState == RecordingState.Paused;
    public bool IsEncoding => _currentState == RecordingState.Encoding;
    public bool IsIdle => _currentState == RecordingState.Idle;
}