﻿using System;
using UnityEngine;

namespace AgXUnity
{
  /// <summary>
  /// Constraint attachments for two objects - a reference object and a connected.
  /// The frame of the reference object, the reference frame, is by default the
  /// frame the constraint will be created from. It's possible to detach the relation
  /// between the frames, setting Synchronized to false.
  /// </summary>
  public class ConstraintAttachmentPair : ScriptAsset
  {
    /// <summary>
    /// The reference object that must contain a RigidBody
    /// component for the constraint to be valid.
    /// </summary>
    public GameObject ReferenceObject
    {
      get { return m_referenceFrame.Parent; }
      set
      {
        if ( value != null && value.GetComponentInParent<RigidBody>() == null ) {
          Debug.LogWarning( "Reference object must have a AgXUnity.RigidBody component (or in parents). Ignoring reference object.", value );
          return;
        }

        m_referenceFrame.SetParent( value );
      }
    }

    /// <summary>
    /// Connected object, the object constrained with the reference object.
    /// Null means "World".
    /// </summary>
    public GameObject ConnectedObject
    {
      get { return m_connectedFrame.Parent; }
      set
      {
        m_connectedFrame.SetParent( value );
      }
    }

    /// <summary>
    /// Reference frame holding world and relative to reference object
    /// transform. Paired with property ReferenceFrame.
    /// </summary>
    [SerializeField]
    private Frame m_referenceFrame = null;

    /// <summary>
    /// Reference frame holding world and relative to reference object
    /// transform.
    /// </summary>
    public Frame ReferenceFrame
    {
      get { return m_referenceFrame; }
    }

    /// <summary>
    /// Connected frame holding world and relative to connected object
    /// transform. Paired with property ConnectedFrame.
    /// </summary>
    [SerializeField]
    private Frame m_connectedFrame = null;

    /// <summary>
    /// Connected frame holding world and relative to connected object
    /// transform.
    /// </summary>
    public Frame ConnectedFrame
    {
      get { return m_connectedFrame; }
    }

    /// <summary>
    /// Synchronized flag. If synchronized the connected frame will, in world,
    /// have the same transform as the reference frame. Set this to false to
    /// have full control over the transform of the connected frame. Paired
    /// with property Synchronized.
    /// </summary>
    [SerializeField]
    private bool m_synchronized = true;

    /// <summary>
    /// Synchronized flag. If synchronized the connected frame will, in world,
    /// have the same transform as the reference frame. Set this to false to
    /// have full control over the transform of the connected frame.
    /// </summary>
    public bool Synchronized
    {
      get { return m_synchronized; }
      set { m_synchronized = value; }
    }

    private ConstraintAttachmentPair()
    {
    }

    /// <summary>
    /// Update callback from some manager, synchronizing the frames if Synchronized == true.
    /// </summary>
    public void Update()
    {
      if ( Synchronized ) {
        m_connectedFrame.Position = m_referenceFrame.Position;
        m_connectedFrame.Rotation = m_referenceFrame.Rotation;
      }
    }

    protected override void Construct()
    {
      m_referenceFrame = Create<Frame>();
      m_connectedFrame = Create<Frame>();
    }

    protected override bool Initialize()
    {
      return true;
    }

    public override void Destroy()
    {
    }
  }
}
