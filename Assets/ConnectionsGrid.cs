namespace Grid
{
    using UnityEngine;
    using UnityEngine.Assertions;
    using System;

    public static class GridExtension
    {
        public static int GridCellCount(this Vector3Int grid)
        {
            return grid.x * grid.y * grid.z;
        }
    }

    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class ConnectionsGrid : MonoBehaviour
    {
        #region Members

        private struct Constants
        {
            public static readonly string kernelName = "Main";
            public static readonly int time = Shader.PropertyToID("_Time");
            public static readonly int speed = Shader.PropertyToID("_Speed");
            public static readonly int maxOffset = Shader.PropertyToID("_MaxOffset");
            public static readonly int converge = Shader.PropertyToID("_Converge");
            public static readonly int convergeRadius = Shader.PropertyToID("_ConvergeRadius");
            public static readonly int convergeStrength = Shader.PropertyToID("_ConvergeStrength");
            public static readonly int convergeSpeed = Shader.PropertyToID("_ConvergeSpeed");
            public static readonly int positions = Shader.PropertyToID("_Positions");
            public static readonly int preOffset = Shader.PropertyToID("_PreOffset");
            public static readonly int dimensions = Shader.PropertyToID("_Dimensions");
        }

        [Serializable]
        public struct GridMoverShaderParameters
        {
            public float convergeRadius;
            public float convergeStrength;
            public float convergeSpeed;
            public float speed;
            public float maxOffset;
        }

        public Vector3Int dimensions = Vector3Int.zero;
        public ComputeShader compute = null;
        public Material material = null;
        public Transform converge = null;
        public GridMoverShaderParameters parameters = new GridMoverShaderParameters();

        private ComputeBuffer positionsBuffer;
        private ComputeBuffer preOffsetBuffer;
        private int kernel;
        private int[] groups;

        #endregion

        #region Initialization

        private void Start()
        {
            PrepareCompute();
            CreateMesh();
            StoreThreadGroupCount();
        }
        
        private void InitializeBuffers()
        {
            int count = dimensions.GridCellCount();
            positionsBuffer = new ComputeBuffer(count, 3 * sizeof(float));
            compute.SetBuffer(kernel, Constants.positions, positionsBuffer);
            preOffsetBuffer = new ComputeBuffer(count, 3 * sizeof(float));
            compute.SetBuffer(kernel, Constants.preOffset, preOffsetBuffer);
        }

        private void SetShaderConstants()
        {
            compute.SetInts(Constants.dimensions, dimensions.x, dimensions.y, dimensions.z);
            material.SetBuffer(Constants.positions, positionsBuffer);
            material.SetVector(Constants.dimensions, new Vector4(dimensions.x, dimensions.y, dimensions.z, 0));
        }

        private void CreateMesh()
        {
            int count = dimensions.GridCellCount();
            var vertices = new Vector3[count];
            var indices = new int[count];
            PopulateMeshData(vertices, indices);
            StoreMeshData(vertices, indices);
        }

        private void PopulateMeshData(Vector3[] vertices, int[] indices)
        {
            for (int x = 0; x < dimensions.x; x++)
            {
                for (int y = 0; y < dimensions.y; y++)
                {
                    for (int z = 0; z < dimensions.z; z++)
                    {
                        int index = x * dimensions.z * dimensions.y + y * dimensions.z + z;
                        vertices[index] = new Vector3(x, y, z);
                        indices[index] = index;
                    }
                }
            }
        }

        private void StoreMeshData(Vector3[] vertices, int[] indices)
        {
            var mesh = new Mesh();
            mesh.name = "Grid";
            mesh.vertices = vertices;
            mesh.SetIndices(indices, MeshTopology.Points, submesh: 0);
            Bounds bounds = mesh.bounds;
            bounds.size *= 2f;
            mesh.bounds = bounds;
            var filter = GetComponent<MeshFilter>();
            filter.mesh = mesh;
        }

        private void PrepareCompute()
        {
            kernel = compute.FindKernel(Constants.kernelName);
            InitializeBuffers();
            SetShaderConstants();
        }

        private void CheckForValidDimensions(int[] sizes)
        {
            var remainders = new Vector3Int(
                dimensions.x % sizes[0], 
                dimensions.y % sizes[1], 
                dimensions.z % sizes[2]
            );
            bool isValid = remainders.x == 0 && remainders.y == 0 && remainders.z == 0;
            Assert.IsTrue(isValid, "Dimensions must be divisible by the compute shader thread group size.");
        }

        private void StoreThreadGroupCount()
        {
            var uSizes = new uint[3];
            compute.GetKernelThreadGroupSizes(kernel, out uSizes[0], out uSizes[1], out uSizes[2]);
            int[] sizes = Array.ConvertAll(uSizes, size => (int)size);
            CheckForValidDimensions(sizes);
            groups = new int[]
            {
                dimensions.x / sizes[0],
                dimensions.y / sizes[1],
                dimensions.z / sizes[2]
            };
        }

        #endregion

        #region Lifetime

        private void Update()
        {
            UpdateComputeParameters();
            material.SetVector(Constants.converge, converge.localPosition);
            compute.Dispatch(kernel, groups[0], groups[1], groups[2]);
        }

        private void UpdateComputeParameters()
        {
            compute.SetFloat(Constants.time, Time.time);
            compute.SetFloat(Constants.speed, parameters.speed);
            compute.SetFloat(Constants.maxOffset, parameters.maxOffset);
            compute.SetFloat(Constants.convergeRadius, parameters.convergeRadius);
            compute.SetFloat(Constants.convergeSpeed, parameters.convergeSpeed);
            compute.SetFloat(Constants.convergeStrength, parameters.convergeStrength);
            compute.SetVector(Constants.converge, converge.localPosition);
        }

        private void OnDestroy()
        {
            positionsBuffer.Dispose();
            preOffsetBuffer.Dispose();
        }

        #endregion
    }
}