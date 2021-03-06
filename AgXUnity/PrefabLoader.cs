﻿using System.Collections.Generic;
using UnityEngine;

namespace AgXUnity
{
  /// <summary>
  /// Prefab manager not doing much but possible to extend if load times gets significant.
  /// </summary>
  public class PrefabLoader
  {
    public static T Instantiate<T>( string prefabName ) where T : Object
    {
      T resource = Resources.Load<T>( prefabName );
      if ( resource == null )
        throw new Exception( "Unable to load resource: " + prefabName + " with type: " + typeof( T ).ToString() );

      T obj = Object.Instantiate<T>( resource );

      if ( typeof( T ) == typeof( GameObject ) ) {
        GameObject go = obj as GameObject;
        go.transform.position = Vector3.zero;
        go.transform.rotation = Quaternion.identity;
      }

      return obj;
    }
  }
}
