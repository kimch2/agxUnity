﻿using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using AgXUnity.Utils;

namespace AgXUnity
{
  /// <summary>
  /// Supported default constraint types.
  /// </summary>
  public enum ConstraintType
  {
    Hinge,
    Prismatic,
    LockJoint,
    CylindricalJoint,
    BallJoint,
    DistanceJoint,
    AngularLockJoint,
    PlaneJoint
  }

  [AddComponentMenu( "" )]
  [CustomTool( "AgXUnityEditor.Tools.ConstraintTool" )]
  public class Constraint : ScriptComponent
  {
    /// <summary>
    /// Create a new constraint component given constraint type.
    /// </summary>
    /// <param name="type">Type of constraint.</param>
    /// <returns>Constraint component, added to a new game object - null if unsuccessful.</returns>
    public static Constraint Create( ConstraintType type )
    {
      GameObject constraintGameObject = new GameObject( Factory.CreateName( "AgXUnity." + type ) );
      try {
        Constraint constraint = constraintGameObject.AddComponent<Constraint>();
        constraint.Type       = type;
        var attachmentPair    = constraint.AttachmentPair; // This will instantiate the attachment pair.

        // Creating a temporary native instance of the constraint, including a rigid body and frames.
        // Given this native instance we copy the default configuration.
        using ( agx.RigidBody tmpRb = new agx.RigidBody() )
        using ( agx.Frame tmpF1 = new agx.Frame() )
        using ( agx.Frame tmpF2 = new agx.Frame() )
        using ( agx.Constraint tmpConstraint = (agx.Constraint)Activator.CreateInstance( constraint.NativeType, new object[] { tmpRb, tmpF1, null, tmpF2 } ) ) {
          for ( ulong i = 0; i < tmpConstraint.getNumElementaryConstraints(); ++i ) {
            ElementaryConstraint ec = ElementaryConstraint.Create( tmpConstraint.getElementaryConstraint( i ) );
            if ( ec == null )
              throw new Exception( "Failed to configure elementary constraint with name: " + tmpConstraint.getElementaryConstraint( i ).getName() + "." );

            constraint.m_elementaryConstraints.Add( ec );
          }

          for ( ulong i = 0; i < tmpConstraint.getNumSecondaryConstraints(); ++i ) {
            ElementaryConstraint sc = ElementaryConstraint.Create( tmpConstraint.getSecondaryConstraint( i ) );
            if ( sc == null )
              throw new Exception( "Failed to configure elementary controller constraint with name: " + tmpConstraint.getElementaryConstraint( i ).getName() + "." );

            constraint.m_elementaryConstraints.Add( sc );
          }
        }

        return constraint;
      }
      catch ( System.Exception e ) {
        Debug.LogException( e );
        DestroyImmediate( constraintGameObject );
        return null;
      }
    }

    /// <summary>
    /// Attachment pair of this constraint, holding parent objects and transforms.
    /// Paired with property AttachmentPair.
    /// </summary>
    [SerializeField]
    private ConstraintAttachmentPair m_attachmentPair = null;

    /// <summary>
    /// Attachment pair of this constraint, holding parent objects and transforms.
    /// </summary>
    [HideInInspector]
    public ConstraintAttachmentPair AttachmentPair
    {
      get
      {
        if ( m_attachmentPair == null )
          m_attachmentPair = ConstraintAttachmentPair.Create<ConstraintAttachmentPair>();
        return m_attachmentPair;
      }
    }

    /// <summary>
    /// Type of this constraint. Paired with property Type.
    /// </summary>
    [SerializeField]
    private ConstraintType m_type = ConstraintType.Hinge;

    /// <summary>
    /// Type of this constraint.
    /// </summary>
    [HideInInspector]
    public ConstraintType Type
    {
      get { return m_type; }
      private set
      {
        m_type = value;
      }
    }

    /// <summary>
    /// Collision state when the simulation is running.
    /// </summary>
    public enum ECollisionsState
    {
      /// <summary>
      /// Do nothing - preserves the current external state.
      /// </summary>
      KeepExternalState,
      /// <summary>
      /// Disables selected Reference object against selected Connected.
      /// </summary>
      DisableReferenceVsConnected,
      /// <summary>
      /// Disables the rigid bodies. If the second object hasn't got a
      /// rigid body - all child shapes in Connected will be disabled
      /// against the first rigid body.
      /// </summary>
      DisableRigidBody1VsRigidBody2
    }

