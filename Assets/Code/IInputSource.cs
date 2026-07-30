// Movement input behind an interface so the source can be keyboard/mouse now and phone
// GPS/motion later (Stage 4 WAYFINDER) without touching the player controller.
public interface IInputSource
{
    float Horizontal { get; } // strafe, -1..1
    float Vertical { get; }   // forward/back, -1..1
    float LookX { get; }      // yaw (turn) delta
    float LookY { get; }      // pitch (look up/down) delta
}
