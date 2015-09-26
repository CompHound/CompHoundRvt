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
    public string _id {get;set;} // : UniqueId // suppress automatic generation
    public string project {get;set;}
    public string path {get;set;}
    public string urn {get;set;} // populated later
    public string family {get;set;}
    public string symbol {get;set;}
    public string category {get;set;}
    public string level {get;set;}
    public double x {get;set;}
    public double y {get;set;}
    public double z {get;set;}
    public double easting {get;set;}
    public double northing {get;set;}
    public string properties {get;set;} // : String // json dictionary of instance properties and values

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
      urn = string.Empty;
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