    /// <summary>
    /// Collisions state when the simulation is running.
    /// </summary>
    [SerializeField]
    private ECollisionsState m_collisionsState = ECollisionsState.KeepExternalState;

    /// <summary>
    /// Collisions state when the simulation is running.
    /// </summary>
    [HideInInspector]
    public ECollisionsState CollisionsState
    {
      get { return m_collisionsState; }
      set { m_collisionsState = value; }
    }

    /// <summary>
    /// Type of the native instance constructed from agxDotNet.dll and current ConstraintType.
    /// </summary>
    public Type NativeType { get { return System.Type.GetType( "agx." + m_type + ", agxDotNet" ); } }

    /// <summary>
    /// Native instance if this constraint is initialized - otherwise null.
    /// </summary>
    public agx.Constraint Native { get; private set; }

    /// <summary>
    /// List of elementary constraints in this constraint - controllers and ordinary.
    /// </summary>
    [SerializeField]
    private List<ElementaryConstraint> m_elementaryConstraints = new List<ElementaryConstraint>();

    /// <summary>
    /// Array of elementary constraints in this constraint - controllers and ordinary.
    /// </summary>
    [HideInInspector]
    public ElementaryConstraint[] ElementaryConstraints { get { return m_elementaryConstraints.ToArray(); } }

    /// <summary>
    /// Finds and returns an array of ordinary ElementaryConstraint objects, i.e., the ones
    /// that aren't controllers.
    /// </summary>
    /// <returns>Array of ordinary elementary constraints.</returns>
    public ElementaryConstraint[] GetOrdinaryElementaryConstraints()
    {
      return ( from ec in m_elementaryConstraints where ( ec as ElementaryConstraintController ) == null select ec ).ToArray();
    }

    /// <summary>
    /// Finds and returns an array of controller elementary constraints, such as motor, lock, range etc.
    /// </summary>
    /// <returns>Array of controllers - if present.</returns>
    public ElementaryConstraintController[] GetElementaryConstraintControllers()
    {
      return ( from ec in m_elementaryConstraints where ec is ElementaryConstraintController select ec as ElementaryConstraintController ).ToArray();
    }

    /// <summary>
    /// Creates native instance and adds it to the simulation if this constraint
    /// is properly configured.
    /// </summary>
    /// <returns>True if successful.</returns>
    protected override bool Initialize()
    {
      if ( AttachmentPair.ReferenceObject == null ) {
        Debug.LogError( "Unable to initialize constraint. Reference object must be valid and contain a rigid body component.", this );
        return false;
      }

      // Synchronize frames to make sure connected frame is up to date.
      AttachmentPair.Update();

      RigidBody rb1 = m_attachmentPair.ReferenceObject.GetInitializedComponentInParent<RigidBody>();
      if ( rb1 == null ) {
        Debug.LogError( "Unable to initialize constraint. Reference object must contain a rigid body component.", m_attachmentPair.ReferenceObject );
        return false;
      }

      // Native constraint frames.
      agx.Frame f1 = new agx.Frame();
      agx.Frame f2 = new agx.Frame();

      // Note that the native constraint want 'f1' given in rigid body frame, and that
      // 'ReferenceFrame' may be relative to any object in the children of the body.
      f1.setLocalTranslate( m_attachmentPair.ReferenceFrame.CalculateLocalPosition( rb1.gameObject ).ToHandedVec3() );
      f1.setLocalRotate( m_attachmentPair.ReferenceFrame.CalculateLocalRotation( rb1.gameObject ).ToHandedQuat() );

      RigidBody rb2 = m_attachmentPair.ConnectedObject != null ? m_attachmentPair.ConnectedObject.GetInitializedComponentInParent<RigidBody>() : null;
      if ( rb2 != null ) {
        // Note that the native constraint want 'f2' given in rigid body frame, and that
        // 'ReferenceFrame' may be relative to any object in the children of the body.
        f2.setLocalTranslate( m_attachmentPair.ConnectedFrame.CalculateLocalPosition( rb2.gameObject ).ToHandedVec3() );
        f2.setLocalRotate( m_attachmentPair.ConnectedFrame.CalculateLocalRotation( rb2.gameObject ).ToHandedQuat() );
      }
      else {
        f2.setLocalTranslate( m_attachmentPair.ConnectedFrame.Position.ToHandedVec3() );
        f2.setLocalRotate( m_attachmentPair.ConnectedFrame.Rotation.ToHandedQuat() );
      }

      try {
        Native = (agx.Constraint)Activator.CreateInstance( NativeType, new object[] { rb1.Native, f1, ( rb2 != null ? rb2.Native : null ), f2 } );

        // Assigning native elementary constraints to our elementary constraint instances.
        foreach ( ElementaryConstraint ec in ElementaryConstraints )
          if ( !ec.OnConstraintInitialize( this ) )
            throw new Exception( "Unable to initialize elementary constraint: " + ec.NativeName + " (not present in native constraint)." );

        bool added = GetSimulation().add( Native );

        // Not possible to handle collisions if connected frame parent is null/world.
        if ( CollisionsState != ECollisionsState.KeepExternalState && m_attachmentPair.ConnectedObject != null ) {
          string groupName          = gameObject.name + gameObject.GetInstanceID().ToString();
          GameObject go1            = null;
          GameObject go2            = null;
          bool propagateToChildren1 = false;
          bool propagateToChildren2 = false;
          if ( CollisionsState == ECollisionsState.DisableReferenceVsConnected ) {
            go1 = m_attachmentPair.ReferenceObject;
            go2 = m_attachmentPair.ConnectedObject;
          }
          else {
            go1                  = rb1.gameObject;
            propagateToChildren1 = true;
            go2                  = rb2 != null ? rb2.gameObject : m_attachmentPair.ConnectedObject;
            propagateToChildren2 = true;
          }

          go1.GetOrCreateComponent<CollisionGroups>().GetInitialized<CollisionGroups>().AddGroup( groupName, propagateToChildren1 );
          go2.GetOrCreateComponent<CollisionGroups>().GetInitialized<CollisionGroups>().AddGroup( groupName, propagateToChildren2 );
          CollisionGroupsManager.Instance.GetInitialized<CollisionGroupsManager>().SetEnablePair( groupName, groupName, false );
        }

        return added && Native.getValid();
      }
      catch ( System.Exception e ) {
        Debug.LogException( e, gameObject );
        return false;
      }
    }

