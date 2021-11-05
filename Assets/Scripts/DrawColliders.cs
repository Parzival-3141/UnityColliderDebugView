using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Draws Colliders in builds. Put on GameObject with Camera Component.
/// </summary>
#if UNITY_EDITOR
[ExecuteAlways]
#endif

[RequireComponent(typeof(Camera))]
public class DrawColliders : MonoBehaviour
{
    public GameObject SelectedObject 
    { 
        get => selectedObject;
        set => SelectObject(value);
    }

    public bool SelectChildren 
    { 
        get => selectChildren; 
        set
        {
            if(!selectedIsNull && value != selectChildren)
            {
                ColRenderer.FillColliderCache(selectedObject, value);
            }

            selectChildren  = value;
            lastSelectChild = value;
        } 
    }


    public bool drawColliders = true;
    public bool drawTriggers = false;

    private GameObject selectedObject = null;

    private bool selectChildren  = true;
    private bool selectedIsNull  = true;
    private bool lastSelectChild = true;

    private ColliderRenderer ColRenderer 
    { 
        get
        {
            if (cRender == null)
            {
                cRender = new ColliderRenderer();
                Debug.Log("creating ColliderRenderer");
            }

            return cRender;
        } 
    }

    private ColliderRenderer cRender;


    private void OnPreCull()
    {
        // Check if selectedObject has become null since selection
        if (!selectedIsNull && selectedObject == null) 
        {
            ColRenderer.ResetColliderCache();
            selectedIsNull = true;
        }

        ColRenderer.RenderColliders(drawColliders, drawTriggers);
    }

    public void SelectObject(GameObject toSelect)
    {
        if (toSelect != selectedObject)
        {
            if (toSelect != null)
                ColRenderer.FillColliderCache(toSelect, selectChildren);
            else
                ColRenderer.ResetColliderCache();

            selectedObject = toSelect;
            selectedIsNull = selectedObject == null;
        }
        //else
        //    Debug.Log("Object already selected! " + toSelect);
    }


    protected class ColliderRenderer
    {
        #region Fields
        private enum ColliderType
        {
            // Values determined by type order in FillAllColliders().
            // (e.g. allColliders[i] == capsuleColliders, ColliderType.Capsule == i, etc.)

            None = -1,
            Capsule = 0,
            Sphere = 1,
            Box = 2
        }

        // Useful reference :)
        private enum ZTest
        {
            Disabled = 0,
            Never = 1,
            Less = 2,
            Equal = 3,
            LEqual = 4,
            Greater = 5,
            NotEquat = 6,
            GEqual = 7,
            Always = 8,
        }


        private CapsuleCollider[] capsuleColliders;
        private SphereCollider[] sphereColliders;
        private BoxCollider[] boxColliders;

        private Collider[][] allColliders;

        // Tuple (Collider, ColliderType, Bool isTrigger)
        private List<(Collider collider, ColliderType type, bool isTrigger)> colliderData;
        //private List<Tuple<Collider, ColliderType, bool>> colliderData;


        private Material colliderMat;
        private Material triggerMat;

        private Color colliderColor;
        private Color triggerColor;

        private static Mesh capsule;
        private static Mesh sphere;
        private static Mesh box;
        
        /// <summary>
        /// Call in Awake or later
        /// </summary>
        public ColliderRenderer()
        {
            LoadDrawMeshes();
            ResetColliderCache();

            colliderColor = Color.HSVToRGB(0.32f, 0.44f, 1f); // light green
            triggerColor  = Color.HSVToRGB(0.82f, 0.44f, 1f); // pink-ish purple

            colliderMat = new Material(Shader.Find("Hidden/Internal-Colored"));
            colliderMat.SetColor("_Color", colliderColor); 
            colliderMat.SetInt("_ZTest", (int)ZTest.Always);

            triggerMat = new Material(colliderMat);
            triggerMat.SetColor("_Color", triggerColor); 
        }

        #endregion


        public void FillColliderCache(GameObject root, bool inChildren)
        {
            capsuleColliders = GetComponents<CapsuleCollider>(root, inChildren);
            sphereColliders  = GetComponents<SphereCollider >(root, inChildren);
            boxColliders     = GetComponents<BoxCollider    >(root, inChildren);

            FillColliderData();
        }

        public void ResetColliderCache()
        {
            capsuleColliders = new CapsuleCollider[0];
            sphereColliders  = new SphereCollider[0];
            boxColliders     = new BoxCollider[0];

            FillColliderData();
        }

