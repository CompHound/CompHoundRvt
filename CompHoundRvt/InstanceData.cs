#region Namespaces
using System;
using Autodesk.Revit.DB;
using System.Diagnostics;
#endregion

namespace CompHoundRvt
{
  /// <summary>
  /// Container for the family instance data to store 
  /// in the external database for the given component.
  /// </summary>
  class InstanceData
  {
    string _id; // : UniqueId // suppress automatic generation
    string project;
    string path;
    string family;
    string symbol;
    string category;
    string level;
    double x;
    double y;
    double z;
    double easting;
    double northing;
    string properties; // : String // json dictionary of instance properties and values

    public InstanceData()
    {
    }

    public InstanceData(
      FamilyInstance a,
      Transform geoTransform )
    {
      Document doc = a.Document;
      FamilySymbol fs = a.Symbol;
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

      string jsonProps = Util.GetPropertiesJson(
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

      _id = a.UniqueId;
      project = doc.Title;
      path = doc.PathName;
      family = fs.FamilyName;
      symbol = fs.Name;
      category = cat.Name;
      level = levelName;
      x = location.X;
      y = location.Y;
      z = location.Z;
      easting = geolocation.X;
      northing = geolocation.Y;
      properties = jsonProps;
    }
  }
}
