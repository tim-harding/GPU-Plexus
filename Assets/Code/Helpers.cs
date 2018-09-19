namespace Grid
{
    using UnityEngine;

    public static class GridExtension
    {
        public static int GridCellCount(this Vector3Int grid)
        {
            return grid.x * grid.y * grid.z;
        }
    }
}