        public void RenderColliders(bool drawColliders, bool drawTriggers)
        {
            //for (int i = 0; i < allColliders.Length; i++)
            //{
            //    for (int j = 0; j < allColliders[i].Length; j++)
            //    {
            //        var m = GetMesh((ColliderType)i);
            //        m.SetIndices(m.GetIndices(0), MeshTopology.Lines, 0);

            //        Graphics.DrawMesh(m, GetMatrix((ColliderType)i, allColliders[i][j]), mat, 0);
            //    }
            //}


            foreach (var col in colliderData)
            {
                if((!col.isTrigger && !drawColliders) || (col.isTrigger && !drawTriggers)) { continue; }

                var m = GetMesh(col.type);
                m.SetIndices(m.GetIndices(0), MeshTopology.Lines, 0);

                Graphics.DrawMesh(m, GetMatrix(col.type, col.collider), col.isTrigger ? triggerMat : colliderMat, 0);
            }

        }


        #region Helper Methods

        #region Mesh Stuff
        private void LoadDrawMeshes()
        {
            capsule = CloneMesh(Resources.GetBuiltinResource<Mesh>("Capsule.fbx"));
            sphere  = CloneMesh(Resources.GetBuiltinResource<Mesh>("Sphere.fbx"));
            box     = CloneMesh(Resources.GetBuiltinResource<Mesh>("Cube.fbx"));
        }

        private Mesh GetMesh(ColliderType colliderType)
        {
            Mesh m;
            switch (colliderType)
            {
                case ColliderType.Capsule:
                    m = capsule;
                    break;

                default: // Default to sphere bc it doesn't cast away from Collider type
                case ColliderType.Sphere:
                    m = sphere;
                    break;

                case ColliderType.Box:
                    m = box;
                    break;
            }

            return m;
        }

        private Mesh CloneMesh(Mesh original)
        {
            Mesh m = new Mesh();
            CloneMesh(original, ref m);
            return m;
        }

        private void CloneMesh(Mesh original, ref Mesh clone)
        {
            clone.name = original.name + "_clone";
            clone.vertices = original.vertices;

            if (original.GetTopology(0) == MeshTopology.Triangles)
                clone.triangles = original.GetTriangles(0);

            clone.normals = original.normals;
            clone.uv = original.uv;
        }
        #endregion

        private Matrix4x4 GetMatrix(ColliderType colliderType, Collider col)
        {
            Matrix4x4 matrix;
            switch (colliderType)
            {
                case ColliderType.Capsule:

                    var capsule = col as CapsuleCollider;

                    Vector3 up = Vector3.up;
                    Vector3 fwd = capsule.transform.forward;

                    switch (capsule.direction)
                    {
                        case 0:
                            up = capsule.transform.right;
                            break;

                        case 1:
                            up = capsule.transform.up;
                            break;

                        case 2:
                            up = capsule.transform.forward;
                            fwd = capsule.transform.right;
                            break;
                    }

                    //@Refactor: 
                    // Incorrectly scales when height <= radius * 2, and it becomes a sphere.
                    // Also incorrect when scaled along collider up axis, might need to use multiple meshes;
                    // Or do some on-the-spot mesh editing and only scale the cylinder bit of the mesh with height.
                    Vector3 cScale = new Vector3(capsule.radius, capsule.height / 4f, capsule.radius);

                    matrix = Matrix4x4.TRS(capsule.bounds.center, Quaternion.LookRotation(fwd, up), cScale);

                    break;


                default: // Default to sphere bc it doesn't cast away from Collider type
                case ColliderType.Sphere:
                    matrix = Matrix4x4.TRS(col.bounds.center, col.transform.rotation, col.bounds.size / 2f);
                    break;


                case ColliderType.Box:

                    var box = col as BoxCollider;
                    var bScale = Vector3.Scale(box.size, box.transform.lossyScale);

                    matrix = Matrix4x4.TRS(box.bounds.center, box.transform.rotation, bScale);
                    break;
            }

            return matrix;
        }

        private T[] GetComponents<T>(GameObject gameObject, bool inChildren) where T : Component
        {
            if (inChildren)
                return gameObject.GetComponentsInChildren<T>();
            else
                return gameObject.GetComponents<T>();
        }

        private void FillColliderData()
        {
            allColliders = new Collider[][] { capsuleColliders, sphereColliders, boxColliders };

            colliderData = new List<(Collider collider, ColliderType type, bool isTrigger)>();

            for (int i = 0; i < allColliders.Length; i++)
            {
                for (int j = 0; j < allColliders[i].Length; j++)
                {
                    var col = allColliders[i][j];

                    colliderData.Add((col, (ColliderType)i, col.isTrigger));
                }
            }
        }

        //public void SetColors(Color collider, Color trigger)
        //{
        //    colliderMat.SetColor("_Color", collider);
        //    triggerMat.SetColor( "_Color", trigger );
        //}

        #endregion
    }
}
