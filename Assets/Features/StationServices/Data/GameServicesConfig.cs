using UnityEngine;

namespace VoidHarvest.Features.StationServices.Data
{
    /// <summary>
    /// Global game services configuration. Starting credits, etc.
    /// See Spec 006: Station Services.
    /// </summary>
    [CreateAssetMenu(menuName = "VoidHarvest/Game Services Config")]
    public class GameServicesConfig : ScriptableObject
    {
        [Tooltip("Credits new players start with.")]
        public int StartingCredits = 0;
    }
}