    protected override void OnDestroy()
    {
      if ( GetSimulation() != null )
        GetSimulation().remove( Native );

      Native = null;

      base.OnDestroy();
    }

    private static Mesh m_gizmosMesh = null;
    private static Mesh GetOrCreateGizmosMesh()
    {
      if ( m_gizmosMesh != null )
        return m_gizmosMesh;

      GameObject tmp = PrefabLoader.Instantiate<GameObject>( @"Debug/ConstraintRenderer" );
      MeshFilter[] filters = tmp.GetComponentsInChildren<MeshFilter>();
      CombineInstance[] combine = new CombineInstance[ filters.Length ];

      for ( int i = 0; i < filters.Length; ++i ) {
        combine[ i ].mesh = filters[ i ].sharedMesh;
        combine[ i ].transform = filters[ i ].transform.localToWorldMatrix;
      }

      m_gizmosMesh = new Mesh();
      m_gizmosMesh.CombineMeshes( combine );

      GameObject.DestroyImmediate( tmp );

      return m_gizmosMesh;
    }

    private static void DrawGizmos( Color color, ConstraintAttachmentPair attachmentPair )
    {
      Gizmos.color = color;
      Gizmos.DrawMesh( GetOrCreateGizmosMesh(), attachmentPair.ReferenceFrame.Position, attachmentPair.ReferenceFrame.Rotation * Quaternion.FromToRotation( Vector3.up, Vector3.forward ), 0.3f * Vector3.one );

      if ( !attachmentPair.Synchronized ) {
        Gizmos.color = Color.red;
        Gizmos.DrawLine( attachmentPair.ReferenceFrame.Position, attachmentPair.ConnectedFrame.Position );
        Gizmos.DrawMesh( GetOrCreateGizmosMesh(), attachmentPair.ConnectedFrame.Position, attachmentPair.ConnectedFrame.Rotation * Quaternion.FromToRotation( Vector3.up, Vector3.forward ), 0.2f * Vector3.one );
      }
    }

    private void OnDrawGizmos()
    {
      DrawGizmos( Color.blue, AttachmentPair );
    }

    private void OnDrawGizmosSelected()
    {
      DrawGizmos( Color.green, AttachmentPair );
    }
  }
}
