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
    public string _id { get; set; } // : UniqueId // suppress automatic generation
    public string project { get; set; }
    public string path { get; set; }
    public string urn { get; set; }
    public string family { get; set; }
    public string symbol { get; set; }
    public string category { get; set; }
    public string level { get; set; }
    public int x { get; set; }
    public int y { get; set; }
    public int z { get; set; }
    public double easting { get; set; }
    public double northing { get; set; }
    public string properties { get; set; } // : String // json dictionary of instance properties and values

    public InstanceData()
    {
    }

    public InstanceData(
      FamilyInstance a,
      Transform geoTransform,
      string model_urn )
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
      // _id        : UniqueId // suppress automatic generation
      // project    : String
      // path       : String
      // urn        : String
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
      urn = model_urn;
      family = fs.FamilyName;
      symbol = fs.Name;
      category = cat.Name;
      level = levelName;
      x = Util.ConvertFeetToMillimetres( location.X );
      y = Util.ConvertFeetToMillimetres( location.Y );
      z = Util.ConvertFeetToMillimetres( location.Z );
      easting = Util.RoundDegreesNorthOrEast( geolocation.X );
      northing = Util.RoundDegreesNorthOrEast( geolocation.Y );
      properties = jsonProps;
    }
  }
}
