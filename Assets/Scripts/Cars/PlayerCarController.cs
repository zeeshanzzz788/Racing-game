using UnityEngine;

namespace VelocityRush.Cars
{
    /// <summary>
    /// Compatibility component used by the existing Velocity Rush spawn, race, UI and AI systems.
    /// All vehicle physics now lives in CarController; new standalone vehicle prefabs may use
    /// CarController directly, while project cars should keep this component until callers are
    /// migrated to the base type.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerCarController : CarController
    {
    }
}
