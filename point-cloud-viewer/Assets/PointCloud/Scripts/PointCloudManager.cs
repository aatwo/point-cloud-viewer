using UnityEngine;
using System.Collections;
using System.IO;

public class PointCloudManager : MonoBehaviour {

	// File
	public string filepath;
	private string filename;
	public Material matVertex;

	// GUI
	private float progress = 0;
	private string guiText;
	private bool loaded = false;

	// PointCloud
	private GameObject pointCloud;

	public float scale = 1f;

	public int numPoints;
	public int numPointGroups;
	private int limitPoints = 65000;

	private Vector3[] points;
	private Color[] colors;
	private Vector3 minValue;

	
	void Start ()
    {
        // Create resource folder
        if( !Directory.Exists( Application.dataPath + "/Resources/" ) )
            UnityEditor.AssetDatabase.CreateFolder( "Assets", "Resources" );

        // Create meshes folder
        if( !Directory.Exists( Application.dataPath + "/Resources/PointCloudMeshes/" ) )
            UnityEditor.AssetDatabase.CreateFolder( "Assets/Resources", "PointCloudMeshes" );

        // Get Filename
        filename = Path.GetFileName(filepath);

        // Check if the PointCloud was loaded previously
        
        if( !Directory.Exists( Application.dataPath + "/Resources/PointCloudMeshes/" + filename ) )
        {
            UnityEditor.AssetDatabase.CreateFolder( "Assets/Resources/PointCloudMeshes", filename );
        }

        /*
        else
        {
            loadStoredMeshes();
        }
        */

        LoadPointCloud();
    }

    void OnGUI()
    {
        if( !loaded )
        {
            GUI.BeginGroup( new Rect( Screen.width / 2 - 100, Screen.height / 2, 400.0f, 20 ) );
            GUI.Box( new Rect( 0, 0, 200.0f, 20.0f ), guiText );
            GUI.Box( new Rect( 0, 0, progress * 200.0f, 20 ), "" );
            GUI.EndGroup();
        }
    }

    void LoadPointCloud()
    {
        if( !File.Exists( filepath ) )
            Debug.LogError( "File does not exist (" + filepath + ")" );

		StartCoroutine( "LoadTxt" ); 		
	}
	
	// Start Coroutine of reading the points from the OFF file and creating the meshes
	IEnumerator LoadTxt()
    {
        // Read file
        StreamReader sr = new StreamReader (filepath);

        string numPointsString = sr.ReadLine();
        int numPoints = 0;
        if(!int.TryParse(numPointsString, out numPoints))
        {
            Debug.LogError( "Unable to parse point count in file. First line should be the point count only." );
        }
        
        // TEMP
        //numPoints = 1000000;
        // END TEMP

		points = new Vector3[numPoints];
		colors = new Color[numPoints];
		minValue = new Vector3();
		
		for( int i = 0; i < numPoints; i++)
        {
			string[] buffer = sr.ReadLine ().Split (',');

            // Position indexes: 0, 1, 2
            // RGB indexes: 3, 4, 5

            double x = double.Parse(buffer[0]);
            double y = double.Parse(buffer[1]);
            double z = double.Parse(buffer[2]);

            if( x > (double)float.MaxValue || y > (double)float.MaxValue || z > (double)float.MaxValue )
                Debug.LogWarning("Values in file exceed floating point maximum and so result may be broken.");

            points[i] = new Vector3( (float)x * 0.0001f, (float)z * 0.0001f, (float)y * 0.0001f );
			
			if (buffer.Length >= 5)
                colors[i] = new Color( float.Parse( buffer[3] ), float.Parse( buffer[4] ), float.Parse( buffer[5] ) );
                //colors[i] = new Color (int.Parse (buffer[3])/255.0f,int.Parse (buffer[4])/255.0f,int.Parse (buffer[5])/255.0f);
			else
				colors[i] = Color.cyan;

			// Relocate Points near the origin
			//calculateMin(points[i]);

			// GUI
			progress = i *1.0f/(numPoints-1)*1.0f;
			if (i%Mathf.FloorToInt(numPoints/20) == 0){
				guiText=i.ToString() + " out of " + numPoints.ToString() + " loaded";
				yield return null;
			}
		}

		
		// Instantiate Point Groups
		numPointGroups = Mathf.CeilToInt (numPoints*1.0f / limitPoints*1.0f);

		pointCloud = new GameObject (filename);

		for (int i = 0; i < numPointGroups-1; i ++) {
			InstantiateMesh (i, limitPoints);
			if (i%10==0){
				guiText = i.ToString() + " out of " + numPointGroups.ToString() + " PointGroups loaded";
				yield return null;
			}
		}
		InstantiateMesh (numPointGroups-1, numPoints- (numPointGroups-1) * limitPoints);

		//Store PointCloud
		UnityEditor.PrefabUtility.CreatePrefab ("Assets/Resources/PointCloudMeshes/" + filename + ".prefab", pointCloud);

		loaded = true;
	}

    // Load stored PointCloud
    void loadStoredMeshes()
    {
        Debug.Log( "Using previously loaded PointCloud: " + filename );

        GameObject pointGroup = Instantiate(Resources.Load ("PointCloudMeshes/" + filename)) as GameObject;

        loaded = true;
    }

    void InstantiateMesh(int meshInd, int nPoints)
    {
		// Create Mesh
		GameObject pointGroup = new GameObject (filename + meshInd);
		pointGroup.AddComponent<MeshFilter> ();
		pointGroup.AddComponent<MeshRenderer> ();
		pointGroup.GetComponent<Renderer>().material = matVertex;

		pointGroup.GetComponent<MeshFilter> ().mesh = CreateMesh (meshInd, nPoints, limitPoints);
		pointGroup.transform.parent = pointCloud.transform;


		// Store Mesh
		UnityEditor.AssetDatabase.CreateAsset(pointGroup.GetComponent<MeshFilter> ().mesh, "Assets/Resources/PointCloudMeshes/" + filename + @"/" + filename + meshInd + ".asset");
		UnityEditor.AssetDatabase.SaveAssets ();
		UnityEditor.AssetDatabase.Refresh();
	}

	Mesh CreateMesh(int id, int nPoints, int limitPoints)
    {
		
		Mesh mesh = new Mesh ();
		
		Vector3[] myPoints = new Vector3[nPoints]; 
		int[] indecies = new int[nPoints];
		Color[] myColors = new Color[nPoints];

		for(int i=0;i<nPoints;++i) {
			myPoints[i] = points[id*limitPoints + i] - minValue;
			indecies[i] = i;
			myColors[i] = colors[id*limitPoints + i];
		}


		mesh.vertices = myPoints;
		mesh.colors = myColors;
		mesh.SetIndices(indecies, MeshTopology.Points,0);
		mesh.uv = new Vector2[nPoints];
		mesh.normals = new Vector3[nPoints];


		return mesh;
	}

	void calculateMin(Vector3 point){
		if (minValue.magnitude == 0)
			minValue = point;


		if (point.x < minValue.x)
			minValue.x = point.x;
		if (point.y < minValue.y)
			minValue.y = point.y;
		if (point.z < minValue.z)
			minValue.z = point.z;
	}
}
