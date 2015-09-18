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
    #region Obsolete code before using RestSharp
    /// <summary>
    /// Retrieve the family instance data to store in 
    /// the external database for the given component
    /// and return it as a dictionary in a JSON 
    /// formatted string.
    /// Obsolete, replaced by GetInstanceData method.
    /// </summary>
    string GetComponentDataJson(
      FamilyInstance a,
      Transform geoTransform )
    {
      Document doc = a.Document;
      FamilySymbol symbol = a.Symbol;

      XYZ location = Util.GetLocation( a );

      XYZ geolocation = geoTransform.OfPoint( 
        location );

      string properties = Util.GetPropertiesJson( 
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
    /// Obsolete, replaced by InstanceData class.
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

      XYZ location = Util.GetLocation( a );

      Debug.Assert( null != location,
        "expected valid location" );

      XYZ geolocation = geoTransform.OfPoint( 
        location );

      string properties = Util.GetPropertiesJson( 
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
    #endregion // Obsolete code before using RestSharp

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      Application app = uiapp.Application;
      Document doc = uiapp.ActiveUIDocument.Document;

      Transform projectLocationTransform
        = Util.GetProjectLocationTransform( doc );

      // Loop through all family instance elements
      // and export their data.

      FilteredElementCollector instances
        = new FilteredElementCollector( doc )
          .OfClass( typeof( FamilyInstance ) );

      InstanceData instanceData;
      string jsonResponse;

      foreach( FamilyInstance e in instances )
      {
        Debug.Print( e.Id.IntegerValue.ToString() );

        instanceData = new InstanceData( e,
          projectLocationTransform );

        jsonResponse = Util.Put(
          "instances/" + e.UniqueId, instanceData );

        Debug.Print( jsonResponse );
      }
      return Result.Succeeded;
    }
  }
}
