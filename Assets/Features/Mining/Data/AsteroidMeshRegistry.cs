using UnityEngine;

namespace VoidHarvest.Features.Mining.Data
{
    /// <summary>
    /// Static registry of unique asteroid meshes, populated at field generation time.
    /// Used by MiningBeamView to set up a MeshCollider proxy for exact beam-to-mesh
    /// surface raycasting. Pre-bakes collision data for instant MeshCollider assignment.
    /// </summary>
    public static class AsteroidMeshRegistry
    {
        private static Mesh[] _meshes;

        public static void Register(Mesh[] meshes)
        {
            _meshes = meshes;
            foreach (var mesh in meshes)
            {
                if (mesh != null)
                    Physics.BakeMesh(mesh.GetInstanceID(), false);
            }
        }

        public static Mesh GetMesh(int index)
        {
            if (_meshes == null || index < 0 || index >= _meshes.Length)
                return null;
            return _meshes[index];
        }
    }
}
