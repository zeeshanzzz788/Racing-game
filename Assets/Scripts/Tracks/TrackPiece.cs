using UnityEngine;

namespace VelocityRush.TrackSystem
{
    public enum TrackPieceType
    {
        Straight,
        LeftCurve,
        RightCurve,
        HillUp,
        HillDown,
        Jump,
        Chicane
    }

    /// <summary>
    /// Metadata attached to a modular road prefab. The entry/exit connector transforms are aligned
    /// by TrackManager, allowing a designer to build a track from reusable straight, turn, hill,
    /// jump, or chicane pieces without hand-positioning every next segment.
    /// </summary>
    [DisallowMultipleComponent]
    public class TrackPiece : MonoBehaviour
    {
        [Header("Connectors")]
        [SerializeField] private Transform entryAnchor;
        [SerializeField] private Transform exitAnchor;
        [SerializeField, Min(1f)] private float nominalLength = 60f;

        [Header("Generation")]
        [SerializeField] private TrackPieceType pieceType = TrackPieceType.Straight;
        [SerializeField, Range(0f, 1f)] private float minimumDifficulty;
        [SerializeField, Range(.01f, 20f)] private float selectionWeight = 1f;
        [Tooltip("Avoids back-to-back jumps/chicanes when TrackManager has an alternative.")]
        [SerializeField] private bool specialPiece;

        [Header("Optional runtime markers")]
        [SerializeField] private Transform[] aiWaypoints;
        [SerializeField] private Transform[] collectibleSlots;
        [SerializeField] private Transform[] powerUpSlots;
        [SerializeField] private Transform[] obstacleSlots;

        public Transform EntryAnchor => entryAnchor == null ? transform : entryAnchor;
        public Transform ExitAnchor => exitAnchor == null ? transform : exitAnchor;
        public float NominalLength => nominalLength;
        public TrackPieceType PieceType => pieceType;
        public float MinimumDifficulty => minimumDifficulty;
        public float SelectionWeight => selectionWeight;
        public bool IsSpecialPiece => specialPiece;
        public Transform[] AiWaypoints => aiWaypoints;
        public Transform[] CollectibleSlots => collectibleSlots;
        public Transform[] PowerUpSlots => powerUpSlots;
        public Transform[] ObstacleSlots => obstacleSlots;

        /// <summary>Invoked after a pooled piece is positioned and enabled.</summary>
        public void PrepareForSpawn()
        {
            gameObject.SetActive(true);
        }

        /// <summary>Invoked before a pooled piece is hidden. Override through companion scripts if needed.</summary>
        public void PrepareForRecycle()
        {
            gameObject.SetActive(false);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Transform entry = EntryAnchor;
            Gizmos.DrawSphere(entry.position, .7f);
            Gizmos.DrawRay(entry.position, entry.forward * 3f);
            Gizmos.color = Color.red;
            Transform exit = ExitAnchor;
            Gizmos.DrawSphere(exit.position, .7f);
            Gizmos.DrawRay(exit.position, exit.forward * 3f);
            Gizmos.color = Color.cyan;
            DrawMarkers(aiWaypoints);
            Gizmos.color = Color.yellow;
            DrawMarkers(collectibleSlots);
            Gizmos.color = new Color(1f, .3f, 1f);
            DrawMarkers(powerUpSlots);
            Gizmos.color = new Color(1f, .35f, .05f);
            DrawMarkers(obstacleSlots);
        }

        private static void DrawMarkers(Transform[] markers)
        {
            if (markers == null) return;
            for (int i = 0; i < markers.Length; i++)
                if (markers[i] != null) Gizmos.DrawWireSphere(markers[i].position, .35f);
        }
    }
}
