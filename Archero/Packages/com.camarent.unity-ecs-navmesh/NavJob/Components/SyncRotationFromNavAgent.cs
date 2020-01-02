using Unity.Collections;
using Unity.Entities;

namespace NavJob.Components
{
    public struct SyncRotationFromNavAgent : IComponentData { }

    public class SyncRotationFromNavAgentComponent : ComponentDataProxy<SyncRotationFromNavAgent> { };
}