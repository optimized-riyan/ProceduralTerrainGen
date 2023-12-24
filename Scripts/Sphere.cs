using System.Collections.Generic;
using Godot;


[Tool]
public partial class Sphere : MeshInstance3D {

    private int _rings = 50;
    private int _radialSegments = 50;
    private float _radius = 10;
	private List<Vector3> verts = new List<Vector3>();
	private List<Vector3> normals = new List<Vector3>();
	private List<Vector2> uvs = new List<Vector2>();
	private List<int> indices = new List<int>();

    public override void _Ready()
    {
        // Insert setting up the surface array and lists here.

        // Vertex indices.
        var thisRow = 0;
        var prevRow = 0;
        var point = 0;

        // Loop over rings.
        for (var i = 0; i < _rings + 1; i++)
        {
            var v = ((float)i) / _rings;
            var w = Mathf.Sin(Mathf.Pi * v);
            var y = Mathf.Cos(Mathf.Pi * v);

            // Loop over segments in ring.
            for (var j = 0; j < _radialSegments; j++)
            {
                var u = ((float)j) / _radialSegments;
                var x = Mathf.Sin(u * Mathf.Pi * 2);
                var z = Mathf.Cos(u * Mathf.Pi * 2);
                var vert = new Vector3(x * _radius * w, y * _radius, z * _radius * w);
                verts.Add(vert);
                normals.Add(vert.Normalized());
                uvs.Add(new Vector2(u, v));
                point += 1;

                // Create triangles in ring using indices.
                if (i > 0 && j > 0)
                {
                    indices.Add(prevRow + j - 1);
                    indices.Add(prevRow + j);
                    indices.Add(thisRow + j - 1);

                    indices.Add(prevRow + j);
                    indices.Add(thisRow + j);
                    indices.Add(thisRow + j - 1);
                }
            }

            if (i > 0)
            {
                indices.Add(prevRow + _radialSegments - 1);
                indices.Add(prevRow);
                indices.Add(thisRow + _radialSegments - 1);

                indices.Add(prevRow);
                indices.Add(prevRow + _radialSegments);
                indices.Add(thisRow + _radialSegments - 1);
            }

            prevRow = thisRow;
            thisRow = point;
        }

        // Insert committing to the ArrayMesh here.
		Godot.Collections.Array arrays = new Godot.Collections.Array();
		arrays.Resize((int)Mesh.ArrayType.Max);
		arrays[(int)Mesh.ArrayType.Vertex] = verts.ToArray();
		arrays[(int)Mesh.ArrayType.TexUV] = uvs.ToArray();
		arrays[(int)Mesh.ArrayType.Normal] = normals.ToArray();
		arrays[(int)Mesh.ArrayType.Index] = indices.ToArray();

		ArrayMesh arrayMesh = new ArrayMesh();
		arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
		
		this.Mesh = arrayMesh;
    }
}