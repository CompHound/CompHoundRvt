#region Namespaces
using System.Collections.Generic;
using Autodesk.Revit.DB;
using RestSharp;
using Parameter = Autodesk.Revit.DB.Parameter;
using System.Diagnostics;
#endregion // Namespaces

namespace CompHoundRvt
{
  class Util
  {
    #region String Formatting
    /// <summary>
    /// Return a string for a real number
    /// formatted to two decimal places.
    /// </summary>
    public static string RealString( double a )
    {
      return a.ToString( "0.##" );
    }

    /// <summary>
    /// Return a JSON string representing a dictionary
    /// of the given parameter names and values.
    /// </summary>
    public static string GetPropertiesJson(
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
    #endregion // String Formatting

    #region Element and Project Location
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
    public static XYZ GetLocation( Element e )
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
    /// Return the project location transform, cf.
    /// https://github.com/jeremytammik/SetoutPoints
    /// </summary>
    public static Transform GetProjectLocationTransform(
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
    #endregion // Element and Project Location

    #region HTTP Access
    /// <summary>
    /// Timeout for HTTP calls.
    /// </summary>
    public static int Timeout = 1000;

    /// <summary>
    /// HTTP access constant to toggle between local and global server.
    /// </summary>
    public static bool UseLocalServer = true;

    // HTTP access constants.

    const string _base_url_local = "http://127.0.0.1:3001";
    const string _base_url_global = "http://comphound.herokuapp.com";
    const string _api_version = "api/v1";

    /// <summary>
    /// Return REST API base URL.
    /// </summary>
    public static string RestApiBaseUrl
    {
      get
      {
        return UseLocalServer
          ? _base_url_local
          : _base_url_global;
      }
    }

    /// <summary>
    /// Return REST API URI.
    /// </summary>
    public static string RestApiUri
    {
      get
      {
        return RestApiBaseUrl + "/" + _api_version;
      }
    }

    /// <summary>
    /// PUT JSON document data into 
    /// the specified mongoDB collection.
    /// </summary>
    public static bool Put(
      string collection_name_and_id,
      InstanceData data,
      out string result )
    {
      var client = new RestClient( RestApiBaseUrl );

      var request = new RestRequest( _api_version + "/"
        + collection_name_and_id, Method.PUT );

      request.RequestFormat = DataFormat.Json;

      request.AddBody( data ); // uses JsonSerializer

      IRestResponse response = client.Execute( request );

      bool rc = false;

      result = response.ErrorMessage;

      if( 0 < result.Length )
      {
        //if( response.ErrorMessage.Equals(
        //  "Unable to connect to the remote server" ) )
        //  "does the database exist at all?"

        Debug.Print( "HTTP PUT error: " + result );
      }
      else
      {
        result = response.Content; // raw content as string

        rc = result.Equals( "Accepted" );
      }
      return rc;
    }
    #endregion // HTTP Access
  }
}
