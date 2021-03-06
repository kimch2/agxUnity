﻿using UnityEngine;

namespace AgXUnity
{
  /// <summary>
  /// Contact material object.
  /// </summary>
  public class ContactMaterial : ScriptAsset
  {
    /// <summary>
    /// Native instance.
    /// </summary>
    private agx.ContactMaterial m_contactMaterial = null;

    /// <summary>
    /// Get the native instance, if created.
    /// </summary>
    public agx.ContactMaterial Native { get { return m_contactMaterial; } }

    /// <summary>
    /// First material in this contact material, paired with property Material1.
    /// </summary>
    [SerializeField]
    private ShapeMaterial m_material1 = null;

    /// <summary>
    /// Get or set first shape material.
    /// Note that it's not possible to change shape material instance after
    /// this contact material has been initialized.
    /// </summary>
    public ShapeMaterial Material1
    {
      get { return m_material1; }
      set
      {
        m_material1 = value;
      }
    }

    /// <summary>
    /// Second material in this contact material, paired with property Material2.
    /// </summary>
    [SerializeField]
    private ShapeMaterial m_material2 = null;

    /// <summary>
    /// Get or set second shape material.
    /// Note that it's not possible to change shape material instance after
    /// this contact material has been initialized.
    /// </summary>
    public ShapeMaterial Material2
    {
      get { return m_material2; }
      set
      {
        m_material2 = value;
      }
    }

    /// <summary>
    /// Friction model coupled to this contact material, paired with property FrictionModel.
    /// </summary>
    [SerializeField]
    private FrictionModel m_frictionModel = null;

    /// <summary>
    /// Get or set friction model coupled to this contact material.
    /// </summary>
    public FrictionModel FrictionModel
    {
      get { return m_frictionModel; }
      set
      {
        m_frictionModel = value;
        if ( Native != null && m_frictionModel != null && m_frictionModel.Native != null )
          Native.setFrictionModel( m_frictionModel.Native );
      }
    }

    /// <summary>
    /// Young's modulus of this contact material, paired with property YoungsModulus.
    /// </summary>
    [SerializeField]
    private float m_youngsModulus = 1.0E10f;

    /// <summary>
    /// Get or set Young's modulus of this contact material.
    /// </summary>
    [ClampAboveZeroInInspector]
    public float YoungsModulus
    {
      get { return m_youngsModulus; }
      set
      {
        m_youngsModulus = value;
        if ( m_contactMaterial != null )
          m_contactMaterial.setYoungsModulus( m_youngsModulus );
      }
    }

    /// <summary>
    /// Surface viscosity of this contact material, paired with property SurfaceViscosity.
    /// </summary>
    [SerializeField]
    private Vector2 m_surfaceViscosity = new Vector2( 1.0E-7f, 1.0E-7f );

    /// <summary>
    /// Get or set surface viscosity of this contact material.
    /// </summary>
    [ClampAboveZeroInInspector( true )]
    public Vector2 SurfaceViscosity
    {
      get { return m_surfaceViscosity; }
      set
      {
        m_surfaceViscosity = value;
        if ( Native != null ) {
          Native.setSurfaceViscosity( m_surfaceViscosity.x, agx.ContactMaterial.FrictionDirection.PRIMARY_DIRECTION );
          Native.setSurfaceViscosity( m_surfaceViscosity.y, agx.ContactMaterial.FrictionDirection.SECONDARY_DIRECTION );
        }
      }
    }

    /// <summary>
    /// Friction coefficients of this contact material, paired with property FrictionCoefficients.
    /// </summary>
    [SerializeField]
    private Vector2 m_frictionCoefficients = new Vector2( 0.4f, 0.4f );

    /// <summary>
    /// Get or set friction coefficients of this contact material.
    /// </summary>
    [ClampAboveZeroInInspector( true )]
    public Vector2 FrictionCoefficients
    {
      get { return m_frictionCoefficients; }
      set
      {
        m_frictionCoefficients = value;
        if ( Native != null ) {
          Native.setFrictionCoefficient( m_frictionCoefficients.x, agx.ContactMaterial.FrictionDirection.PRIMARY_DIRECTION );
          Native.setFrictionCoefficient( m_frictionCoefficients.y, agx.ContactMaterial.FrictionDirection.SECONDARY_DIRECTION );
        }
      }
    }

    /// <summary>
    /// Restitution of this contact material, paired with property Restitution.
    /// </summary>
    [SerializeField]
    private float m_restitution = 0.45f;

    /// <summary>
    /// Get or set restitution of this contact material.
    /// </summary>
    [ClampAboveZeroInInspector( true )]
    public float Restitution
    {
      get { return m_restitution; }
      set
      {
        m_restitution = value;
        if ( Native != null )
          Native.setRestitution( m_restitution );
      }
    }

    private ContactMaterial()
    {
    }

    protected override void Construct()
    {
    }

    protected override bool Initialize()
    {
      if ( Material1 == null || Material2 == null ) {
        Debug.LogWarning( name + ": Trying to create contact material with at least one unreferenced ShapeMaterial.", this );
        return false;
      }

      agx.Material m1 = Material1.GetInitialized<ShapeMaterial>().Native;
      agx.Material m2 = Material2.GetInitialized<ShapeMaterial>().Native;
      agx.ContactMaterial old = GetSimulation().getMaterialManager().getContactMaterial( m1, m2 );
      if ( old != null ) {
        Debug.LogWarning( name + ": Material manager already contains a contact material with this material pair. Ignoring this contact material.", this );
        return false;
      }

      m_contactMaterial = GetSimulation().getMaterialManager().getOrCreateContactMaterial( m1, m2 );

      if ( FrictionModel != null ) {
        m_contactMaterial.setFrictionModel( FrictionModel.GetInitialized<FrictionModel>().Native );
        // When the user changes friction model type (enum = BoxFriction, ScaleBoxFriction etc.)
        // the friction model object will create a new native instance. We'll receive callbacks
        // when this happens so we can assign it to our native contact material.
        FrictionModel.OnNativeInstanceChanged += OnFrictionModelNativeInstanceChanged;
      }

      return true;
    }

    public override void Destroy()
    {
      if ( GetSimulation() != null )
        GetSimulation().getMaterialManager().remove( m_contactMaterial );
      m_contactMaterial = null;
    }

    /// <summary>
    /// Callback from AgXUnity.FrictionModel when the friction model type has been changed.
    /// </summary>
    /// <param name="frictionModel"></param>
    private void OnFrictionModelNativeInstanceChanged( agx.FrictionModel frictionModel )
    {
      if ( Native != null )
        Native.setFrictionModel( frictionModel );
    }
  }
}
