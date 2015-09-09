#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
#endregion

namespace CompHoundRvt
{
  /// <summary>
  /// Export all target element ids and their
  /// FireRating parameter values to external database.
  /// </summary>
  [Transaction( TransactionMode.ReadOnly )]
  public class Command : IExternalCommand
  {
    /// <summary>
    /// Return the midpoint between two points.
    /// </summary>
    public static XYZ Midpoint( XYZ p, XYZ q )
    {
      return 0.5 * ( p + q );
    }

    /// <summary>
    /// Return a location point for a given element,
    /// if one can be determined, regardless of whether
    /// its Location property is a point or a curve.
    /// </summary>
    XYZ GetLocation( Element e )
    {
      XYZ p = null;

      Location x = e.Location;
      
      if( null == x )
      {
        BoundingBoxXYZ bb = e.get_BoundingBox( null );
      
        if( null != bb )
        {
          p = Midpoint( bb.Min, bb.Max );
        }
      }
      else
      {
        LocationPoint lp = x as LocationPoint;
        if( null != lp )
        {
          p = lp.Point;
        }
        else
        {
          LocationCurve lc = x as LocationCurve;
          if( null != lc )
          {
            Curve c = lc.Curve;
            p = Midpoint( c.GetEndPoint( 0 ), c.GetEndPoint( 1 ) );
          }
        }
      }
      return p;
    }

    /// <summary>
    /// Return a JSON string representing a dictionary
    /// of the given parameter names and values.
    /// </summary>
    string GetPropertiesJson(
      IList<Parameter> parameters )
    {
      int n = parameters.Count;
      List<string> a = new List<string>( n );
      foreach( Parameter p in parameters )
      {
        a.Add( string.Format( "\"{0}\":\"{1}\"",
          p.Definition.Name, p.AsValueString() ) );
      }
      string s = string.Join( ",", a );
      return "{" + s + "}";
    }

    /// <summary>
    /// Retrieve the family instance data to store in 
    /// the external database for the given component
    /// and return it as a dictionary in a JSON 
    /// formatted string.
    /// </summary>
    string GetComponentDataJson(
      FamilyInstance a,
      Transform geoTransform )
    {
      Document doc = a.Document;
      FamilySymbol symbol = a.Symbol;

      XYZ location = GetLocation( a );

      XYZ geolocation = geoTransform.OfPoint( 
        location );

      string properties = GetPropertiesJson( 
        a.GetOrderedParameters() );

      // /a/src/web/CompHoundWeb/model/instance.js
      // _id         : UniqueId // suppress automatic generation
      // project    : String
      // path       : String
      // family     : String
      // symbol     : String
      // level      : String
      // x          : Number
      // y          : Number
      // z          : Number
      // easting    : Number // Geo2d?
      // northing   : Number
      // properties : String // json dictionary of instance properties and values

      string s = string.Format(
        "\"_id\": \"{0}\", "
        + "\"project\": \"{1}\", "
        + "\"path\": \"{2}\", "
        + "\"family\": \"{3}\", "
        + "\"symbol\": \"{4}\", "
        + "\"level\": \"{5}\", "
        + "\"x\": \"{6}\", "
        + "\"y\": \"{7}\", "
        + "\"z\": \"{8}\", "
        + "\"easting\": \"{9}\", "
        + "\"northing\": \"{10}\", "
        + "\"properties\": \"{11}\"",
        a.UniqueId, doc.Title, doc.PathName,
        symbol.FamilyName, symbol.Name,
        doc.GetElement( a.LevelId ).Name,
        Util.RealString( location.X ),
        Util.RealString( location.Y ),
        Util.RealString( location.Z ),
        Util.RealString( geolocation.X ),
        Util.RealString( geolocation.Y ), 
        properties );
      
      return "{" + s + "}";
    }

    /// <summary>
    /// Retrieve the family instance data to store in 
    /// the external database for the given component
    /// and return it as a dictionary-like object.
    /// </summary>
    object GetInstanceData(
      FamilyInstance a,
      Transform geoTransform )
    {
      Document doc = a.Document;
      FamilySymbol symbol = a.Symbol;
      Category cat = a.Category;

      Debug.Assert( null != cat, 
        "expected valid category" );

      string levelName = ElementId.InvalidElementId == a.LevelId
        ? "-1"
        : doc.GetElement( a.LevelId ).Name;

      XYZ location = GetLocation( a );

      Debug.Assert( null != location,
        "expected valid location" );

      XYZ geolocation = geoTransform.OfPoint( 
        location );

      string properties = GetPropertiesJson( 
        a.GetOrderedParameters() );

      // /a/src/web/CompHoundWeb/model/instance.js
      // _id         : UniqueId // suppress automatic generation
      // project    : String
      // path       : String
      // family     : String
      // symbol     : String
      // category   : String
      // level      : String
      // x          : Number
      // y          : Number
      // z          : Number
      // easting    : Number // Geo2d?
      // northing   : Number
      // properties : String // json dictionary of instance properties and values

      object data = new {
        _id = a.UniqueId,
        project = doc.Title,
        path = doc.PathName,
        family = symbol.FamilyName,
        symbol = symbol.Name,
        category = cat.Name,
        level = levelName,
        x = location.X,
        y = location.Y,
        z = location.Z,
        easting = geolocation.X,
        northing = geolocation.Y,
        properties = properties
      };
      
      return data;
    }

    /// <summary>
    /// Return the project location transform, cf.
    /// https://github.com/jeremytammik/SetoutPoints
    /// </summary>
    Transform GetProjectLocationTransform( 
      Document doc )
    {
      // Retrieve the active project location position.

      ProjectPosition projectPosition
        = doc.ActiveProjectLocation.get_ProjectPosition(
          XYZ.Zero );

      // Create a translation vector for the offsets

      XYZ translationVector = new XYZ(
        projectPosition.EastWest,
        projectPosition.NorthSouth,
        projectPosition.Elevation );

      Transform translationTransform
        = Transform.CreateTranslation(
          translationVector );

      // Create a rotation for the angle about true north

      Transform rotationTransform
        = Transform.CreateRotation(
          XYZ.BasisZ, projectPosition.Angle );

      // Combine the transforms 

      Transform finalTransform
        = translationTransform.Multiply(
          rotationTransform );

      return finalTransform;
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      Application app = uiapp.Application;
      Document doc = uiapp.ActiveUIDocument.Document;

      Transform projectLocationTransform
        = GetProjectLocationTransform( doc );

      // Loop through all family instance elements
      // and export their data.

      FilteredElementCollector instances
        = new FilteredElementCollector( doc )
          .OfClass( typeof( FamilyInstance ) );

      object instanceData;
      string jsonResponse;

      foreach( FamilyInstance e in instances )
      {
        Debug.Print( e.Id.IntegerValue.ToString() );

        instanceData = GetInstanceData( e,
          projectLocationTransform );

        jsonResponse = Util.Put(
          "instances/" + e.UniqueId, instanceData );

        Debug.Print( jsonResponse );
      }
      return Result.Succeeded;
    }
  }
}
