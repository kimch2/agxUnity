﻿using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace AgXUnity.Utils
{
  public class Raycast
  {
    public class TriangleHit
    {
      public static TriangleHit Invalid = new TriangleHit() { Target = null, Vertices = new Vector3[ 0 ], Distance = float.PositiveInfinity };

      public bool Valid { get { return this != Invalid; } }

      public GameObject Target { get; set; }

      public Vector3[] Vertices { get; set; }

      public MeshUtils.Edge[] Edges
      {
        get
        {
          return new MeshUtils.Edge[]
          {
            new MeshUtils.Edge( Vertices[ 0 ], Vertices[ 1 ], Normal ),
            new MeshUtils.Edge( Vertices[ 1 ], Vertices[ 2 ], Normal ),
            new MeshUtils.Edge( Vertices[ 2 ], Vertices[ 0 ], Normal )
          };
        }
      }

      public MeshUtils.Edge ClosestEdge { get; set; }

      public Vector3 Point { get; set; }

      public Vector3 Normal { get; set; }

      public float Distance { get; set; }
    }

    public class ClosestEdgeHit
    {
      public static ClosestEdgeHit Invalid = new ClosestEdgeHit() { Target = null, Edge = null };

      public GameObject Target { get; set; }

      public MeshUtils.Edge Edge { get; set; }
    }

    public class Hit
    {
      public static Hit Invalid = new Hit() { Triangle = TriangleHit.Invalid, ClosestEdge = ClosestEdgeHit.Invalid };

      public bool Valid { get { return this != Invalid; } }

      public TriangleHit Triangle { get; set; }

      public ClosestEdgeHit ClosestEdge { get; set; }

      public Hit()
      {
        Triangle    = new TriangleHit();
        ClosestEdge = new ClosestEdgeHit();
      }
    }

    public GameObject Target { get; set; }

    public Hit LastHit { get; private set; }

    public Hit Test( Ray ray, float rayLength = 500.0f )
    {
      LastHit = Hit.Invalid;

      if ( Target == null )
        return Hit.Invalid;

      Hit hit = new Hit();

      Collide.Shape shape = Target.GetComponent<Collide.Shape>();
      if ( shape != null ) {
        if ( shape is Collide.Mesh )
          hit.Triangle = MeshUtils.FindClosestTriangle( ( shape as Collide.Mesh ).SourceObject, shape.gameObject, ray, rayLength );
        else if ( shape is Collide.HeightField )
          hit.Triangle = TriangleHit.Invalid;
        else {
          GameObject tmp = PrefabLoader.Instantiate( Rendering.DebugRenderData.GetPrefabName( shape.GetType().Name ) );

          if ( tmp != null ) {
            tmp.hideFlags            = HideFlags.HideAndDontSave;
            tmp.transform.position   = shape.transform.position;
            tmp.transform.rotation   = shape.transform.rotation;
            tmp.transform.localScale = shape.GetScale();

            hit.Triangle        = MeshUtils.FindClosestTriangle( tmp, ray, rayLength );
            hit.Triangle.Target = shape.gameObject;

            GameObject.DestroyImmediate( tmp );
          }
        }
      }
      else {
        MeshFilter filter = Target.GetComponent<MeshFilter>();
        hit.Triangle = filter != null ? MeshUtils.FindClosestTriangle( filter.sharedMesh, Target, ray, rayLength ) : TriangleHit.Invalid;
      }

      if ( hit.Triangle.Valid )
        hit.Triangle.ClosestEdge = ShapeUtils.FindClosestEdgeToSegment( ray.GetPoint( 0 ), ray.GetPoint( rayLength ), hit.Triangle.Edges );

      List<MeshUtils.Edge> allEdges = FindPrincipalEdges( shape, 10.0f ).ToList();
      if ( hit.Triangle.Valid )
        allEdges.Add( hit.Triangle.ClosestEdge );

      hit.ClosestEdge.Target = Target;
      hit.ClosestEdge.Edge   = ShapeUtils.FindClosestEdgeToSegment( ray.GetPoint( 0 ), ray.GetPoint( rayLength ), allEdges.ToArray() );

      return ( LastHit = hit );
    }

    public static Hit Test( GameObject target, Ray ray, float rayLength = 500.0f )
    {
      return ( new Raycast() { Target = target } ).Test( ray, rayLength );
    }

    private MeshUtils.Edge[] FindPrincipalEdges( Collide.Shape shape, float principalEdgeExtension )
    {
      if ( shape != null && shape.GetUtils() != null )
        return shape.GetUtils().GetPrincipalEdgesWorld( principalEdgeExtension );

      Mesh mesh = shape is Collide.Mesh ?
                    ( shape as Collide.Mesh ).SourceObject :
                  Target.GetComponent<MeshFilter>() != null ?
                    Target.GetComponent<MeshFilter>().sharedMesh :
                  null;

      Vector3 halfExtents = 0.5f * Vector3.one;
      if ( mesh != null )
        halfExtents = mesh.bounds.extents;

      MeshUtils.Edge[] edges = ShapeUtils.ExtendAndTransformEdgesToWorld( Target.transform,
                                new MeshUtils.Edge[]
                                {
                                  new MeshUtils.Edge( BoxShapeUtils.GetLocalFace( halfExtents, ShapeUtils.Direction.Negative_X ),
                                                      BoxShapeUtils.GetLocalFace( halfExtents, ShapeUtils.Direction.Positive_X ),
                                                      ShapeUtils.GetLocalFaceDirection( ShapeUtils.Direction.Positive_Y ),
                                                      MeshUtils.Edge.EdgeType.Principal ),
                                  new MeshUtils.Edge( BoxShapeUtils.GetLocalFace( halfExtents, ShapeUtils.Direction.Negative_Y ),
                                                      BoxShapeUtils.GetLocalFace( halfExtents, ShapeUtils.Direction.Positive_Y ),
                                                      ShapeUtils.GetLocalFaceDirection( ShapeUtils.Direction.Positive_Z ),
                                                      MeshUtils.Edge.EdgeType.Principal ),
                                  new MeshUtils.Edge( BoxShapeUtils.GetLocalFace( halfExtents, ShapeUtils.Direction.Negative_Z ),
                                                      BoxShapeUtils.GetLocalFace( halfExtents, ShapeUtils.Direction.Positive_Z ),
                                                      ShapeUtils.GetLocalFaceDirection( ShapeUtils.Direction.Positive_X ),
                                                      MeshUtils.Edge.EdgeType.Principal )
                                },
                                principalEdgeExtension );

      return edges;
    }
  }
}